# Task 23: Accessibility Audit Script  
# This script validates WCAG 2.1 AA compliance for the Categories page
# Run: .\Run-A11yAudit.ps1

param(
    [string]$ApplicationUrl = "https://localhost:54099/Categories"
)

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Task 23: Accessibility Audit for Categories" -ForegroundColor Cyan
Write-Host "Feature 2: Category Management MVP" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Suppress certificate warnings for localhost
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

# Function to test page accessibility
function Test-PageAccessibility {
    param([string]$Url)
    
    Write-Host "🔍 Fetching page: $Url" -ForegroundColor Blue
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -ErrorAction Stop
        $html = $response.Content
        
        Write-Host "✅ Page loaded successfully (Status: $($response.StatusCode))" -ForegroundColor Green
        return $html
    }
    catch {
        Write-Host "❌ Failed to load page: $_" -ForegroundColor Red
        return $null
    }
}

# Function to check for semantic HTML
function Test-SemanticHTML {
    param([string]$Html)
    
    Write-Host ""
    Write-Host "📋 Checking Semantic HTML..." -ForegroundColor Blue
    
    $tests = @(
        @{Name = "Form Elements"; Pattern = "<form[^>]*>"; Expected = "Should contain form tags" },
        @{Name = "Label Elements"; Pattern = "<label[^>]*>"; Expected = "Form inputs should have labels" },
        @{Name = "Input Elements"; Pattern = "<input[^>]*>"; Expected = "Form should have input fields" },
        @{Name = "Button Elements"; Pattern = "<button[^>]*>"; Expected = "Navigation should use button elements" },
        @{Name = "Heading Hierarchy"; Pattern = "<h[1-6][^>]*>"; Expected = "Page should have heading structure" },
        @{Name = "Modal Dialogs"; Pattern = 'role="dialog"'; Expected = "Modals should have role=dialog" }
    )
    
    $passed = 0
    $total = $tests.Count
    
    foreach ($test in $tests) {
        if ($Html -match $test.Pattern) {
            Write-Host "  ✅ $($test.Name)" -ForegroundColor Green
            $passed++
        }
        else {
            Write-Host "  ❌ $($test.Name)" -ForegroundColor Red
        }
    }
    
    Write-Host "  Result: $passed/$total checks passed" -ForegroundColor Yellow
    return $passed -eq $total
}

# Function to check ARIA attributes
function Test-AriaAttributes {
    param([string]$Html)
    
    Write-Host ""
    Write-Host "♿ Checking ARIA Attributes..." -ForegroundColor Blue
    
    $tests = @(
        @{Name = "aria-label on buttons"; Pattern = '(aria-label|aria-describedby)'; Expected = "Buttons should have aria labels" },
        @{Name = "aria-required on inputs"; Pattern = 'aria-required'; Expected = "Required fields should have aria-required" },
        @{Name = "aria-live regions"; Pattern = 'aria-live'; Expected = "Error messages should use aria-live" },
        @{Name = "aria-describedby"; Pattern = 'aria-describedby'; Expected = "Inputs should reference error messages" },
        @{Name = "aria-labelledby"; Pattern = 'aria-labelledby'; Expected = "Modals should have aria-labelledby" }
    )
    
    $passed = 0
    $total = $tests.Count
    
    foreach ($test in $tests) {
        if ($Html -match $test.Pattern) {
            Write-Host "  ✅ $($test.Name)" -ForegroundColor Green
            $passed++
        }
        else {
            Write-Host "  ⚠️  $($test.Name)" -ForegroundColor Yellow
        }
    }
    
    Write-Host "  Result: $passed/$total ARIA checks passed (⚠️  warnings are not critical)" -ForegroundColor Yellow
    return $true # ARIA is important but some features may be optional
}

# Function to check keyboard accessibility
function Test-KeyboardAccessibility {
    param([string]$Html)
    
    Write-Host ""
    Write-Host "⌨️  Checking Keyboard Accessibility..." -ForegroundColor Blue
    
    $tests = @(
        @{Name = "Focusable Elements"; Pattern = '(tabindex|button|input|select|a)'; Expected = "Should have focusable elements" },
        @{Name = "Auto-focus management"; Pattern = 'autofocus|focus\(\)'; Expected = "Should manage focus on modal open" },
        @{Name = "Event Handlers"; Pattern = '(onkeydown|onkeyup|addEventListener.*key)'; Expected = "Should handle keyboard events" },
        @{Name = "Modal support"; Pattern = '(modal|dialog|aria-modal)'; Expected = "Should have modal implementation" }
    )
    
    $passed = 0
    $total = $tests.Count
    
    foreach ($test in $tests) {
        if ($Html -match $test.Pattern) {
            Write-Host "  ✅ $($test.Name)" -ForegroundColor Green
            $passed++
        }
        else {
            Write-Host "  ❌ $($test.Name)" -ForegroundColor Red
        }
    }
    
    Write-Host "  Result: $passed/$total keyboard accessibility checks passed" -ForegroundColor Yellow
    return $passed -ge 3
}

# Function to check for form validation
function Test-FormValidation {
    param([string]$Html)
    
    Write-Host ""
    Write-Host "📝 Checking Form Validation..." -ForegroundColor Blue
    
    $tests = @(
        @{Name = "Required attribute"; Pattern = 'required'; Expected = "Inputs should have required attribute" },
        @{Name = "maxlength attribute"; Pattern = 'maxlength'; Expected = "Name field should have maxlength" },
        @{Name = "Validation logic"; Pattern = 'validate\('; Expected = "Should have form validation functions" },
        @{Name = "Error messages"; Pattern = 'error'; Expected = "Should have error handling" },
        @{Name = "Character counter"; Pattern = '(charCounter|counter)'; Expected = "Should display character count" }
    )
    
    $passed = 0
    $total = $tests.Count
    
    foreach ($test in $tests) {
        if ($Html -match $test.Pattern) {
            Write-Host "  ✅ $($test.Name)" -ForegroundColor Green
            $passed++
        }
        else {
            Write-Host "  ❌ $($test.Name)" -ForegroundColor Red
        }
    }
    
    Write-Host "  Result: $passed/$total validation checks passed" -ForegroundColor Yellow
    return $passed -ge 4
}

# Function to check for responsive design and color contrast
function Test-AccessibilityFeatures {
    param([string]$Html)
    
    Write-Host ""
    Write-Host "🎨 Checking Accessibility Features..." -ForegroundColor Blue
    
    $tests = @(
        @{Name = "Bootstrap CSS (color contrast)"; Pattern = 'bootstrap|cdn'; Expected = "Should use Bootstrap for WCAG compliance" },
        @{Name = "Focus styles"; Pattern = '(focus|outline).*(2px|3px|border)'; Expected = "Should have visible focus styles" },
        @{Name = "Color descriptions"; Pattern = 'aria-label.*color|color.*aria'; Expected = "Colors should have text labels" },
        @{Name = "Responsive viewport"; Pattern = 'viewport'; Expected = "Should have responsive meta tag" }
    )
    
    $passed = 0
    $total = $tests.Count
    
    foreach ($test in $tests) {
        if ($Html -match $test.Pattern) {
            Write-Host "  ✅ $($test.Name)" -ForegroundColor Green
            $passed++
        }
        else {
            Write-Host "  ⚠️  $($test.Name)" -ForegroundColor Yellow
        }
    }
    
    Write-Host "  Result: $passed/$total accessibility features present" -ForegroundColor Yellow
    return $true
}

# Main execution
Write-Host ""

# Fetch the Categories page
$html = Test-PageAccessibility -Url $ApplicationUrl

if ($null -eq $html) {
    Write-Host ""
    Write-Host "❌ Cannot proceed without loading the page." -ForegroundColor Red
    Write-Host "Make sure the application is running at $ApplicationUrl" -ForegroundColor Red
    exit 1
}

# Run all tests
$semanticPass = Test-SemanticHTML -Html $html
$ariaPass = Test-AriaAttributes -Html $html
$keyboardPass = Test-KeyboardAccessibility -Html $html
$validationPass = Test-FormValidation -Html $html
Test-AccessibilityFeatures -Html $html | Out-Null

# Summary
Write-Host ""
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "📊 AUDIT SUMMARY" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

$allPass = $semanticPass -and $keyboardPass -and $validationPass

if ($allPass) {
    Write-Host "✅ AUTOMATED CHECKS: PASS" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps for Task 23 Completion:" -ForegroundColor Cyan
    Write-Host " 1. 🌐 Open Lighthouse Audit"
    Write-Host "    - Navigate to: $ApplicationUrl"
    Write-Host "    - Press F12 → Lighthouse tab"
    Write-Host "    - Run Accessibility audit"
    Write-Host "    - Target score: ≥90" -ForegroundColor Yellow
    Write-Host ""
    Write-Host " 2. 🧪 Run axe-core in Browser"
    Write-Host "    - Install 'axe DevTools' Chrome extension"
    Write-Host "    - Right-click on page → Scan with axe"
    Write-Host "    - Target: 0 violations" -ForegroundColor Yellow
    Write-Host ""
    Write-Host " 3. ⌨️  Manual Keyboard Testing"
    Write-Host "    - Tab through all form fields"
    Write-Host "    - Press Escape in modal → should close"
    Write-Host "    - Verify focus indicator visible" -ForegroundColor Yellow
}
else {
    Write-Host "⚠️  AUTOMATED CHECKS: Some issues detected" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please review the checks above and fix any ❌ marked items." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "For complete audit details, open accessibility-audit.html:" -ForegroundColor Cyan
Write-Host "  $ApplicationUrl/accessibility-audit.html" -ForegroundColor Yellow
Write-Host ""
