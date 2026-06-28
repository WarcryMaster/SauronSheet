import { test as base } from '@playwright/test';

/**
 * Auth fixture for SauronSheet E2E tests.
 *
 * The page is already authenticated via storageState (from auth.setup.ts).
 * This fixture exists solely as a typed alias so tests can use
 * `{ authenticatedPage: page }` for self-documenting intent.
 *
 * No login or teardown needed — the Playwright context manages auth
 * lifecycle via the shared `.auth/user.json` storage state.
 */

interface TestFixtures {
  authenticatedPage: any;
}

export const test = base.extend<TestFixtures>({
  authenticatedPage: async ({ page }, use) => {
    await use(page);
  },
});

export { expect } from '@playwright/test';
