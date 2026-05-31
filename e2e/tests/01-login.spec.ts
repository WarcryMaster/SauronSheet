import { test, expect } from '@playwright/test';
import { resolveTestAccount } from '../fixtures/budget-data.fixture';

/**
 * E2E Tests for Login/Authentication Flow
 */

test.describe('Login Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/auth/login');
  });

  test('TC-001: Login page loads with all required elements', async ({ page }) => {
    // Verify page title
    await expect(page).toHaveTitle(/Login|Sign In/i);
    
    // Verify email input exists (actual name is 'Input.Email')
    const emailInput = page.locator('input[type="email"]');
    await expect(emailInput).toBeVisible();
    
    // Verify password input exists (actual name is 'Input.Password')
    const passwordInput = page.locator('input[type="password"]');
    await expect(passwordInput).toBeVisible();
    
    // Verify submit button exists
    const submitButton = page.locator('button[type="submit"]');
    await expect(submitButton).toBeVisible();
    
    // Verify "Register" link exists in the form area
    const registerLink = page.locator('.card-body a[href*="register"]');
    await expect(registerLink).toBeVisible();
  });

  test('TC-002: Login with valid credentials redirects to Dashboard', async ({ page }) => {
    const account = resolveTestAccount();

    await page.fill('input[type="email"]', account.email);
    await page.fill('input[type="password"]', account.password);
    await page.click('button[type="submit"]');
    
    // Wait for navigation to dashboard
    await page.waitForURL(/dashboard/i, { timeout: 15000 });
    
    // Verify we're on the dashboard
    await expect(page).toHaveURL(/dashboard/i);
  });

  test('TC-003: Login with invalid credentials shows error message', async ({ page }) => {
    await page.fill('input[type="email"]', 'invalid@example.com');
    await page.fill('input[type="password"]', 'WrongPassword123!');
    await page.click('button[type="submit"]');
    
    // Wait for error message alert to appear (server-side validation)
    const errorMessage = page.locator('.alert-danger');
    await expect(errorMessage).toBeVisible({ timeout: 10000 });
  });

  test('TC-004: Login form validation - empty email', async ({ page }) => {
    await page.fill('input[type="password"]', 'SomePassword123!');
    await page.click('button[type="submit"]');
    
    // HTML5 validation should prevent submission for required field
    const emailInput = page.locator('input[type="email"]');
    await expect(emailInput).toBeVisible();
    
    // Check that browser validation fired (form was not submitted)
    // The page should still be on login (no error alert from server)
    await expect(page).toHaveURL(/\/auth\/login/i);
  });

  test('TC-005: Login form validation - empty password', async ({ page }) => {
    await page.fill('input[type="email"]', 'test@example.com');
    await page.click('button[type="submit"]');
    
    // HTML5 validation should prevent submission for required field
    const passwordInput = page.locator('input[type="password"]');
    await expect(passwordInput).toBeVisible();
    
    // Check that browser validation fired (form was not submitted)
    await expect(page).toHaveURL(/\/auth\/login/i);
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
