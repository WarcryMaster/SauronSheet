# Validar compresión y cache headers
Write-Host "Checklist performance headers:"
Write-Host "- [ ] Brotli/Gzip activos en site.css"
Write-Host "- [ ] Cache-Control: public, max-age=31536000 en site.css"
Write-Host "- [ ] Cache-Control: no-cache en páginas dinámicas"
Write-Host "Revisar headers con curl o navegador y marcar checklist."
