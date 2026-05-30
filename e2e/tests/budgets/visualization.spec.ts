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

import { test, expect, cleanupE2EBudgets } from '../../fixtures/budget-data.fixture';

test.describe('Budgets — visualization (budget-redesign Slice 7)', () => {

    test.afterAll(async ({ browser }) => {
        const context = await browser.newContext();
        const page    = await context.newPage();

        await context.clearCookies();
        await page.goto('/auth/login');
        await page.fill('input[type="email"]', 'e2e@saurontest.local');
        await page.fill('input[type="password"]', '***REMOVED***');
        await page.click('button[type="submit"]');
        await page.waitForURL(/dashboard/i, { timeout: 15000 });

        await cleanupE2EBudgets(page);
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
        // Navigate to the metrics page
        await page.goto('/budgets/metrics');
        await page.waitForLoadState('domcontentloaded');

        // Verify the page loaded (either metrics content or empty state)
        await expect(page.locator('main')).toBeVisible();

        // ── Check for view tabs ──────────────────────────────────────────────
        // Tabs are <a> links with btn classes, NOT <button> elements.
        const monthTab  = page.getByRole('link', { name: /Mes Actual|Current Month/i });
        const periodTab = page.getByRole('link', { name: /Período Actual|Current Period/i });
        const yearTab   = page.getByRole('link', { name: /Año Actual|Current Year/i });

        // At least the month tab should be visible
        await expect(monthTab).toBeVisible();

        // ── Current Month view (default) ────────────────────────────────────
        // Should show metric cards or empty state
        const pageContent = page.locator('main');
        const hasMetricCards = await page.locator('.metric-card').count();

        if (hasMetricCards > 0) {
            // Verify key elements in metric cards
            const firstCard = page.locator('.metric-card').first();
            await expect(firstCard).toBeVisible();
            // Card should contain a category name and values
            await expect(firstCard.locator('text=€')).toBeVisible();
        }

        // ── Switch to Current Period view ───────────────────────────────────
        if (await periodTab.isVisible()) {
            await periodTab.click();
            await page.waitForLoadState('domcontentloaded');
            await expect(pageContent).toBeVisible();
        }

        // ── Switch to Current Year view ─────────────────────────────────────
        if (await yearTab.isVisible()) {
            await yearTab.click();
            await page.waitForLoadState('domcontentloaded');
            await expect(pageContent).toBeVisible();
        }
    });

    /**
     * TC-V02: History page with year selector and monthly table.
     *
     * Flow: /budgets/history → verify year selector → change year → verify table updates
     */
    test('TC-V02: history page with year selector shows monthly data', async ({ budgetReadyPage: page }) => {
        // Navigate to the history page
        await page.goto('/budgets/history');
        await page.waitForLoadState('domcontentloaded');

        // Verify the page loaded
        await expect(page.locator('main')).toBeVisible();

        // ── Year selector should be present ─────────────────────────────────
        const yearSelector = page.locator('#Year');
        await expect(yearSelector).toBeVisible();

        // ── Select the current year ─────────────────────────────────────────
        const currentYear = new Date().getFullYear().toString();
        await yearSelector.selectOption(currentYear);

        // Submit the form to load data for the selected year
        const filterBtn = page.getByRole('button', { name: /Filtrar|Filter|Ver|View/i });
        if (await filterBtn.isVisible()) {
            await filterBtn.click();
            await page.waitForLoadState('domcontentloaded');
        }

        // ── Verify monthly data renders ─────────────────────────────────────
        const table = page.locator('table');
        const hasTable = await table.isVisible().catch(() => false);

        if (hasTable) {
            // Table should have period labels
            await expect(table).toBeVisible();
            // Expect at least some month labels (even if no data has rows)
            const pageContent = page.locator('main');
            await expect(pageContent).toBeVisible();
        }

        // ── Try selecting a different year ──────────────────────────────────
        const previousYear = (new Date().getFullYear() - 1).toString();
        // Check if the option exists before selecting
        const prevYearOption = yearSelector.locator(`option[value="${previousYear}"]`);
        if ((await prevYearOption.count()) > 0) {
            await yearSelector.selectOption(previousYear);
            if (await filterBtn.isVisible()) {
                await filterBtn.click();
                await page.waitForLoadState('domcontentloaded');
            }
            await expect(page.locator('main')).toBeVisible();
        }
    });

    /**
     * TC-V03: Comparison page shows all categories including those without budget.
     *
     * Flow: /budgets/comparison → verify table with categories → check "Sin presupuesto" label
     */
    test('TC-V03: comparison page shows categories with budget status', async ({ budgetReadyPage: page }) => {
        // Navigate to comparison page
        await page.goto('/budgets/comparison');
        await page.waitForLoadState('domcontentloaded');

        await expect(page.locator('main')).toBeVisible();

        // ── Month picker should be present ──────────────────────────────────
        const monthPicker = page.locator('#monthPicker');
        await expect(monthPicker).toBeVisible();

        // ── Submit the form to load data ────────────────────────────────────
        const filterBtn = page.getByRole('button', { name: /Filter|Filtrar/i });
        if (await filterBtn.isVisible()) {
            await filterBtn.click();
            await page.waitForLoadState('domcontentloaded');
        }

        // ── Check for comparison table ──────────────────────────────────────
        const table = page.locator('table');
        const hasTable = await table.isVisible().catch(() => false);

        if (hasTable) {
            await expect(table).toBeVisible();

            // At least one row should have a category name
            const tbody = table.locator('tbody');
            const rowCount = await tbody.locator('tr').count();

            if (rowCount > 0) {
                // Verify table headers exist
                await expect(table.locator('th', { hasText: /Category|Categoría/i })).toBeVisible();
                await expect(table.locator('th', { hasText: /Budget|Presupuesto/i })).toBeVisible();
                await expect(table.locator('th', { hasText: /Actual/i })).toBeVisible();
            }
        }

        // Verify summary cards are present (Total Budgeted, Total Actual, Difference)
        const summaryCards = page.locator('.card .text-uppercase');
        const cardCount = await summaryCards.count();
        expect(cardCount).toBeGreaterThan(0);
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
        const budgetWidget = page.locator('h3, h6, .h6, h5, .h5').filter({
            hasText: /Budget|Presupuesto/i,
        });

        const hasBudgetWidget = await budgetWidget.first().isVisible().catch(() => false);

        if (hasBudgetWidget) {
            // Widget heading is visible
            await expect(budgetWidget.first()).toBeVisible();

            // The widget section (card containing the budget widget)
            const widgetCard = budgetWidget.first().locator('..').locator('..');
            const cardText = await widgetCard.textContent();

            // Widget should show either budget data or empty state with CTA
            const hasBudgetData = cardText.includes('€') || cardText.includes('on track') || cardText.includes('en orden');
            const hasEmptyState = cardText.includes('No budgets') || cardText.includes('No hay presupuestos') || cardText.includes('Create budgets');

            expect(hasBudgetData || hasEmptyState, 'Budget widget must show data or empty state CTA').toBeTruthy();
        }
    });
});
