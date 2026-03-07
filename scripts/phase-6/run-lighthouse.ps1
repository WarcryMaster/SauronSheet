# Run Lighthouse audit (requires Node.js + lighthouse CLI)
param(
    [string]$url = "http://localhost:5000"
)

lighthouse $url --output html --output-path "lighthouse-report.html" --preset=desktop
Write-Host "Lighthouse audit complete. Report: lighthouse-report.html"
