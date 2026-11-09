<#
.SYNOPSIS
  Marca a mano los hitos de una corrida del experimento de MTTR en eval/results/runs.csv.

.DESCRIPTION
  Eventos de una corrida (en este orden):
    start  justo después de inyectar la falla
    diag   cuando identificaste la causa raíz (anotar cuál en -Note)
    fix    cuando aplicaste el arreglo (el comando que lo resolvió en -Note)
  La resolución de la alerta la registra solo eval/timer.ps1.

.EXAMPLE
  ./eval/mark.ps1 -Run R01 -Scenario F3 -Condition argos -Event start
  ./eval/mark.ps1 -Run R01 -Scenario F3 -Condition argos -Event diag -Note "node-exporter detenido"
  ./eval/mark.ps1 -Run R01 -Scenario F3 -Condition argos -Event fix  -Note "docker start node-exporter"
  ./eval/mark.ps1 -Run R02 -Condition manual -Event start      # modo ciego: sin -Scenario
#>
param(
    [Parameter(Mandatory)] [string] $Run,
    # En modo ciego se omite: el resumen la toma de eval/results/blind.csv.
    [ValidateSet('F1', 'F2', 'F3', 'F4', 'F5', 'F6', 'F7')] [string] $Scenario = '',
    [Parameter(Mandatory)] [ValidateSet('argos', 'manual')] [string] $Condition,
    [Parameter(Mandatory)] [ValidateSet('start', 'diag', 'fix')] [string] $Event,
    [string] $Note = ''
)

$results = Join-Path $PSScriptRoot 'results'
New-Item -ItemType Directory -Force $results | Out-Null
$csv = Join-Path $results 'runs.csv'
if (-not (Test-Path $csv)) { 'run;scenario;condition;event;at;note' | Set-Content -Encoding utf8 $csv }

$at = (Get-Date).ToUniversalTime()
('{0};{1};{2};{3};{4:o};"{5}"' -f $Run, $Scenario, $Condition, $Event, $at, $Note.Replace('"', "'")) |
    Add-Content -Encoding utf8 $csv
Write-Host ("[{0:u}] {1} {2}/{3}: {4} {5}" -f $at, $Run, $Scenario, $Condition, $Event, $Note)
