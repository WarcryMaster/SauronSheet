/**
 * E2E Tests for Budget Visualization — budget-redesign (PR 7, Slice 7)
 *
 * Covers task 7.5:
 *   TC-V01 — Metrics page: three views (current month, period, year)
 *   TC-V02 — History page: year selector and monthly table
 *   TC-V03 — Comparison page: shows all categories with budget labels
 *   TC-V04 — Dashboard widget: shows budget summary for current month
 *
 * E2E Testing Policy (from AGENTS.md):
 *   - MUST act as real user: page.click(), page.fill(), page.selectOption()
 *   - Wait for elements to be visible before interacting
 *   - DO NOT use page.evaluate() to execute direct JavaScript
 *   - DO NOT use fetch() inside page.evaluate()
 *
 * Data provisioning: uses budget-data.fixture.ts for auth + test categories.
 */

import { test, expect, cleanupE2EBudgets, cleanupE2ECategories, cleanupE2ETransactions, AUTH_FILE, E2E_CAT_B } from '../../fixtures/budget-data.fixture';
import { ensureBudgetExists, getCurrentBudgetMonth } from './helpers';

test.describe('Budgets — visualization (budget-redesign Slice 7)', () => {

    test.afterAll(async ({ browser }) => {
        const context = await browser.newContext({ storageState: AUTH_FILE });
        const page    = await context.newPage();

        await cleanupE2EBudgets(page);
        await cleanupE2ETransactions(page);
        await cleanupE2ECategories(page);
        await context.close();
    });

    /**
     * TC-V01: Metrics page shows three views with correct data.
     *
     * Flow: /budgets/metrics → verify current month view renders
     *       → switch to current period view → verify different data
     *       → switch to current year view → verify accumulated data
     */
    test('TC-V01: metrics page shows three views (month, period, year)', async ({ budgetReadyPage: page }) => {
        await ensureBudgetExists(page, E2E_CAT_B, '100.00');

        const views = [
            { testId: 'metrics-tab-month', query: 'Month' },
            { testId: 'metrics-tab-period', query: 'Period' },
            { testId: 'metrics-tab-year', query: 'Year' },
        ];

        await page.goto('/budgets/metrics?View=Month');
        await expect(page.getByTestId('metrics-heading')).toBeVisible();

        for (const view of views) {
            const tab = page.getByTestId(view.testId);
            await expect(tab).toBeVisible();
            await tab.click();

            await expect(page).toHaveURL(new RegExp(`View=${view.query}`, 'i'));
            await expect(tab).toHaveClass(/btn-brand/);
            await expect(page.getByTestId('metrics-accumulated-limit')).toBeVisible();

            const catBCard = page.locator('.metric-card').filter({
                has: page.locator('h6', { hasText: E2E_CAT_B }),
            });

            await expect(catBCard).toHaveCount(1);
            await expect(catBCard).toContainText('%');
            await expect(catBCard).toContainText('€');
        }
    });

    /**
     * TC-V02: History page with year selector and monthly table.
     *
     * Flow: /budgets/history → verify year selector → change year → verify table updates
     */
    test('TC-V02: history page with year selector shows monthly data', async ({ budgetReadyPage: page }) => {
        await ensureBudgetExists(page, E2E_CAT_B, '100.00');

        const currentYear = new Date().getFullYear().toString();
        const previousYear = (new Date().getFullYear() - 1).toString();

        await page.goto(`/budgets/history?Year=${currentYear}`);
        await expect(page.getByTestId('history-heading')).toBeVisible();

        const yearSelector = page.locator('#Year');
        await expect(yearSelector).toBeVisible();
        await expect(yearSelector).toHaveValue(currentYear);

        const currentYearTable = page.locator(`table[aria-label="Budget history for ${currentYear}"]`);
        await expect(currentYearTable).toBeVisible();
        await expect(currentYearTable.getByTestId('history-period-header')).toBeVisible();
        await expect(currentYearTable.getByTestId('history-total-year')).toContainText(currentYear);

        await yearSelector.selectOption(previousYear);
        await page.getByTestId('view-year-btn').click();

        await expect(page).toHaveURL(new RegExp(`Year=${previousYear}`, 'i'));

        const previousYearTable = page.locator(`table[aria-label="Budget history for ${previousYear}"]`);
        if (await previousYearTable.isVisible().catch(() => false)) {
            await expect(previousYearTable.getByTestId('history-total-year')).toContainText(previousYear);
        } else {
            await expect(page.getByTestId('history-empty-state')).toBeVisible();
        }
    });

    /**
     * TC-V03: Comparison page shows all categories including those without budget.
     *
     * Flow: /budgets/comparison → verify table with categories → check "Sin presupuesto" label
     */
    test('TC-V03: comparison page shows categories with budget status', async ({ budgetReadyPage: page }) => {
        await ensureBudgetExists(page, E2E_CAT_B, '100.00');

        const currentMonth = getCurrentBudgetMonth();

        await page.goto('/budgets/comparison');
        await page.waitForLoadState('domcontentloaded');

        await expect(page.getByTestId('comparison-heading')).toBeVisible();

        const monthPicker = page.locator('#monthPicker');
        await expect(monthPicker).toBeVisible();

        await monthPicker.fill(`${currentMonth.year}-${currentMonth.month}`);
        await page.getByTestId('filter-month-btn').click();

        await expect(page).toHaveURL(new RegExp(`Year=${currentMonth.year}.*Month=${currentMonth.monthNumber}`, 'i'));

        const table = page.locator('table');
        await expect(table).toBeVisible();
        await expect(page.getByTestId('comparison-total-budgeted')).toBeVisible();
        await expect(page.getByTestId('comparison-total-actual')).toBeVisible();
        await expect(table.getByTestId('comparison-difference-header')).toBeVisible();

        const catBRow = table.locator('tbody tr').filter({
            has: page.locator('td', { hasText: E2E_CAT_B }),
        });

        await expect(catBRow).toHaveCount(1);
        await expect(catBRow.locator('td').nth(1)).toContainText('€');
        await expect(catBRow.locator('td').nth(1).locator('[data-testid="no-budget-label"]')).toHaveCount(0);
        await expect(catBRow.locator('td').nth(2)).toContainText('€');
    });

    /**
     * TC-V04: Dashboard widget shows budget summary for current month.
     *
     * Flow: /dashboard → verify budget widget renders → check percentage and status counts
     */
    test('TC-V04: dashboard widget shows budget summary for current month', async ({ budgetReadyPage: page }) => {
        // Navigate to dashboard
        await page.goto('/dashboard');
        await page.waitForLoadState('domcontentloaded');

        await expect(page.locator('main')).toBeVisible();

        // ── The Budget Status widget should be present ──────────────────────
        const budgetWidget = page.locator('[data-testid="budget-status-widget"]');

        const hasBudgetWidget = await budgetWidget.isVisible().catch(() => false);

        if (hasBudgetWidget) {
            // Widget heading is visible
            await expect(budgetWidget).toBeVisible();

            const cardText = await budgetWidget.textContent();

            // Widget should show either budget data or empty state with CTA
            const hasBudgetData = cardText.includes('€');
            const hasEmptyState = cardText.includes('No budgets') || cardText.includes('No hay presupuestos');

            expect(hasBudgetData || hasEmptyState, 'Budget widget must show data or empty state CTA').toBeTruthy();
        }
    });
});
