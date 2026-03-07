# Ejecutar auditoría axe-core (requiere Node.js + axe CLI)
param(
    [string]$url = "http://localhost:5000"
)

axe $url --save axe-report.json
Write-Host "axe-core audit complete. Report: axe-report.json"
