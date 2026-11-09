<#
.SYNOPSIS
  Cronómetro objetivo del experimento de MTTR: consulta Alertmanager cada N segundos y registra
  en eval/results/alerts.csv cuándo empezó (startsAt) y cuándo dejó de estar activa cada alerta.

.DESCRIPTION
  No depende de n8n ni de Argos, así que mide igual en la condición "manual" (workflow apagado)
  y en la condición "argos". Dejarlo corriendo en una terminal aparte durante todo el experimento
  (Ctrl+C para cortar). 'resolved_seen_at' tiene una precisión de ±IntervalSeconds.

.EXAMPLE
  ./eval/timer.ps1
#>
param(
    [string] $AlertmanagerUrl = 'http://localhost:9093',
    [int] $IntervalSeconds = 5
)

$ErrorActionPreference = 'Stop'
$results = Join-Path $PSScriptRoot 'results'
New-Item -ItemType Directory -Force $results | Out-Null
$csv = Join-Path $results 'alerts.csv'
if (-not (Test-Path $csv)) {
    'fingerprint;alertname;severity;starts_at;first_seen_at;resolved_seen_at;duration_min' | Set-Content -Encoding utf8 $csv
}

$open = @{}   # fingerprint → alerta activa
Write-Host "Observando $AlertmanagerUrl cada ${IntervalSeconds}s → $csv (Ctrl+C para terminar)"
while ($true) {
    $now = (Get-Date).ToUniversalTime()
    try {
        $alerts = Invoke-RestMethod "$AlertmanagerUrl/api/v2/alerts?active=true&silenced=false&inhibited=true"
    }
    catch {
        Write-Warning "Alertmanager no responde: $($_.Exception.Message)"
        Start-Sleep -Seconds $IntervalSeconds
        continue
    }

    $seen = @{}
    foreach ($a in $alerts) {
        $seen[$a.fingerprint] = $true
        if (-not $open.ContainsKey($a.fingerprint)) {
            $open[$a.fingerprint] = [pscustomobject]@{
                Name = $a.labels.alertname; Severity = $a.labels.severity
                StartsAt = ([datetime]$a.startsAt).ToUniversalTime(); FirstSeen = $now
            }
            Write-Host ("[{0:u}] FIRING   {1} (startsAt {2:u})" -f $now, $a.labels.alertname, $open[$a.fingerprint].StartsAt)
        }
    }

    foreach ($fp in @($open.Keys)) {
        if ($seen.ContainsKey($fp)) { continue }
        $a = $open[$fp]
        $minutes = [math]::Round(($now - $a.StartsAt).TotalMinutes, 2)
        ('{0};{1};{2};{3:o};{4:o};{5:o};{6}' -f $fp, $a.Name, $a.Severity, $a.StartsAt, $a.FirstSeen, $now,
            $minutes.ToString([cultureinfo]::InvariantCulture)) | Add-Content -Encoding utf8 $csv
        Write-Host ("[{0:u}] RESOLVED {1} ({2} min)" -f $now, $a.Name, $minutes)
        $open.Remove($fp)
    }
    Start-Sleep -Seconds $IntervalSeconds
}
