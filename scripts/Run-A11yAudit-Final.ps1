# Task 23: Accessibility Audit (Production Ready)

param([string]$ApplicationUrl = "http://localhost:54099/Categories")

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Task 23: Accessibility Audit" -ForegroundColor Cyan
Write-Host "Feature 2: Category Management MVP" -ForegroundColor Cyan 
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Skip certificate validation globally
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@

[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy

function Test-PageLoad {
    param([string]$Url)
    
    Write-Host "[*] Connecting to: $Url" -ForegroundColor Blue
    
    try {
        # Try HTTP first
        if ($Url -match "https") {
            $altUrl = $Url -replace "https://", "http://"
            Write-Host "[*] Trying HTTP fallback: $altUrl" -ForegroundColor Yellow
            $response = Invoke-WebRequest -Uri $altUrl -UseBasicParsing -ErrorAction Stop
        }
        else {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -ErrorAction Stop
        }
        
        Write-Host "[OK] Connected! Status: $($response.StatusCode)" -ForegroundColor Green
        return $response.Content
    }
    catch {
        Write-Host "[ERROR] Connection failed: $_" -ForegroundColor Red
        
        # Try HTTP directly
        Write-Host "[*] Attempting HTTP on port 5000..." -ForegroundColor Yellow
        try {
            $fallback = Invoke-WebRequest -Uri "http://localhost:5000/Categories" -UseBasicParsing -ErrorAction Stop
            Write-Host "[OK] Connected to alternate port! Status: $($fallback.StatusCode)" -ForegroundColor Green
            return $fallback.Content
        }
        catch {
            Write-Host "[ERROR] All connection attempts failed" -ForegroundColor Red
            return $null
        }
    }
}

function Analyze-Accessibility {
    param([string]$Html)
    
    $results = @{
        SemanticHTML = 0
        AriaAttributes = 0
        KeyboardSupport = 0
        FormValidation = 0
        Total = 0
    }
    
    # Semantic HTML checks
    Write-Host ""
    Write-Host "[TEST] Semantic HTML" -ForegroundColor Blue
    $formCount = ([regex]::Matches($Html, "<form")).Count
    $labelCount = ([regex]::Matches($Html, "<label")).Count
    $inputCount = ([regex]::Matches($Html, "<input")).Count
    $buttonCount = ([regex]::Matches($Html, "<button")).Count
    
    Write-Host "  Forms: $formCount | Labels: $labelCount | Inputs: $inputCount | Buttons: $buttonCount"
    
    if ($formCount -gt 0 -and $labelCount -gt 0 -and $inputCount -gt 0 -and $buttonCount -gt 1) {
        Write-Host "  [PASS] Solid semantic structure" -ForegroundColor Green
        $results.SemanticHTML = 4
    }
    
    # ARIA Attributes
    Write-Host ""
    Write-Host "[TEST] ARIA Attributes" -ForegroundColor Blue
    $ariaLabels = ([regex]::Matches($Html, "aria-label")).Count
    $ariaRequired = ([regex]::Matches($Html, "aria-required")).Count
    $ariaLive = ([regex]::Matches($Html, "aria-live")).Count
    
    Write-Host "  ARIA Labels: $ariaLabels | Required: $ariaRequired | Live: $ariaLive"
    
    if ($ariaLabels -gt 5) {
        Write-Host "  [PASS] Comprehensive ARIA implementation" -ForegroundColor Green
        $results.AriaAttributes = 3
    }
    elseif ($ariaLabels -gt 0) {
        Write-Host "  [WARN] Some ARIA attributes present" -ForegroundColor Yellow
        $results.AriaAttributes = 2
    }
    
    # Keyboard Support
    Write-Host ""
    Write-Host "[TEST] Keyboard Support" -ForegroundColor Blue
    $tabindex = ([regex]::Matches($Html, "tabindex")).Count
    $keyboardEvents = ([regex]::Matches($Html, "(onkeydown|onkeyup|addEventListener.*key)")).Count
    $modals = ([regex]::Matches($Html, "(modal|dialog)")).Count
    
    Write-Host "  Tabindex: $tabindex | Keyboard Events: $keyboardEvents | Modals: $modals"
    
    if ($keyboardEvents -gt 5 -and $modals -gt 0) {
        Write-Host "  [PASS] Good keyboard accessibility" -ForegroundColor Green
        $results.KeyboardSupport = 3
    }
    
    # Form Validation
    Write-Host ""
    Write-Host "[TEST] Form Validation" -ForegroundColor Blue
    $required = ([regex]::Matches($Html, "required")).Count
    $maxLength = ([regex]::Matches($Html, "maxlength")).Count
    $validation = ([regex]::Matches($Html, "validate")).Count
    $charCounter = ([regex]::Matches($Html, "(charCounter|counter)")).Count
    
    Write-Host "  Required: $required | MaxLength: $maxLength | Validation: $validation | Counter: $charCounter"
    
    if ($required -gt 3 -and $maxLength -gt 0 -and $validation -gt 0) {
        Write-Host "  [PASS] Comprehensive form validation" -ForegroundColor Green
        $results.FormValidation = 4
    }
    
    return $results
}

# Main execution
Write-Host ""
$html = Test-PageLoad -Url $ApplicationUrl

if ($null -eq $html) {
    Write-Host ""
    Write-Host "Make sure the application is running:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project src/SauronSheet.Frontend" -ForegroundColor Gray
    exit 1
}

# Run analysis
$audit = Analyze-Accessibility -Html $html

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "AUDIT RESULTS" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Semantic HTML: $($audit.SemanticHTML)/4" -ForegroundColor Green
Write-Host "ARIA Attributes: $($audit.AriaAttributes)/3" -ForegroundColor Green
Write-Host "Keyboard Support: $($audit.KeyboardSupport)/3" -ForegroundColor Green
Write-Host "Form Validation: $($audit.FormValidation)/4" -ForegroundColor Green
Write-Host ""
Write-Host "Total Score: $($audit.SemanticHTML + $audit.AriaAttributes + $audit.KeyboardSupport + $audit.FormValidation)/14" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host " 1. Open Lighthouse: Press F12 in browser -> Lighthouse tab" -ForegroundColor Gray
Write-Host " 2. Run axe-core: Install axe DevTools Chrome extension" -ForegroundColor Gray
Write-Host " 3. Manual testing: Tab through form, press Escape in modal" -ForegroundColor Gray
Write-Host ""
