import { test as base } from '@playwright/test';

/**
 * Auth fixture for SauronSheet E2E tests
 * Provides authenticated page context for tests that require login
 */

interface TestFixtures {
  authenticatedPage: any;
}

export const test = base.extend<TestFixtures>({
  authenticatedPage: async ({ page }, use) => {
    // Setup: Login before each test
    await page.goto('/Auth/Login');
    
    // Use test credentials from environment variables
    const testEmail = process.env.TEST_USER_EMAIL || 'test@example.com';
    const testPassword = process.env.TEST_USER_PASSWORD || 'TestPassword123!';
    
    // Fill login form
    await page.fill('input[name="email"]', testEmail);
    await page.fill('input[name="password"]', testPassword);
    
    // Submit login
    await page.click('button[type="submit"]');
    
    // Wait for navigation to dashboard (successful login)
    await page.waitForURL(/\/Dashboard/, { timeout: 10000 });
    
    // Provide the authenticated page to the test
    await use(page);
    
    // Teardown: Logout after each test
    try {
      await page.goto('/Auth/Logout');
      await page.waitForURL(/\/Auth\/Login|\/Index/, { timeout: 5000 });
    } catch (e) {
      // Ignore logout errors in teardown
    }
  },
});

export { expect } from '@playwright/test';
