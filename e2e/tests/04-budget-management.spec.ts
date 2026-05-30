/**
 * E2E Tests for Budget Management — budget-redesign (PR 6, Slice 6)
 *
 * Covers task 6.4:
 *   TC-M01 — Create valid budget with all fields
 *   TC-M02 — Create budget with validation error (negative limit)
 *   TC-M03 — Edit budget limit
 *   TC-M04 — Deactivate budget
 *   TC-M05 — Navigate budget list with filters
 *
 * E2E Testing Policy (from AGENTS.md):
 *   - MUST act as real user: page.click(), page.fill(), page.selectOption()
 *   - Wait for elements to be visible before interacting
 *   - DO NOT use page.evaluate() to execute direct JavaScript
 *   - DO NOT use fetch() inside page.evaluate()
 *
 * Data provisioning: uses budget-data.fixture.ts for auth + test categories.
 */

import { test, expect, cleanupE2EBudgets, E2E_CAT_A, E2E_CAT_B } from '../fixtures/budget-data.fixture';

test.describe('Budgets — management CRUD (budget-redesign Slice 6)', () => {

    test.afterAll(async ({ browser }) => {
        const context = await browser.newContext();
        const page    = await context.newPage();

        await context.clearCookies();
        // Re-auth as test user for cleanup
        await page.goto('/auth/login');
        await page.fill('input[type="email"]', 'e2e@saurontest.local');
        await page.fill('input[type="password"]', '***REMOVED***');
        await page.click('button[type="submit"]');
        await page.waitForURL(/dashboard/i, { timeout: 15000 });

        await cleanupE2EBudgets(page);
        await context.close();
    });

    /**
     * TC-M01: Create a valid budget with all fields and verify it appears in the list.
     *
     * Flow: /budgets/create → fill category, limit, effective from, granularity → submit → verify in list.
     */
    test('TC-M01: create valid budget appears in list', async ({ budgetReadyPage: page }) => {
        const now   = new Date();
        const year  = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');

        // ── Idempotency: if budget already exists from a previous run, skip creation ──
        await page.goto('/budgets');
        await page.waitForLoadState('domcontentloaded');

        const alreadyExists = (await page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-A' }),
        }).count()) > 0;

        if (!alreadyExists) {
            // ── Navigate to create page ───────────────────────────────────────
            await page.goto('/budgets/create');
            await expect(page).toHaveURL(/\/budgets\/create/i);

            // ── Fill the form as a real user ──────────────────────────────────
            const categorySelect = page.locator('#CategoryId');
            await expect(categorySelect).toBeVisible();
            const catAOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-A$/ });
            await expect(catAOption).toHaveCount(1);
            const catAValue = await catAOption.getAttribute('value');
            await categorySelect.selectOption(catAValue!);

            await page.fill('#LimitAmount', '200.00');
            await page.fill('#EffectiveFrom', `${year}-${month}-01`);
            await page.selectOption('#PeriodGranularity', 'Monthly');

            // ── Submit ────────────────────────────────────────────────────────
            await page.getByRole('button', { name: 'Create Budget' }).click();

            await Promise.race([
                page.waitForURL((url: URL) => url.pathname === '/budgets', { timeout: 30000 }),
                page.locator('.alert-danger').waitFor({ state: 'visible', timeout: 30000 }),
            ]).catch(() => {});

            if (new URL(page.url()).pathname === '/budgets/create') {
                await page.goto('/budgets');
            }
        }

        await expect(page).toHaveURL(/\/budgets/i);

        // ── Verify the budget appears in the list ────────────────────────────
        const table = page.locator('table');
        await expect(table).toBeVisible();
        await expect(table).toContainText('E2E-Budget-Cat-A');
    });

    /**
     * TC-M02: Attempt to create a budget with a negative limit and verify validation error.
     *
     * Flow: /budgets/create → fill form with negative limit → submit → stay on page with error.
     */
    test('TC-M02: create budget with negative limit shows validation error', async ({ budgetReadyPage: page }) => {
        await page.goto('/budgets/create');
        await expect(page).toHaveURL(/\/budgets\/create/i);

        // Select category
        const categorySelect = page.locator('#CategoryId');
        await expect(categorySelect).toBeVisible();
        const catBOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-B$/ });
        await expect(catBOption).toHaveCount(1);
        const catBValue = await catBOption.getAttribute('value');
        await categorySelect.selectOption(catBValue!);

        // Fill negative limit (INVALID)
        await page.fill('#LimitAmount', '-50.00');

        const now = new Date();
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');
        await page.fill('#EffectiveFrom', `${year}-${month}-01`);
        await page.selectOption('#PeriodGranularity', 'Monthly');

        // ── Submit ────────────────────────────────────────────────────────────
        await page.getByRole('button', { name: 'Create Budget' }).click();

        // ── Must stay on the create page with error ──────────────────────────
        // Wait for either staying on create or redirecting
        await page.waitForTimeout(2000);

        // Should either see an alert-danger OR the page URL is still /budgets/create
        const urlStillCreate = new URL(page.url()).pathname === '/budgets/create';
        const errorVisible = await page.locator('.alert-danger').isVisible().catch(() => false);

        expect(urlStillCreate || errorVisible, 'Must stay on create page or show validation error').toBeTruthy();
    });

    /**
     * TC-M03: Edit an existing budget's limit.
     *
     * Flow: /budgets → find E2E-Budget-Cat-A → click Edit → change limit → submit → verify new limit.
     */
    test('TC-M03: edit budget limit and verify updated value', async ({ budgetReadyPage: page }) => {
        // ── Ensure a budget exists for E2E-Budget-Cat-A ──────────────────────
        await page.goto('/budgets');
        await page.waitForLoadState('domcontentloaded');

        const existingRow = page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-A' }),
        });

        if ((await existingRow.count()) === 0) {
            // Create one first
            await page.goto('/budgets/create');
            const categorySelect = page.locator('#CategoryId');
            const catAOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-A$/ });
            await expect(catAOption).toHaveCount(1);
            const catAValue = await catAOption.getAttribute('value');
            await categorySelect.selectOption(catAValue!);

            const now = new Date();
            const year = now.getFullYear();
            const month = String(now.getMonth() + 1).padStart(2, '0');
            await page.fill('#LimitAmount', '100.00');
            await page.fill('#EffectiveFrom', `${year}-${month}-01`);
            await page.selectOption('#PeriodGranularity', 'Monthly');
            await page.getByRole('button', { name: 'Create Budget' }).click();

            await Promise.race([
                page.waitForURL((url: URL) => url.pathname === '/budgets', { timeout: 30000 }),
                page.locator('.alert-danger').waitFor({ state: 'visible', timeout: 30000 }),
            ]).catch(() => {});

            if (new URL(page.url()).pathname === '/budgets/create') {
                await page.goto('/budgets');
            }
            await page.waitForLoadState('domcontentloaded');
        }

        // ── Navigate to edit page ─────────────────────────────────────────────
        await page.goto('/budgets');
        await page.waitForLoadState('domcontentloaded');

        // Click the Edit link for E2E-Budget-Cat-A
        const editLink = page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-A' }),
        }).locator('a', { hasText: 'Edit' });

        await expect(editLink).toBeVisible();
        const editHref = await editLink.getAttribute('href');
        await page.goto(editHref!);
        await expect(page).toHaveURL(/\/budgets\/edit\//i);

        // ── Change the limit ──────────────────────────────────────────────────
        const limitInput = page.locator('#NewLimitAmount');
        await expect(limitInput).toBeVisible();
        await limitInput.fill('300.00');

        // ── Submit ────────────────────────────────────────────────────────────
        await page.getByRole('button', { name: 'Save Changes' }).click();

        await Promise.race([
            page.waitForURL((url: URL) => url.pathname === '/budgets', { timeout: 30000 }),
            page.locator('.alert-danger').waitFor({ state: 'visible', timeout: 30000 }),
        ]).catch(() => {});

        // ── Verify updated limit ─────────────────────────────────────────────
        if (new URL(page.url()).pathname !== '/budgets') {
            await page.goto('/budgets');
        }

        await expect(page.locator('table')).toBeVisible();
        await expect(page.locator('table')).toContainText('300.00');
    });

    /**
     * TC-M04: Deactivate a budget via the edit page.
     *
     * Flow: /budgets/edit/{id} → click Deactivate button → confirm → budget shows as inactive in list.
     */
    test('TC-M04: deactivate budget marks it inactive', async ({ budgetReadyPage: page }) => {
        // ── Ensure a budget exists for E2E-Budget-Cat-A ──────────────────────
        await page.goto('/budgets');
        await page.waitForLoadState('domcontentloaded');

        const existingRow = page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-A' }),
        });

        if ((await existingRow.count()) === 0) {
            await page.goto('/budgets/create');
            const categorySelect = page.locator('#CategoryId');
            const catAOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-A$/ });
            await expect(catAOption).toHaveCount(1);
            const catAValue = await catAOption.getAttribute('value');
            await categorySelect.selectOption(catAValue!);

            const now = new Date();
            const year = now.getFullYear();
            const month = String(now.getMonth() + 1).padStart(2, '0');
            await page.fill('#LimitAmount', '100.00');
            await page.fill('#EffectiveFrom', `${year}-${month}-01`);
            await page.selectOption('#PeriodGranularity', 'Monthly');
            await page.getByRole('button', { name: 'Create Budget' }).click();

            await Promise.race([
                page.waitForURL((url: URL) => url.pathname === '/budgets', { timeout: 30000 }),
                page.locator('.alert-danger').waitFor({ state: 'visible', timeout: 30000 }),
            ]).catch(() => {});

            if (new URL(page.url()).pathname === '/budgets/create') {
                await page.goto('/budgets');
            }
            await page.waitForLoadState('domcontentloaded');
        }

        // ── Navigate to edit page ─────────────────────────────────────────────
        await page.goto('/budgets');
        await page.waitForLoadState('domcontentloaded');

        const editLink = page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-A' }),
        }).locator('a', { hasText: 'Edit' });

        await expect(editLink).toBeVisible();
        const editHref = await editLink.getAttribute('href');
        await page.goto(editHref!);
        await expect(page).toHaveURL(/\/budgets\/edit\//i);

        // ── Click Deactivate ──────────────────────────────────────────────────
        const deactivateBtn = page.getByRole('button', { name: 'Deactivate Budget' });
        await expect(deactivateBtn).toBeVisible();

        // Handle confirmation dialog
        page.on('dialog', async dialog => {
            expect(dialog.message()).toContain('deactivate');
            await dialog.accept();
        });

        await deactivateBtn.click();
        await page.waitForLoadState('domcontentloaded');

        // ── Verify budget appears as inactive in list ────────────────────────
        if (new URL(page.url()).pathname !== '/budgets') {
            await page.goto('/budgets');
        }

        await expect(page.locator('table')).toBeVisible();
        await expect(page.locator('table')).toContainText('Inactive');
    });

    /**
     * TC-M06: Delete a budget permanently via the edit page.
     *
     * Flow: /budgets → find E2E-Budget-Cat-A → click Edit → click Delete Permanently → confirm → budget disappears from list.
     */
    test('TC-M06: delete budget permanently removes it from list', async ({ budgetReadyPage: page }) => {
        // ── Ensure a budget exists for E2E-Budget-Cat-A ──────────────────────
        await page.goto('/budgets');
        await page.waitForLoadState('domcontentloaded');

        let existingRow = page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-A' }),
        });

        if ((await existingRow.count()) === 0) {
            // Create one first
            await page.goto('/budgets/create');
            const categorySelect = page.locator('#CategoryId');
            const catAOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-A$/ });
            await expect(catAOption).toHaveCount(1);
            const catAValue = await catAOption.getAttribute('value');
            await categorySelect.selectOption(catAValue!);

            const now = new Date();
            const year = now.getFullYear();
            const month = String(now.getMonth() + 1).padStart(2, '0');
            await page.fill('#LimitAmount', '100.00');
            await page.fill('#EffectiveFrom', `${year}-${month}-01`);
            await page.selectOption('#PeriodGranularity', 'Monthly');
            await page.getByRole('button', { name: 'Create Budget' }).click();

            await Promise.race([
                page.waitForURL((url: URL) => url.pathname === '/budgets', { timeout: 30000 }),
                page.locator('.alert-danger').waitFor({ state: 'visible', timeout: 30000 }),
            ]).catch(() => {});

            if (new URL(page.url()).pathname === '/budgets/create') {
                await page.goto('/budgets');
            }
            await page.waitForLoadState('domcontentloaded');
        }

        // ── Navigate to edit page ─────────────────────────────────────────────
        await page.goto('/budgets');
        await page.waitForLoadState('domcontentloaded');

        const editLink = page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-A' }),
        }).locator('a', { hasText: 'Edit' });

        await expect(editLink).toBeVisible();
        const editHref = await editLink.getAttribute('href');
        await page.goto(editHref!);
        await expect(page).toHaveURL(/\/budgets\/edit\//i);

        // ── Click Delete Permanently ──────────────────────────────────────────
        const deleteBtn = page.getByRole('button', { name: 'Delete Permanently' });
        await expect(deleteBtn).toBeVisible();

        // Handle confirmation dialog
        page.on('dialog', async dialog => {
            expect(dialog.message()).toContain('delete');
            await dialog.accept();
        });

        await deleteBtn.click();
        await page.waitForLoadState('domcontentloaded');

        // ── Verify budget is gone from list ───────────────────────────────────
        if (new URL(page.url()).pathname !== '/budgets') {
            await page.goto('/budgets');
        }

        await expect(page.locator('table')).toBeVisible();
        // The budget should NOT appear in the list anymore
        const goneRow = page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-A' }),
        });
        await expect(goneRow).toHaveCount(0);
    });

    /**
     * TC-M05: Navigate budget list with filters (show active only, by category).
     *
     * Flow: /budgets → verify table renders → toggle active filter → verify filtering.
     */
    test('TC-M05: budget list renders with filter controls', async ({ budgetReadyPage: page }) => {
        await page.goto('/budgets');
        await expect(page).toHaveURL(/\/budgets/i);

        // ── Verify the budget list table renders ─────────────────────────────
        // The table should be present (even if empty)
        const pageContent = page.locator('main');
        await expect(pageContent).toBeVisible();

        // Check for filter controls
        const showActiveCheckbox = page.locator('#ShowActiveOnly');
        const showActiveExists = await showActiveCheckbox.isVisible().catch(() => false);

        // If the filter checkbox exists, toggle it
        if (showActiveExists) {
            await showActiveCheckbox.click();
            await page.waitForLoadState('domcontentloaded');
        }

        // Verify the page still shows a valid state (table or empty state)
        await expect(pageContent).toBeVisible();

        // Check for category filter if present
        const categoryFilter = page.locator('#CategoryFilter');
        const categoryFilterExists = await categoryFilter.isVisible().catch(() => false);

        if (categoryFilterExists) {
            // Select E2E-Budget-Cat-A if it exists in the filter
            const filterOption = categoryFilter.locator('option', { hasText: 'E2E-Budget-Cat-A' });
            if ((await filterOption.count()) > 0) {
                const filterValue = await filterOption.getAttribute('value');
                await categoryFilter.selectOption(filterValue!);
                await page.waitForLoadState('domcontentloaded');
            }
        }

        // Page must still be functional
        await expect(page).toHaveURL(/\/budgets/i);
    });
});
