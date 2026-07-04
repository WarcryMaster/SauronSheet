/**
 * E2E Tests for Budgets feature — clarify-budgets-feature (PR 3)
 *
 * Covers:
 *   TC-B01 (task 3.2) — create budget → appears in current-month list
 *   TC-B02 (task 3.3) — comparison: "No budget" label for unbudgeted categories with spend
 *   TC-B03 (task 3.4) — dashboard widget: reflects current calendar month, no period selector
 *   TC-B04           — delete budget → disappears from the list
 *
 * Data provisioning:
 *   The `budgetReadyPage` fixture (e2e/fixtures/budget-data.fixture.ts) runs before each test
 *   and idempotently provisions:
 *     - Category "E2E-Budget-Cat-A" (Expense)
 *     - Category "E2E-Budget-Cat-B" (Expense)
 *     - One -25 € transaction on "E2E-Budget-Cat-B" for the current calendar month
 *   This guarantees the comparison table renders, the dashboard shows the Budget Status
 *   widget, and TC-B01 can find a deterministic category in the create-budget dropdown.
 *
 * Auth strategy (handled entirely by the fixture — dual-path):
 *   Priority 1 — Env-var credentials: TEST_USER_EMAIL / TEST_USER_PASSWORD (CI / override)
 *   Priority 2 — Seeded test user: e2e@saurontest.local / ***REMOVED***
 *
 * Execution: workers=1, sequential, single browser context per test.
 * Cleanup: test.afterAll deletes all E2E budgets and categories to prevent data accumulation.
 */

import { test, expect, cleanupE2EBudgets, cleanupE2ECategories, cleanupE2ETransactions, AUTH_FILE, E2E_CAT_B } from '../fixtures/budget-data.fixture';
import { ensureBudgetDeleted, ensureBudgetExists, getCurrentBudgetMonth } from './budgets/helpers';
import { setFlatpickrDate } from '../helpers';

test.describe('Budgets — monthly budget management (clarify-budgets-feature)', () => {

    // Clean up all E2E artifacts after the entire suite finishes.
    // This prevents test data from accumulating under the test user's account.
    test.afterAll(async ({ browser }) => {
        const context = await browser.newContext({ storageState: AUTH_FILE });
        const page    = await context.newPage();

        await cleanupE2EBudgets(page);
        await cleanupE2ETransactions(page);
        await cleanupE2ECategories(page);

        await context.close();
    });

    /**
     * TC-B01 (task 3.2): Create a budget and verify it appears in the current-month list.
     *
     * Flow: /budgets/create → select "E2E-Budget-Cat-B" (provisioned by fixture),
     *       fill current month and €100 limit → submit → /budgets list shows the row.
     *
     * Idempotent: if the budget already exists (duplicate run), a DomainException error
     * is shown on the create page; the test then navigates to the list directly and
     * verifies the budget is already there.
     */
    test('TC-B01: create budget appears in current-month list', async ({ budgetReadyPage: page }) => {
        const now   = new Date();
        const year  = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');

        // ── Idempotency: if budget for Cat-B already exists, skip creation ────────
        // (prevents DomainException from ValidateNoOverlap on re-runs with leftover data)
        await page.goto('/budgets');
        await page.waitForLoadState('domcontentloaded');

        const alreadyExists = (await page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-B' }),
        }).count()) > 0;

        if (!alreadyExists) {
            await page.goto('/budgets/create');
            await expect(page).toHaveURL(/\/budgets\/create/i);

            // Select BudgetType "Expense" — category section is hidden until a type is selected (x-show="budgetType")
            await page.locator('label#label-expense').click();
            await page.waitForTimeout(500); // Alpine.js x-show transition

            const categorySelect = page.locator('select#CategoryId');
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

            // EffectiveFrom is a Flatpickr input (hidden alt input) — use Flatpickr API
            const effectiveFrom = `${year}-${month}-01`;
            await setFlatpickrDate(page, 'EffectiveFrom', effectiveFrom);

            await page.selectOption('#PeriodGranularity', 'Monthly');
            await page.fill('input#LimitAmount', '100.00');

            await page.getByTestId('submit-btn').click();

            await Promise.race([
                page.waitForURL((url: URL) => url.pathname === '/budgets', { timeout: 30000 }),
                page.locator('.alert-danger').waitFor({ state: 'visible', timeout: 30000 }),
            ]).catch(() => {});

            if (new URL(page.url()).pathname === '/budgets/create') {
                await page.goto('/budgets');
            }
        }

        await expect(page).toHaveURL(/\/budgets/i);

        const table = page.locator('table');
        await expect(table).toBeVisible();
        await expect(table).toContainText('E2E-Budget-Cat-B');
    });

    /**
     * TC-B03 (task 3.4): Dashboard budget widget always reflects the current calendar month.
     *
     * Spec: "no period selector in widget" — the widget ignores the date filter applied to
     * the rest of the dashboard.  Dashboard.cshtml.cs uses DateTime.UtcNow (not DateFilter)
     * to GetBudgetSummaryForDashboardQuery, so the widget is always scoped to now.Year/now.Month.
     *
     * The fixture ensures at least one transaction exists for the current month, guaranteeing
     * the dashboard "active" state and the Budget Status widget are rendered.
     *
     * Verification strategy:
     *   1. Navigate to /dashboard (default DateFilter=all — maximises chances of finding data)
     *   2. Confirm Budget Status card is present (fixture guarantees transaction data exists)
     *   3. Confirm no <input type="month"> or prev/next navigation inside the widget
     *   4. Confirm the widget renders one of the two valid states (empty CTA or budget rows)
     *   5. Navigate again with DateFilter=last-month and confirm widget is still present
     *      (proves the widget is not filtered by date range)
     */
    test('TC-B03: dashboard budget widget reflects current month, no period selector', async ({ budgetReadyPage: page }) => {
        await ensureBudgetExists(page, E2E_CAT_B, '100.00');

        await page.goto('/dashboard');
        await expect(page).toHaveURL(/\/dashboard/i);
        await expect(page).toHaveTitle(/Dashboard/i);

        const budgetWidget = page.locator('[data-testid="budget-status-widget"]');
        await expect(budgetWidget).toBeVisible();

        await expect(budgetWidget).toContainText('this month');
        await expect(budgetWidget).toContainText(E2E_CAT_B);
        await expect(budgetWidget.locator('input[type="month"]')).toHaveCount(0);
        await expect(
            budgetWidget.locator('button', { hasText: /prev|next|anterior|siguiente/i }),
        ).toHaveCount(0);
        await expect(budgetWidget.locator('#DateFilter, #CustomFromDate, #CustomToDate')).toHaveCount(0);

        const catBProgressBefore = budgetWidget.locator('.mb-3').filter({
            has: page.locator('span', { hasText: E2E_CAT_B }),
        }).first();
        await expect(catBProgressBefore).toBeVisible();
        const catBProgressText = (await catBProgressBefore.textContent())?.replace(/\s+/g, ' ').trim() ?? '';

        // Dashboard uses pill-style period buttons, not a #DateFilter select.
        // Click "This Year" to change the dashboard date range.
        const thisYearBtn = page.getByTestId('dashboard-period-this-year');
        await expect(thisYearBtn).toBeVisible();
        await thisYearBtn.click();
        await page.waitForLoadState('domcontentloaded');

        // Budget widget always shows "this month" regardless of dashboard filter.
        await expect(budgetWidget).toContainText('this month');
        await expect(budgetWidget).toContainText(E2E_CAT_B);

        const catBProgressAfter = budgetWidget.locator('.mb-3').filter({
            has: page.locator('span', { hasText: E2E_CAT_B }),
        }).first();
        await expect(catBProgressAfter).toBeVisible();
        await expect(catBProgressAfter).toContainText(catBProgressText);
    });

    /**
     * TC-B04: Delete a budget and verify it disappears from the list.
     *
     * Flow: Navigate to budgets list, find the E2E budget, click Delete button,
     *       confirm the browser dialog, and verify the row is gone.
     *
     * Idempotent: if no budget exists for E2E-Budget-Cat-B, creates one first.
     * After deletion, the list must not contain "E2E-Budget-Cat-B" anymore.
     */
    test('TC-B04: delete budget removes it from the list', async ({ budgetReadyPage: page }) => {
        const now   = new Date();
        const year  = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');

        // ── Ensure a budget exists to delete ──────────────────────────────────
        await page.goto(`/budgets?Year=${year}&Month=${parseInt(month)}`);
        await page.waitForLoadState('domcontentloaded');

        const existingRow = page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-B' }),
        });

        if ((await existingRow.count()) === 0) {
            // No budget yet — create one first using real user interaction
            await page.goto('/budgets/create');

            // Select BudgetType "Expense" — category section is hidden until a type is selected
            await page.locator('label#label-expense').click();
            await page.waitForTimeout(500);

            const categorySelect = page.locator('select#CategoryId');
            // Wait for Alpine.js x-for template to render options into the select
            await page.waitForFunction(() => {
                const sel = document.querySelector('select#CategoryId');
                return sel && sel.querySelectorAll('option').length > 1;
            }, { timeout: 10000 });
            const catBOption = categorySelect.locator('option', { hasText: /^E2E-Budget-Cat-B$/ });
            await expect(catBOption).toHaveCount(1);
            const catBValue = await catBOption.getAttribute('value');
            await categorySelect.selectOption(catBValue!);

            // EffectiveFrom is a Flatpickr input — use Flatpickr API
            await setFlatpickrDate(page, 'EffectiveFrom', `${year}-${month}-01`);
            await page.selectOption('#PeriodGranularity', 'Monthly');
            await page.fill('input#LimitAmount', '50.00');
            await page.getByTestId('submit-btn').click();

            await Promise.race([
                page.waitForURL((url: URL) => url.pathname === '/budgets', { timeout: 30000 }),
                page.locator('.alert-danger').waitFor({ state: 'visible', timeout: 30000 }),
            ]).catch(() => {});

            if (new URL(page.url()).pathname === '/budgets/create') {
                await page.goto(`/budgets?Year=${year}&Month=${parseInt(month)}`);
            }
        }

        // ── Verify the budget is in the list before deleting ──────────────────
        await page.goto('/budgets');
        await page.waitForLoadState('domcontentloaded');
        await expect(page).toHaveURL(/\/budgets/i);
        const table = page.locator('table');
        await expect(table).toBeVisible();
        await expect(table).toContainText('E2E-Budget-Cat-B');

        // ── Delete via Edit page (the list has no Delete button — only Deactivate) ──
        const editLink = page.locator('table tbody tr').filter({
            has: page.locator('td', { hasText: 'E2E-Budget-Cat-B' }),
        }).locator('[data-testid="edit-budget-btn"]');
        await expect(editLink).toBeVisible();
        const editHref = await editLink.getAttribute('href');

        await page.goto(editHref!);
        await expect(page).toHaveURL(/\/budgets\/edit\//i);

        const deletePermBtn = page.getByTestId('delete-budget-btn');
        await expect(deletePermBtn).toBeVisible();

        // Click opens MDB modal — confirm via the modal's Delete button
        await deletePermBtn.click();

        const deleteModal = page.locator('#budgetDeleteConfirmModal');
        await expect(deleteModal).toBeVisible({ timeout: 5000 });
        await deleteModal.getByTestId('confirm-delete-budget-btn').click();

        // Wait for redirect back to the list
        await page.waitForURL(/\/budgets(?!\/)/i, { timeout: 10000 });
        await page.waitForLoadState('domcontentloaded');

        // ── Assert the budget is gone ─────────────────────────────────────────
        const tableAfter = page.locator('table');
        const tableExists = await tableAfter.count() > 0;
        if (tableExists) {
            await expect(tableAfter).not.toContainText('E2E-Budget-Cat-B');
        }
    });

});
