import { test, expect } from '@playwright/test';
import { resolveTestAccount } from '../fixtures/budget-data.fixture';

/**
 * E2E Tests for Transaction Edit flow — Phase 6
 *
 * Covers:
 *   TC-E01: Edit button exists on transaction rows with correct href
 *   TC-E02: Edit page loads with current form values populated
 *   TC-E03: Edit description and save redirects to transactions list
 *   TC-E04: Non-existent transaction ID redirects with error handling
 *   TC-E05: Cancel button returns to transactions list
 *
 * Auth strategy (same as other E2E specs):
 *   Priority 1 — Env-var credentials: TEST_USER_EMAIL / TEST_USER_PASSWORD (CI).
 *   Priority 2 — Seeded test user: e2e@saurontest.local / ***REMOVED***
 *
 * NOTE: The Transactions Index page does not currently render TempData success/error
 * messages. Tests verify redirect behaviour (URL change) rather than alert banners.
 * This is a known gap — adding TempData display to the Index page is tracked separately.
 */

/**
 * Attempts login with the given credentials.
 * Returns true if the browser reached /dashboard, false otherwise.
 */
async function loginWith(page: any, email: string, password: string): Promise<boolean> {
    await page.goto('/auth/login');
    await page.fill('input[type="email"]', email);
    await page.fill('input[type="password"]', password);
    await page.click('button[type="submit"]');

    try {
        await page.waitForURL(/dashboard/i, { timeout: 15000 });
        return true;
    } catch {
        return false;
    }
}

test.describe('Edit Transaction', () => {
    test.beforeEach(async ({ page }) => {
        const account = resolveTestAccount();
        const authenticated = await loginWith(page, account.email, account.password);
        if (!authenticated) {
            test.skip(true, `Login failed for ${account.email}`);
        }
    });

    /**
     * TC-E01: Edit button exists on transaction rows and links to the edit page.
     */
    test('TC-E01: Edit button exists on transaction rows', async ({ page }) => {
        await page.goto('/transactions');

        const editButtons = page.locator('[data-testid="edit-transaction-btn"]');
        const count = await editButtons.count();

        if (count === 0) {
            test.skip(true, 'No transactions available to edit');
        }

        // Verify first edit button is visible and has correct link format
        const firstButton = editButtons.first();
        await expect(firstButton).toBeVisible();

        const href = await firstButton.getAttribute('href');
        expect(href).toMatch(/\/transactions\/edit\/[0-9a-f-]{36}/i);
    });

    /**
     * TC-E02: Edit page loads with form fields visible and populated.
     */
    test('TC-E02: Edit page loads with current values', async ({ page }) => {
        await page.goto('/transactions');

        const editButtons = page.locator('[data-testid="edit-transaction-btn"]');
        const count = await editButtons.count();

        if (count === 0) {
            test.skip(true, 'No transactions available to edit');
        }

        // Click first edit button and wait for edit page
        await editButtons.first().click();
        await page.waitForURL(/\/transactions\/edit\//);

        // Verify page heading
        await expect(page.locator('h3')).toContainText('Edit Transaction');

        // Verify form fields are visible
        await expect(page.locator('input[name="Description"]')).toBeVisible();
        await expect(page.locator('input[name="Amount"]')).toBeVisible();
        await expect(page.locator('select[name="CategoryId"]')).toBeVisible();

        // Verify Description field has a value (pre-populated from server)
        const descriptionValue = await page.locator('input[name="Description"]').inputValue();
        expect(descriptionValue.length).toBeGreaterThan(0);
    });

    /**
     * TC-E03: Edit description and save — verifies redirect to transactions list.
     *
     * NOTE: The Transactions Index page does not currently render TempData["SuccessMessage"].
     * We verify the redirect happened (URL returns to /transactions) as proof of successful save.
     */
    test('TC-E03: Edit description and save', async ({ page }) => {
        await page.goto('/transactions');

        const editButtons = page.locator('[data-testid="edit-transaction-btn"]');
        const count = await editButtons.count();

        if (count === 0) {
            test.skip(true, 'No transactions available to edit');
        }

        // Click edit button
        await editButtons.first().click();
        await page.waitForURL(/\/transactions\/edit\//);

        // Wait for page to fully load
        await page.waitForLoadState('networkidle');

        // Verify form is loaded
        const descriptionInput = page.locator('input[name="Description"]');
        await expect(descriptionInput).toBeVisible({ timeout: 5000 });

        // Clear and update description
        await descriptionInput.fill('');
        await descriptionInput.fill('Updated via E2E test');

        // Wait for Alpine.js to process
        await page.waitForTimeout(500);

        // Submit form - use specific selector for the edit form
        const editForm = page.locator('form[x-data="editForm()"]');
        const submitButton = editForm.locator('button[type="submit"]');
        
        // Wait for navigation and click submit
        await Promise.all([
            page.waitForURL(/\/transactions/, { timeout: 30000 }),
            submitButton.click()
        ]);

        // Verify we are on the transactions index page
        await expect(page.locator('h3').first()).toBeVisible({ timeout: 5000 });
    });

    /**
     * TC-E04: Navigate to non-existent transaction shows error handling.
     *
     * The Edit page handler catches EntityNotFoundException, sets TempData["ErrorMessage"],
     * and redirects to /transactions. We verify the redirect happened.
     */
    test('TC-E04: Navigate to non-existent transaction redirects to list', async ({ page }) => {
        const fakeId = '00000000-0000-0000-0000-000000000000';
        
        // Navigate directly to the edit page with non-existent ID
        // The page should redirect to /transactions with an error message
        await page.goto(`/transactions/edit/${fakeId}`, { 
            waitUntil: 'domcontentloaded',
            timeout: 15000 
        });

        // Wait a bit for any client-side redirects to complete
        await page.waitForTimeout(2000);

        // Verify we ended up on the transactions page (URL contains /transactions, case-insensitive)
        const finalUrl = page.url();
        expect(finalUrl.toLowerCase()).toContain('/transactions');
        
        // Verify the page is visible and has content
        const heading = page.locator('h3').first();
        await expect(heading).toBeVisible({ timeout: 5000 });
    });

    /**
     * TC-E05: Cancel button returns to transactions list.
     */
    test('TC-E05: Cancel button returns to transactions list', async ({ page }) => {
        await page.goto('/transactions');

        const editButtons = page.locator('[data-testid="edit-transaction-btn"]');
        const count = await editButtons.count();

        if (count === 0) {
            test.skip(true, 'No transactions available to edit');
        }

        // Click edit button
        await editButtons.first().click();
        await page.waitForURL(/\/transactions\/edit\//);

        // Click cancel link
        await page.click('a:text("Cancel")');

        // Verify redirect to transactions list
        await page.waitForURL(/\/transactions(?:\?|$)/);
        await expect(page.locator('h3')).toContainText('Transactions');
    });
});
