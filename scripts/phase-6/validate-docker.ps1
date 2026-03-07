# Validar build y run de Dockerfile
$dockerfile = "../../src/SauronSheet.Frontend/Dockerfile"
docker build -f $dockerfile -t sauronsheet-frontend-test ..\..\src\SauronSheet.Frontend
if ($LASTEXITCODE -eq 0) {
  Write-Host "[OK] Docker build exitoso"
  docker run --rm -d -p 5000:5000 sauronsheet-frontend-test
  Write-Host "[OK] Docker run exitoso (verificar en http://localhost:5000)"
} else {
  Write-Host "[ERROR] Docker build falló"
}
