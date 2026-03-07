# Validar tamaño de CSS final (site.css < 50KB)
$css = Get-Item ../../src/SauronSheet.Frontend/wwwroot/css/site.css
if ($css.Length -lt 51200) {
  Write-Host "OK: site.css size is $($css.Length) bytes (< 50KB)"
  exit 0
} else {
  Write-Host "ERROR: site.css size is $($css.Length) bytes (>= 50KB)"
  exit 1
}
