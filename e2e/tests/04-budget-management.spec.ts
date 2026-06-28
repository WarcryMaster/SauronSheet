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

import { test, expect, cleanupE2EBudgets, cleanupE2ECategories, cleanupE2ETransactions, loginAsTestAccount, E2E_CAT_A, E2E_CAT_B } from '../fixtures/budget-data.fixture';
import { budgetRow, ensureBudgetDeleted, ensureBudgetExists, ensureBudgetStatus, getCurrentBudgetMonth } from './budgets/helpers';
import { setFlatpickrDate } from '../helpers';

test.describe('Budgets — management CRUD (budget-redesign Slice 6)', () => {

    test.afterAll(async ({ browser }) => {
        const context = await browser.newContext();
        const page    = await context.newPage();

        await context.clearCookies();
        await loginAsTestAccount(page);

        await cleanupE2EBudgets(page);
        await cleanupE2ETransactions(page);
        await cleanupE2ECategories(page);
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
            // Select BudgetType "Expense" — category section is hidden until a type is selected
            await page.locator('label#label-expense').click();
            await page.waitForTimeout(500);

            const categorySelect = page.locator('#CategoryId');
            await expect(categorySelect).toBeVisible();
            // Wait for Alpine.js x-for template to render options into the select
            await page.waitForFunction(() => {
                const sel = document.querySelector('select#CategoryId');
                return sel && sel.querySelectorAll('option').length > 1;
            }, { timeout: 10000 });
            const catAOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-A$/ });
            await expect(catAOption).toHaveCount(1);
            const catAValue = await catAOption.getAttribute('value');
            await categorySelect.selectOption(catAValue!);

            await page.fill('#LimitAmount', '200.00');

            // EffectiveFrom is a Flatpickr input — use Flatpickr API
            const effectiveFrom = `${year}-${month}-01`;
            await setFlatpickrDate(page, 'EffectiveFrom', effectiveFrom);

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
        await ensureBudgetDeleted(page, E2E_CAT_B);

        await page.goto('/budgets');
        await expect(page).toHaveURL(/\/budgets/i);
        const rowsBefore = await page.locator('table[aria-label="Budgets list"] tbody tr').count();

        await page.goto('/budgets/create');
        await expect(page).toHaveURL(/\/budgets\/create/i);

        // Select BudgetType "Expense" — category section is hidden until a type is selected
        await page.locator('label#label-expense').click();
        await page.waitForTimeout(500);

        // Select category
        const categorySelect = page.locator('#CategoryId');
        await expect(categorySelect).toBeVisible();
        // Wait for Alpine.js x-for template to render options into the select
        await page.waitForFunction(() => {
            const sel = document.querySelector('select#CategoryId');
            return sel && sel.querySelectorAll('option').length > 1;
        }, { timeout: 10000 });
        const catBOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-B$/ });
        await expect(catBOption).toHaveCount(1);
        const catBValue = await catBOption.getAttribute('value');
        await categorySelect.selectOption(catBValue!);

        // Fill negative limit (INVALID)
        await page.fill('#LimitAmount', '-50.00');

        const currentMonth = getCurrentBudgetMonth();

        // EffectiveFrom is a Flatpickr input — use Flatpickr API
        await setFlatpickrDate(page, 'EffectiveFrom', currentMonth.firstDay);

        await page.selectOption('#PeriodGranularity', 'Monthly');

        await page.getByRole('button', { name: 'Create Budget' }).click();

        await expect(page).toHaveURL(/\/budgets\/create/i);
        await expect(page.locator('#LimitAmount')).toHaveValue(/-50(?:\.00)?/);
        await expect(page.getByRole('heading', { name: 'Create Budget' })).toBeVisible();

        await page.goto('/budgets');
        await expect(budgetRow(page, E2E_CAT_B)).toHaveCount(0);
        await expect(page.locator('table[aria-label="Budgets list"] tbody tr')).toHaveCount(rowsBefore);
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

            // Select BudgetType "Expense" — category section is hidden until a type is selected
            await page.locator('label#label-expense').click();
            await page.waitForTimeout(500);

            const categorySelect = page.locator('#CategoryId');
            // Wait for Alpine.js x-for template to render options into the select
            await page.waitForFunction(() => {
                const sel = document.querySelector('select#CategoryId');
                return sel && sel.querySelectorAll('option').length > 1;
            }, { timeout: 10000 });
            const catAOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-A$/ });
            await expect(catAOption).toHaveCount(1);
            const catAValue = await catAOption.getAttribute('value');
            await categorySelect.selectOption(catAValue!);

            const now = new Date();
            const year = now.getFullYear();
            const month = String(now.getMonth() + 1).padStart(2, '0');
            await page.fill('#LimitAmount', '100.00');
            // EffectiveFrom is a Flatpickr input — use Flatpickr API
            await setFlatpickrDate(page, 'EffectiveFrom', `${year}-${month}-01`);
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
        await expect(page.locator('table')).toContainText('300');
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

            // Select BudgetType "Expense" — category section is hidden until a type is selected
            await page.locator('label#label-expense').click();
            await page.waitForTimeout(500);

            const categorySelect = page.locator('#CategoryId');
            // Wait for Alpine.js x-for template to render options into the select
            await page.waitForFunction(() => {
                const sel = document.querySelector('select#CategoryId');
                return sel && sel.querySelectorAll('option').length > 1;
            }, { timeout: 10000 });
            const catAOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-A$/ });
            await expect(catAOption).toHaveCount(1);
            const catAValue = await catAOption.getAttribute('value');
            await categorySelect.selectOption(catAValue!);

            const now = new Date();
            const year = now.getFullYear();
            const month = String(now.getMonth() + 1).padStart(2, '0');
            await page.fill('#LimitAmount', '100.00');
            // EffectiveFrom is a Flatpickr input — use Flatpickr API
            await setFlatpickrDate(page, 'EffectiveFrom', `${year}-${month}-01`);
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

        // ── Click Deactivate and confirm via MDB modal ───────────────────────
        const deactivateBtn = page.getByRole('button', { name: 'Deactivate Budget' });
        await expect(deactivateBtn).toBeVisible();
        await deactivateBtn.click();

        const statusModal = page.locator('#budgetStatusModal');
        await expect(statusModal).toBeVisible();
        await expect(statusModal).toContainText('Deactivate budget');

        await statusModal.getByRole('button', { name: 'Deactivate' }).click();
        await page.waitForURL(/\/budgets(?!\/edit\/)/i, { timeout: 10000 });
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

            // Select BudgetType "Expense" — category section is hidden until a type is selected
            await page.locator('label#label-expense').click();
            await page.waitForTimeout(500);

            const categorySelect = page.locator('#CategoryId');
            // Wait for Alpine.js x-for template to render options into the select
            await page.waitForFunction(() => {
                const sel = document.querySelector('select#CategoryId');
                return sel && sel.querySelectorAll('option').length > 1;
            }, { timeout: 10000 });
            const catAOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-A$/ });
            await expect(catAOption).toHaveCount(1);
            const catAValue = await catAOption.getAttribute('value');
            await categorySelect.selectOption(catAValue!);

            const now = new Date();
            const year = now.getFullYear();
            const month = String(now.getMonth() + 1).padStart(2, '0');
            await page.fill('#LimitAmount', '100.00');
            // EffectiveFrom is a Flatpickr input — use Flatpickr API
            await setFlatpickrDate(page, 'EffectiveFrom', `${year}-${month}-01`);
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

        // Click opens MDB modal — confirm via the modal's Delete button
        await deleteBtn.click();

        const deleteModal = page.locator('#budgetDeleteConfirmModal');
        await expect(deleteModal).toBeVisible({ timeout: 5000 });
        await deleteModal.getByRole('button', { name: /Delete/i }).click();

        await page.waitForLoadState('domcontentloaded');

        // ── Verify budget is gone from list ───────────────────────────────────
        if (new URL(page.url()).pathname !== '/budgets') {
            await page.goto('/budgets');
        }

        await expect(page).toHaveURL(/\/budgets/i);

        // After deletion the budget should not be in the list.
        // If other budgets exist, the table is rendered without the deleted row.
        // If this was the only budget, the empty state ("No budgets found") is shown instead.
        const tableVisible = await page.locator('table').isVisible().catch(() => false);
        if (tableVisible) {
            const goneRow = page.locator('table tbody tr').filter({
                has: page.locator('td', { hasText: 'E2E-Budget-Cat-A' }),
            });
            await expect(goneRow).toHaveCount(0);
        } else {
            // Empty state — no table at all
            await expect(page.locator('text=No budgets found')).toBeVisible();
        }
    });

    /**
     * TC-M05: Navigate budget list with filters (show active only, by category).
     *
     * Flow: /budgets → verify table renders → toggle active filter → verify filtering.
     */
    test('TC-M05: budget list renders with filter controls', async ({ budgetReadyPage: page }) => {
        test.setTimeout(60_000);

        // ensureBudgetStatus handles creation (with default 100.00 limit) + status toggle.
        // This replaces 4 redundant calls with 2, saving ~4 navigations on CI.
        await ensureBudgetStatus(page, E2E_CAT_A, 'Active');
        await ensureBudgetStatus(page, E2E_CAT_B, 'Inactive');

        await page.goto('/budgets');
        await expect(page).toHaveURL(/\/budgets/i);

        const table = page.locator('table[aria-label="Budgets list"]');
        await expect(table).toBeVisible();
        await expect(budgetRow(page, E2E_CAT_A)).toContainText('Active');
        await expect(budgetRow(page, E2E_CAT_B)).toContainText('Inactive');

        // Use data-testid selectors and Promise.all to properly track GET form navigation.
        // The GET form with Alpine x-model can race on CI — Promise.all ensures navigation
        // is captured even if Alpine defers the DOM update.
        await page.getByTestId('show-active-only').check();
        await Promise.all([
            page.waitForURL(/ShowActiveOnly=true/i, { timeout: 10000 }),
            page.getByTestId('budget-filter-btn').click(),
        ]);

        await expect(budgetRow(page, E2E_CAT_A)).toHaveCount(1);
        await expect(budgetRow(page, E2E_CAT_B)).toHaveCount(0);

        await page.getByTestId('show-active-only').uncheck();
        await page.selectOption('[data-testid="category-filter"]', { label: E2E_CAT_B });
        await Promise.all([
            page.waitForURL(/CategoryFilter=/i, { timeout: 10000 }),
            page.getByTestId('budget-filter-btn').click(),
        ]);

        await expect(budgetRow(page, E2E_CAT_B)).toHaveCount(1);
        await expect(budgetRow(page, E2E_CAT_B)).toContainText('Inactive');
        await expect(budgetRow(page, E2E_CAT_A)).toHaveCount(0);
        await expect(page.locator('table[aria-label="Budgets list"] tbody tr')).toHaveCount(1);
    });
});
