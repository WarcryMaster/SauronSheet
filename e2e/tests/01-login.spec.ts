import { test, expect } from '@playwright/test';

/**
 * E2E Tests for Login/Authentication Flow
 * Covers scenarios from phase-6-spec.md: SC-6.4 (Password Reset)
 */

test.describe('Login Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/Auth/Login');
  });

  test('TC-001: Login page loads with all required elements', async ({ page }) => {
    // Verify page title
    await expect(page).toHaveTitle(/Login|Sign In/i);
    
    // Verify email input exists
    const emailInput = page.locator('input[name="email"], input[type="email"]');
    await expect(emailInput).toBeVisible();
    
    // Verify password input exists
    const passwordInput = page.locator('input[name="password"], input[type="password"]');
    await expect(passwordInput).toBeVisible();
    
    // Verify submit button exists
    const submitButton = page.locator('button[type="submit"]');
    await expect(submitButton).toBeVisible();
    
    // Verify "Forgot Password" link exists
    const forgotPasswordLink = page.locator('a[href*="ForgotPassword"], a:has-text("Forgot"), a:has-text("Reset")');
    await expect(forgotPasswordLink).toBeVisible();
    
    // Verify "Register" link exists
    const registerLink = page.locator('a[href*="Register"], a:has-text("Register"), a:has-text("Sign Up")');
    await expect(registerLink).toBeVisible();
  });

  test('TC-002: Login with valid credentials redirects to Dashboard', async ({ page }) => {
    const testEmail = process.env.TEST_USER_EMAIL || 'test@example.com';
    const testPassword = process.env.TEST_USER_PASSWORD || 'TestPassword123!';
    
    await page.fill('input[name="email"], input[type="email"]', testEmail);
    await page.fill('input[name="password"], input[type="password"]', testPassword);
    await page.click('button[type="submit"]');
    
    // Wait for navigation to dashboard
    await page.waitForURL(/Dashboard/, { timeout: 15000 });
    
    // Verify we're on the dashboard
    await expect(page).toHaveURL(/Dashboard/);
  });

  test('TC-003: Login with invalid credentials shows error message', async ({ page }) => {
    await page.fill('input[name="email"], input[type="email"]', 'invalid@example.com');
    await page.fill('input[name="password"], input[type="password"]', 'WrongPassword123!');
    await page.click('button[type="submit"]');
    
    // Wait for error message to appear
    await page.waitForSelector('.alert-danger, .text-danger, [role="alert"]:has-text("Invalid"), :has-text("error")', { timeout: 5000 });
    
    // Verify error message is visible
    const errorMessage = page.locator('.alert-danger, .text-danger, [role="alert"]');
    await expect(errorMessage).toBeVisible();
  });

  test('TC-004: Login form validation - empty email', async ({ page }) => {
    await page.fill('input[name="password"], input[type="password"]', 'SomePassword123!');
    await page.click('button[type="submit"]');
    
    // Check for validation error on email field
    const emailError = page.locator('.field-validation-error, .text-danger:has-text("email"), input:invalid');
    await expect(emailError.first()).toBeVisible({ timeout: 5000 });
  });

  test('TC-005: Login form validation - empty password', async ({ page }) => {
    await page.fill('input[name="email"], input[type="email"]', 'test@example.com');
    await page.click('button[type="submit"]');
    
    // Check for validation error on password field
    const passwordError = page.locator('.field-validation-error, .text-danger:has-text("password"), input:invalid');
    await expect(passwordError.first()).toBeVisible({ timeout: 5000 });
  });

  test('TC-006: Forgot Password link navigates to reset page', async ({ page }) => {
    const forgotPasswordLink = page.locator('a[href*="ForgotPassword"], a:has-text("Forgot"), a:has-text("Reset")');
    await forgotPasswordLink.click();
    
    // Wait for navigation to forgot password page
    await page.waitForURL(/ForgotPassword|ResetPassword/, { timeout: 10000 });
    
    // Verify we're on the password reset page
    await expect(page).toHaveURL(/ForgotPassword|ResetPassword/);
  });

  test('TC-007: Responsive design - mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    // Verify login form is still visible and usable
    const emailInput = page.locator('input[name="email"], input[type="email"]');
    await expect(emailInput).toBeVisible();
    
    const submitButton = page.locator('button[type="submit"]');
    await expect(submitButton).toBeInViewport();
  });
});
