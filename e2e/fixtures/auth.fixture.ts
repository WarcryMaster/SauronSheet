import { test as base } from '@playwright/test';
import { loginAsTestAccount } from './budget-data.fixture';

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
    await loginAsTestAccount(page);
    
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
