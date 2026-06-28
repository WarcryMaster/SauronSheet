/**
 * Global auth setup for Playwright E2E tests.
 *
 * Logs in once per worker and saves the authenticated browser state
 * (cookies + localStorage) to a shared file. All tests in the worker
 * reuse this state via `storageState`, skipping the login step.
 *
 * This saves ~3-4s per test file × 10+ test files = ~30-40s per CI run.
 */

import { test as setup, expect } from '@playwright/test';
import { loginAsTestAccount } from './fixtures/budget-data.fixture';

const AUTH_FILE = '.auth/user.json';

setup('authenticate', async ({ page }) => {
    // The page is fresh — no prior cookies or storage.
    // The app web server is already running (Playwright's webServer config).
    await loginAsTestAccount(page);

    // Verify we're really authenticated
    await expect(page).toHaveURL(/dashboard/i);

    // Persist cookies and localStorage for all test projects
    await page.context().storageState({ path: AUTH_FILE });

    setup.skip(!AUTH_FILE, 'Auth setup did not produce a storage state file');
});
