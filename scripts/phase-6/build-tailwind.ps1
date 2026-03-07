# Build Tailwind CSS for development/production
$tailwindCli = "tailwindcss"
$input = "../../src/SauronSheet.Frontend/tailwind-input.css"
$output = "../../src/SauronSheet.Frontend/wwwroot/css/site.css"
$mode = if ($env:ASPNETCORE_ENVIRONMENT -eq "Production") { "--minify" } else { "" }

& $tailwindCli -i $input -o $output $mode
Write-Host "Tailwind CSS built: $output"
