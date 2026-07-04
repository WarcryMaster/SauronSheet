param(
    [string]$Pptx = "resources\SauronSheet_TFM_Presentation.pptx",
    [string]$OutDir = "resources\preview"
)

$ErrorActionPreference = "Stop"
$full = (Resolve-Path $Pptx).Path
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
$outFull = (Resolve-Path $OutDir).Path

try {
    $ppt = New-Object -ComObject PowerPoint.Application
} catch {
    Write-Output "NO_POWERPOINT"
    exit 1
}

$pres = $ppt.Presentations.Open($full, $true, $false, $false)
# Export selected slides as PNG (1-based)
$targets = @(1, 4, 7, 13, 18, 21, 24, 29, 37, 39)
foreach ($n in $targets) {
    $slide = $pres.Slides.Item($n)
    $dest = Join-Path $outFull ("slide_{0:D2}.png" -f $n)
    $slide.Export($dest, "PNG", 1280, 720)
}
$pres.Close()
$ppt.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($ppt) | Out-Null
Write-Output "RENDERED"
