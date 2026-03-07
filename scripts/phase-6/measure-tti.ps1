# Medir TTI/FCP con Lighthouse (perfil Slow 4G)
param(
    [string]$url = "http://localhost:5000"
)

lighthouse $url --output json --output-path "tti-report.json" --preset=desktop --throttling.method=devtools --throttling.cpuSlowdownMultiplier=4 --throttling.rttMs=150 --throttling.throughputKbps=1600 --throttling.requestLatencyMs=150 --throttling.downloadThroughputKbps=1600 --throttling.uploadThroughputKbps=750
Write-Host "TTI/FCP audit complete. Report: tti-report.json"
