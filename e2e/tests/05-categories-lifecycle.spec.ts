/**
 * E2E Tests for Category Management — create and delete lifecycle.
 *
 * Covers:
 *   TC-C01 — Create a category via UI modal
 *   TC-C02 — Delete a category via list page
 *
 * E2E Testing Policy (from AGENTS.md):
 *   - MUST act as real user: page.click(), page.fill(), page.selectOption()
 *   - Wait for elements to be visible before interacting
 *   - DO NOT use page.evaluate() to execute direct JavaScript
 *   - DO NOT use fetch() inside page.evaluate()
 */

import { test, expect } from '@playwright/test';

const E2E_CAT_DELETE = 'E2E-Delete-Cat';

test.describe('Categories — create and delete lifecycle', () => {

    test.afterAll(async ({ browser }) => {
        // Cleanup: delete the test category if it still exists
        const context = await browser.newContext();
        const page = await context.newPage();

        await context.clearCookies();
        await page.goto('/auth/login');
        await page.fill('input[type="email"]', 'e2e@saurontest.local');
        await page.fill('input[type="password"]', '***REMOVED***');
        await page.click('button[type="submit"]');
        await page.waitForURL(/dashboard/i, { timeout: 15000 });

        await page.goto('/categories');
        await page.waitForLoadState('domcontentloaded');

        // Find and delete the test category if it exists
        const catItem = page.locator('.list-group-item').filter({
            has: page.locator('.fw-bold', { hasText: new RegExp(`^${E2E_CAT_DELETE}$`) }),
        });

        if ((await catItem.count()) > 0) {
            const deleteBtn = catItem.locator('button', { hasText: /delete/i });
            if (await deleteBtn.isVisible().catch(() => false)) {
                page.on('dialog', async dialog => {
                    await dialog.accept();
                });
                await deleteBtn.click();
                await page.waitForLoadState('domcontentloaded');
            }
        }

        await context.close();
    });

    /**
     * TC-C01: Create a category via the UI modal form.
     */
    test('TC-C01: create category via modal form', async ({ page }) => {
        // ── Authenticate ─────────────────────────────────────────────────────
        await page.goto('/auth/login');
        await page.fill('input[type="email"]', 'e2e@saurontest.local');
        await page.fill('input[type="password"]', '***REMOVED***');
        await page.click('button[type="submit"]');
        await page.waitForURL(/dashboard/i, { timeout: 15000 });

        // ── Navigate to categories ───────────────────────────────────────────
        await page.goto('/categories');
        await page.waitForLoadState('domcontentloaded');

        // ── Check if category already exists (idempotency) ───────────────────
        const existing = page.locator('.list-group-item').filter({
            has: page.locator('.fw-bold', { hasText: new RegExp(`^${E2E_CAT_DELETE}$`) }),
        });

        if ((await existing.count()) > 0) {
            // Already exists — skip creation, just verify it's there
            await expect(existing).toBeVisible();
            return;
        }

        // ── Open create modal ────────────────────────────────────────────────
        await page.getByRole('button', { name: 'Add New Category' }).click();

        const createModal = page.locator('#createCategoryModal');
        await expect(createModal).toBeVisible();

        // ── Fill form ────────────────────────────────────────────────────────
        await page.fill('#createName', E2E_CAT_DELETE);
        await page.selectOption('#createType', '1'); // Expense
        await page.selectOption('#createIcon', 'tag');
        await page.fill('#createColor', '#3498DB');

        const submitBtn = page.locator('#createSubmitBtn');
        await expect(submitBtn).toBeEnabled({ timeout: 5000 });

        // ── Submit ───────────────────────────────────────────────────────────
        await submitBtn.click();

        // ── Verify category appears in list ──────────────────────────────────
        await expect(createModal).toBeHidden({ timeout: 10000 });
        await page.waitForLoadState('domcontentloaded');

        const newItem = page.locator('.list-group-item').filter({
            has: page.locator('.fw-bold', { hasText: new RegExp(`^${E2E_CAT_DELETE}$`) }),
        });
        await expect(newItem).toBeVisible({ timeout: 5000 });
    });

    /**
     * TC-C02: Delete a category via the list page.
     */
    test('TC-C02: delete category removes it from list', async ({ page }) => {
        // ── Authenticate ─────────────────────────────────────────────────────
        await page.goto('/auth/login');
        await page.fill('input[type="email"]', 'e2e@saurontest.local');
        await page.fill('input[type="password"]', '***REMOVED***');
        await page.click('button[type="submit"]');
        await page.waitForURL(/dashboard/i, { timeout: 15000 });

        // ── Ensure category exists (reuse TC-C01 logic) ──────────────────────
        await page.goto('/categories');
        await page.waitForLoadState('domcontentloaded');

        const existing = page.locator('.list-group-item').filter({
            has: page.locator('.fw-bold', { hasText: new RegExp(`^${E2E_CAT_DELETE}$`) }),
        });

        if ((await existing.count()) === 0) {
            // Create it first
            await page.getByRole('button', { name: 'Add New Category' }).click();
            await page.locator('#createCategoryModal').waitFor({ state: 'visible' });
            await page.fill('#createName', E2E_CAT_DELETE);
            await page.selectOption('#createType', '1');
            await page.selectOption('#createIcon', 'tag');
            await page.fill('#createColor', '#3498DB');
            await page.locator('#createSubmitBtn').click();
            await page.locator('#createCategoryModal').waitFor({ state: 'hidden', timeout: 10000 });
            await page.waitForLoadState('domcontentloaded');
        }

        // ── Delete the category ──────────────────────────────────────────────
        const catItem = page.locator('.list-group-item').filter({
            has: page.locator('.fw-bold', { hasText: new RegExp(`^${E2E_CAT_DELETE}$`) }),
        });

        await expect(catItem).toBeVisible();

        const deleteBtn = catItem.locator('button', { hasText: /delete/i });
        await expect(deleteBtn).toBeVisible();

        // The delete button opens an MDB modal (#deleteConfirmModal), NOT a native browser dialog.
        // Click the delete button to open the modal, then click the confirm button inside it.
        await deleteBtn.click();

        const deleteModal = page.locator('#deleteConfirmModal');
        await expect(deleteModal).toBeVisible({ timeout: 5000 });

        const confirmDeleteBtn = page.locator('#deleteConfirmBtn');
        await expect(confirmDeleteBtn).toBeVisible();
        await confirmDeleteBtn.click();

        // Wait for modal to close and page to reload after deletion
        await expect(deleteModal).toBeHidden({ timeout: 10000 });
        await page.waitForLoadState('domcontentloaded');

        // ── Verify category is gone ──────────────────────────────────────────
        const goneItem = page.locator('.list-group-item').filter({
            has: page.locator('.fw-bold', { hasText: new RegExp(`^${E2E_CAT_DELETE}$`) }),
        });
        await expect(goneItem).toHaveCount(0);
    });
});
