# Ejecutar auditoría Lighthouse accesibilidad
param(
    [string]$url = "http://localhost:5000"
)

lighthouse $url --output html --output-path "lighthouse-a11y-report.html" --only-categories=accessibility
Write-Host "Lighthouse accessibility audit complete. Report: lighthouse-a11y-report.html"
