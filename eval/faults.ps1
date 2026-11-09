<#
.SYNOPSIS
  Inyecta o restaura una falla del catálogo del OE3 (eval/scenarios.json).

.EXAMPLE
  ./eval/faults.ps1 -Scenario F3 -Action inject
  ./eval/faults.ps1 -Scenario F3 -Action restore

.EXAMPLE
  # Modo ciego (experimento de MTTR): elige una falla al azar del pool y NO la muestra; queda anotada en
  # eval/results/blind.csv para el resumen. El restore por corrida limpia lo que haya quedado.
  ./eval/faults.ps1 -Action inject -Run R01 -Pool F1,F2,F3,F4,F7
  ./eval/faults.ps1 -Action restore -Run R01

.NOTES
  En el experimento de MTTR el "restore" lo hace el operador A MANO (es el arreglo que se mide);
  este script solo sirve para preparar/limpiar. Las fallas con tráfico (F1/F2) lanzan k6 'normal'
  en segundo plano (contenedor argos-eval-k6, ~5 min) para que haya requests que fallen.
#>
param(
    [ValidateSet('F1', 'F2', 'F3', 'F4', 'F5', 'F6', 'F7')] [string] $Scenario,
    [Parameter(Mandatory)] [ValidateSet('inject', 'restore')] [string] $Action,
    [string] $Run,
    [string[]] $Pool
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$k6Compose = Join-Path $root 'k6/docker-compose.yml'
$blindLog = Join-Path $PSScriptRoot 'results/blind.csv'
$blind = -not $Scenario
if ($blind) {
    if (-not $Run) { throw 'Sin -Scenario hace falta -Run (modo ciego).' }
    if ($Action -eq 'inject') {
        if (-not $Pool) { throw 'Modo ciego: indicar -Pool (p.ej. F1,F2,F3,F4,F7).' }
        $Scenario = Get-Random -InputObject ($Pool | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
        New-Item -ItemType Directory -Force (Split-Path $blindLog) | Out-Null
        if (-not (Test-Path $blindLog)) { 'run;scenario;injected_at' | Set-Content -Encoding utf8 $blindLog }
        ('{0};{1};{2:o}' -f $Run, $Scenario, (Get-Date).ToUniversalTime()) | Add-Content -Encoding utf8 $blindLog
    }
    else {
        $entry = Import-Csv $blindLog -Delimiter ';' | Where-Object run -eq $Run | Select-Object -Last 1
        if (-not $entry) { throw "No hay corrida $Run en $blindLog" }
        $Scenario = $entry.scenario
    }
}
$label = if ($blind) { "Corrida $Run" } else { $Scenario }

function Start-K6([string] $test) {
    docker rm -f argos-eval-k6 2>$null | Out-Null
    docker compose -f $k6Compose run -d --name argos-eval-k6 --rm k6 run "/tests/$test" | Out-Null
    Write-Host "k6 $test corriendo en segundo plano (argos-eval-k6)"
}

function Stop-K6 { docker rm -f argos-eval-k6 2>$null | Out-Null }

$stopTargets = @{
    F1 = 'membresia-api'; F2 = 'membresia-sqlserver'; F3 = 'node-exporter'
    F4 = 'cadvisor'; F7 = 'nginx-prometheus-exporter'
}

switch ($Action) {
    'inject' {
        if ($stopTargets.ContainsKey($Scenario)) { docker stop $stopTargets[$Scenario] | Out-Null }
        switch ($Scenario) {
            { $_ -in 'F1', 'F2' } { Start-K6 'normal-traffic.js' }
            # En modo ciego el tráfico normal corre igual en todas las fallas: que el k6 no delate F1/F2.
            { $blind -and $_ -notin 'F1', 'F2', 'F6' } { Start-K6 'normal-traffic.js' }
            'F5' {
                # stress-ng sobre todas las CPU; HostCriticalCPU (>95 % promedio 5m, for 2m) tarda ~7 min.
                docker rm -f argos-eval-stress 2>$null | Out-Null
                docker run -d --name argos-eval-stress --rm alexeiled/stress-ng:latest --cpu 0 --cpu-load 100 --timeout 20m | Out-Null
            }
            'F6' { Start-K6 'attack-pattern.js' }
        }
        Write-Host ("[{0:u}] {1}: falla inyectada" -f (Get-Date).ToUniversalTime(), $label)
    }
    'restore' {
        if ($stopTargets.ContainsKey($Scenario)) { docker start $stopTargets[$Scenario] | Out-Null }
        if ($Scenario -eq 'F5') { docker rm -f argos-eval-stress 2>$null | Out-Null }
        if ($blind -or $Scenario -in 'F1', 'F2', 'F6') { Stop-K6 }
        Write-Host ("[{0:u}] {1}: restaurada ({2})" -f (Get-Date).ToUniversalTime(), $label, $Scenario)
    }
}
