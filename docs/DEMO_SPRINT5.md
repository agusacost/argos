# Demo Sprint 5 — Auto-documentación + Dashboard (runbook)

Guía paso a paso para **demostrar el bucle completo de Argos**: alerta nueva → el RAG la detecta como
no documentada → el LLM genera la documentación técnica → se persiste en Qdrant → aparece en el
dashboard y la base de conocimiento → se envía el email.

Todos los comandos son **PowerShell** desde `D:\DEV\argos`. Proyecto compose: `argos-platform`.

---

## ⚠️ Antes de empezar (leer esto)

- **Apagar la PC NO borra nada.** Los volúmenes (workflow de n8n, credencial SMTP, datos de Qdrant),
  las imágenes ya construidas y la red `argos-net` **sobreviven al apagado**. Solo se pierden con
  `docker compose down -v` — **ese comando NO lo corras**.
- Tras encender, con la política `restart: unless-stopped` los contenedores suelen **volver a arrancar
  solos** cuando Docker Desktop levanta. Igual, el Paso 1 los deja arriba de forma idempotente.
- **Hacé el Paso 3 (email) HOY**, antes de apagar: el cambio queda guardado y listo para mañana.

---

## Paso 0 — Encender y esperar a Docker (PC recién prendida)

1. Iniciá sesión en Windows. Abrí **Docker Desktop** (si no arranca solo) y esperá a que el ícono de
   la ballena esté **fijo/estable** (no animado) = daemon listo. Puede tardar 1-2 min (backend WSL2).
2. (Opcional) Confirmá que responde:
   ```powershell
   docker version --format "{{.Server.Version}}"   # si imprime versión, el daemon está listo
   ```

## Paso 1 — Levantar el stack (idempotente)

Las imágenes ya están construidas, así que **NO hace falta `--build`** (arranca en segundos).

```powershell
cd D:\DEV\argos

# La red externa persiste al reboot; el 2>$null ignora "already exists"
docker network create argos-net 2>$null

# Levantar/asegurar todo (usa las imágenes ya construidas)
docker compose -p argos-platform up -d
```

> Solo usá `--build` si **cambiaste código** desde la última vez (rag-api o dashboard):
> `docker compose -p argos-platform up -d --build`

## Paso 2 — Esperar a que esté listo (chequeo automático)

Tras encender, los servicios tardan unos segundos en quedar sanos (rag-api precarga el modelo de
embeddings, n8n registra el webhook). Pegá este bloque: espera hasta que **los 3 estén verdes** y
avisa "TODO LISTO".

```powershell
function Wait-Url($url, $name, $tries=40) {
  for ($i=0; $i -lt $tries; $i++) {
    try { Invoke-RestMethod $url -TimeoutSec 3 | Out-Null; Write-Host "OK  $name"; return $true } catch {}
    Start-Sleep 3
  }
  Write-Host "FALLA  $name ($url)"; return $false
}
$a = Wait-Url "http://localhost:8000/health"  "rag-api"
$b = Wait-Url "http://localhost:5678/healthz" "n8n"
$c = Wait-Url "http://localhost:3001/"        "dashboard"
if ($a -and $b -and $c) { Write-Host "`n>>> TODO LISTO — se puede demostrar <<<" -ForegroundColor Green }
```

También podés ver el estado crudo:
```powershell
docker ps --filter "name=argos-" --format "{{.Names}}  {{.Status}}"
```
`argos-n8n` y `argos-rag-api` deben figurar **(healthy)**.

## Paso 3 — (Recomendado) Fijar un email que abras seguro en la demo

El envío funciona (Gmail devuelve `messageId`), pero Gmail→Hotmail suele caer en **Correo no deseado**.
Para la demo conviene mandarlo a tu **propio Gmail** (Gmail→Gmail entra a bandeja principal al toque).

1. Editá `docker/monitoring/.env` y cambiá la línea:
   ```
   ALERT_RECIPIENT_EMAIL=juanagustinacost@gmail.com
   ```
2. Recreá n8n para que tome el cambio (no borra nada; el workflow y la credencial persisten):
   ```powershell
   docker compose -p argos-platform up -d --force-recreate n8n
   # esperá salud:
   for ($i=0;$i -lt 30){ try{ if((Invoke-RestMethod http://localhost:5678/healthz).status -eq 'ok'){break} }catch{}; Start-Sleep 2; $i++ }
   ```

## Paso 4 — Abrir el dashboard y anotar el estado inicial

Abrí **http://localhost:3001** y mostrá:
- **Métricas**: total de incidentes y la card **"Auto-documentados"** (anotá el número actual).
- **Base de conocimiento**: cuántos docs auto-generados hay ahora.

Así después se ve el antes/después.

## Paso 5 — Disparar la prueba e2e (alerta NUEVA)

Pegá este bloque completo en PowerShell. Simula una alerta de Alertmanager con un tema que **no** está
en la base de conocimiento (certificado TLS por expirar), con `severity=critical` para superar el umbral.

```powershell
$summary = "Certificado TLS del dominio principal expira en menos de 48 horas"
$desc = "El certificado TLS de api.argos.local vence pronto; renovar via ACME antes del vencimiento para evitar caida del proxy HTTPS y errores de handshake."
$alert = @{ labels = @{ alertname = "CertificadoTLSPorExpirar"; severity = "critical" }; annotations = @{ summary = $summary; description = $desc } }
$payload = @{
  status = "firing"
  alerts = @($alert, $alert, $alert)
  commonLabels = @{ alertname = "CertificadoTLSPorExpirar"; severity = "critical" }
  commonAnnotations = @{ summary = $summary; description = $desc }
} | ConvertTo-Json -Depth 6

Invoke-RestMethod -Uri "http://localhost:5678/webhook/argos-alerts" -Method Post -Body $payload -ContentType "application/json"
# Esperado: @{ message = Workflow was started }
```

> **Opcional (para verificar antes que dé "nuevo"):** confirma que el RAG lo clasifica como no documentado:
> ```powershell
> $q = @{ text = "$summary. $desc"; top_k = 3 } | ConvertTo-Json
> (Invoke-RestMethod http://localhost:8000/query -Method Post -Body $q -ContentType "application/json").is_documented
> # Esperado: False
> ```

## Paso 6 — Verificar el resultado

El workflow tarda ~10-20s (hace 2 llamadas al LLM: síntesis + documentación).

```powershell
# 1) Aparece el incidente auto-generado en la base de conocimiento
$kb = Invoke-RestMethod "http://localhost:8000/incidents/knowledge-base"
"KB auto-generados = $($kb.total)"
$kb.data | ForEach-Object { " - $($_.tipo_alerta) [$($_.estado)]" }

# 2) Métricas actualizadas (total sube; auto_documentado incrementa)
$m = Invoke-RestMethod "http://localhost:8000/incidents/metrics"
"total=$($m.total_incidents)  auto_documentado=$($m.by_status.auto_documentado)"

# 3) Ver el documento markdown que generó el LLM
$id = $kb.data[0].id
(Invoke-RestMethod "http://localhost:8000/incidents/$id").documento
```

Y en el **dashboard (http://localhost:3001)**, refrescá:
- **Incidentes**: fila `CertificadoTLSPorExpirar` con badge violeta **auto documentado** → click → modal
  con el documento markdown renderizado.
- **Base de conocimiento**: el nuevo doc.
- **Métricas**: la card "Auto-documentados" subió.

Y revisá el **email** en la casilla del Paso 3 (si es Gmail, bandeja principal; si dejaste Hotmail, mirá
**Correo no deseado**).

---

## Guion sugerido para el profesor (30-60 s)

1. "Esta es una alerta de infraestructura que **nunca vio** el sistema" → muestro `is_documented=False`.
2. "La disparo como lo haría Alertmanager" → POST al webhook.
3. "Argos consulta su base de conocimiento, no encuentra el incidente, y **genera la documentación
   técnica con IA**" → muestro en el dashboard el incidente nuevo + el markdown (causa raíz, pasos,
   prevención) + el email.
4. "Queda **persistido** en la base vectorial y disponible como conocimiento para la próxima vez."

---

## Problemas comunes

| Síntoma | Causa | Solución |
|---------|-------|----------|
| El POST al webhook devuelve **404** | El workflow quedó inactivo | `docker exec argos-n8n n8n update:workflow --id=argos-alert-analysis --active=true` y `docker restart argos-n8n` |
| No aparece incidente nuevo | El resumen coincidió con la KB (`is_documented=True`) | Usá un tema más distinto (cambiá `$summary`/`$desc`) |
| No llega el email | Filtro de spam / dirección equivocada | Revisá Correo no deseado; usá el Paso 3 (Gmail propio) |
| Re-corro y el total no sube | El incidente ya existe (dedup por `tipo_alerta+resumen`) | Cambiá el `$summary` para que sea otro incidente |
| `argos-n8n` figura *unhealthy* | — | El healthcheck usa `127.0.0.1` (no `localhost`); si recién arrancó, esperá el `start_period` |

## Reset opcional (para demostrar desde cero)

Si querés que la card "Auto-documentados" arranque en 0, cambiá el `$summary` a otro incidente nuevo en
cada corrida (cada resumen distinto = incidente distinto). No hace falta borrar nada. **No uses
`down -v`** salvo que quieras reconstruir todo (perderías el workflow y la credencial SMTP de n8n).
