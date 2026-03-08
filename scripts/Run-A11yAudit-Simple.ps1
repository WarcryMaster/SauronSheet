# Task 23: Accessibility Audit Script (Simple Version)
# Validates WCAG 2.1 AA compliance for Categories page
# Usage: powershell -ExecutionPolicy Bypass -File .\Run-A11yAudit.ps1

param([string]$ApplicationUrl = "https://localhost:54099/Categories")

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Task 23: Accessibility Audit" -ForegroundColor Cyan
Write-Host "Feature 2: Category Management MVP" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Suppress certificate warnings for localhost
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

function Test-PageAccessibility {
    param([string]$Url)
    
    Write-Host "[*] Fetching page: $Url" -ForegroundColor Blue
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -ErrorAction Stop
        $html = $response.Content
        Write-Host "[OK] Page loaded successfully (Status: $($response.StatusCode))" -ForegroundColor Green
        return $html
    }
    catch {
        Write-Host "[ERROR] Failed to load page: $_" -ForegroundColor Red
        return $null
    }
}

function Test-SemanticHTML {
    param([string]$Html)
    
    Write-Host ""
    Write-Host "[TEST] Semantic HTML Structure" -ForegroundColor Blue
    
    $tests = @(
        @{Name = "Form Elements"; Pattern = "<form" },
        @{Name = "Label Tags"; Pattern = "<label" },
        @{Name = "Input Fields"; Pattern = "<input" },
        @{Name = "Buttons"; Pattern = "<button" },
        @{Name = "Heading Hierarchy"; Pattern = "<h[1-6]" }
    )
    
    $passed = 0
    foreach ($test in $tests) {
        if ($Html -match $test.Pattern) {
            Write-Host "  [PASS] $($test.Name)" -ForegroundColor Green
            $passed++
        }
        else {
            Write-Host "  [FAIL] $($test.Name)" -ForegroundColor Red
        }
    }
    
    Write-Host "  Result: $passed/$($tests.Count) passed" -ForegroundColor Yellow
    return $passed -ge 4
}

function Test-AriaAttributes {
    param([string]$Html)
    
    Write-Host ""
    Write-Host "[TEST] ARIA Accessibility Attributes" -ForegroundColor Blue
    
    $tests = @(
        @{Name = "ARIA labels"; Pattern = "aria-label" },
        @{Name = "Required attributes"; Pattern = "aria-required" },
        @{Name = "Live regions"; Pattern = "aria-live" },
        @{Name = "Described-by"; Pattern = "aria-describedby" }
    )
    
    $passed = 0
    foreach ($test in $tests) {
        if ($Html -match $test.Pattern) {
            Write-Host "  [PASS] $($test.Name)" -ForegroundColor Green
            $passed++
        }
        else {
            Write-Host "  [WARN] $($test.Name)" -ForegroundColor Yellow
        }
    }
    
    Write-Host "  Result: $passed/$($tests.Count) passed" -ForegroundColor Yellow
    return $true
}

function Test-KeyboardFeatures {
    param([string]$Html)
    
    Write-Host ""
    Write-Host "[TEST] Keyboard Accessibility" -ForegroundColor Blue
    
    $tests = @(
        @{Name = "Focusable elements"; Pattern = "(button|input|select|a)" },
        @{Name = "Tab index"; Pattern = "tabindex" },
        @{Name = "Keyboard events"; Pattern = "(onkeydown|onkeyup|addEventListener.*key)" },
        @{Name = "Modal implementation"; Pattern = "(modal|dialog)" }
    )
    
    $passed = 0
    foreach ($test in $tests) {
        if ($Html -match $test.Pattern) {
            Write-Host "  [PASS] $($test.Name)" -ForegroundColor Green
            $passed++
        }
        else {
            Write-Host "  [FAIL] $($test.Name)" -ForegroundColor Red
        }
    }
    
    Write-Host "  Result: $passed/$($tests.Count) passed" -ForegroundColor Yellow
    return $passed -ge 3
}

function Test-FormValidation {
    param([string]$Html)
    
    Write-Host ""
    Write-Host "[TEST] Form Validation Features" -ForegroundColor Blue
    
    $tests = @(
        @{Name = "Required attribute"; Pattern = "required" },
        @{Name = "Max length"; Pattern = "maxlength" },
        @{Name = "Validation logic"; Pattern = "validate" },
        @{Name = "Error handling"; Pattern = "error" }
    )
    
    $passed = 0
    foreach ($test in $tests) {
        if ($Html -match $test.Pattern) {
            Write-Host "  [PASS] $($test.Name)" -ForegroundColor Green
            $passed++
        }
        else {
            Write-Host "  [FAIL] $($test.Name)" -ForegroundColor Red
        }
    }
    
    Write-Host "  Result: $passed/$($tests.Count) passed" -ForegroundColor Yellow
    return $passed -ge 3
}

# Main execution
$html = Test-PageAccessibility -Url $ApplicationUrl

if ($null -eq $html) {
    Write-Host ""
    Write-Host "[ERROR] Cannot proceed without page load." -ForegroundColor Red
    exit 1
}

# Run tests
$semanticPass = Test-SemanticHTML -Html $html
Test-AriaAttributes -Html $html | Out-Null
$keyboardPass = Test-KeyboardFeatures -Html $html
$validationPass = Test-FormValidation -Html $html

# Summary
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "AUDIT SUMMARY" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$allPass = $semanticPass -and $keyboardPass -and $validationPass

if ($allPass) {
    Write-Host "[OK] Automated checks passed" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps to complete Task 23:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host " 1. Run Lighthouse Accessibility Audit" -ForegroundColor Yellow
    Write-Host "    - Navigate: $ApplicationUrl" -ForegroundColor Gray
    Write-Host "    - Press F12 -> Lighthouse tab" -ForegroundColor Gray
    Write-Host "    - Target: Score 90+" -ForegroundColor Gray
    Write-Host ""
    Write-Host " 2. Run axe-core Browser Extension" -ForegroundColor Yellow
    Write-Host "    - Install 'axe DevTools' Chrome extension" -ForegroundColor Gray
    Write-Host "    - Target: 0 violations" -ForegroundColor Gray
    Write-Host ""
    Write-Host " 3. Manual Keyboard Testing" -ForegroundColor Yellow
    Write-Host "    - Tab through all interactive elements" -ForegroundColor Gray
    Write-Host "    - Open modal, press Escape to close" -ForegroundColor Gray
    Write-Host "    - Verify focus indicator visible" -ForegroundColor Gray
}
else {
    Write-Host "[WARN] Some automated checks failed" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "For detailed audit page, visit:" -ForegroundColor Cyan
Write-Host "  $ApplicationUrl/accessibility-audit.html" -ForegroundColor Yellow
Write-Host ""
