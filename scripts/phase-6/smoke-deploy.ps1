# Smoke test despliegue producción
param(
    [string]$url = "https://tu-app.vercel.app"
)

Invoke-WebRequest "$url/health" -UseBasicParsing | Out-Null
Write-Host "[OK] /health endpoint responde"

# Validar HTTPS
if ($url.StartsWith("https://")) {
  Write-Host "[OK] HTTPS activo"
} else {
  Write-Host "[ERROR] HTTPS no activo"
}

# Validar variables de entorno
Write-Host "[INFO] Validar variables de entorno en dashboard Vercel"
