/**
 * E2E Tests for Annual Analysis Dashboard (annual-analysis-redesign PR #3)
 *
 * Covers the new dashboard layout:
 *   - 4 KPI cards (income, expense, net, fixed-cost percentage)
 *   - Monthly trend line chart and fixed/variable distribution donut chart
 *   - Year-over-Year comparison section with fallback message
 *   - Collapsible income/expense detail tables
 *   - Row expansion showing monthly mini-bars
 *   - Empty state for a year with no data
 *   - Year selector reloads the page with the selected year
 *
 * E2E Testing Policy (from AGENTS.md):
 *   - MUST act as real user: page.click(), page.fill(), page.selectOption()
 *   - Wait for elements to be visible before interacting
 *   - DO NOT use page.evaluate() to execute direct JavaScript (Flatpickr date input is the documented exception)
 *   - DO NOT use fetch() inside page.evaluate()
 */

import { test, expect } from '../fixtures/auth.fixture';
import { setFlatpickrDate } from '../helpers';

const currentYear = new Date().getFullYear();
const alternateYear = currentYear - 1;
const emptyYear = currentYear + 1;

async function seedAnnualData(page: any, year?: number): Promise<void> {
    const targetYear = year ?? currentYear;
    const testDate = `${targetYear}-01-15`;

    await page.goto('/transactions/add');
    await expect(page).toHaveURL(/\/transactions\/add/i);

    await setFlatpickrDate(page, 'Date', testDate);
    await page.fill('#Description', 'E2E Annual Income');
    await page.fill('#Amount', '100');
    await page.selectOption('#Currency', 'EUR');
    await page.fill('#CategoryName', 'E2E-Annual-Income');
    await page.getByRole('button', { name: 'Add Transaction' }).click();
    await page.waitForURL(/\/transactions/i, { timeout: 10000 });

    await page.goto('/transactions/add');
    await expect(page).toHaveURL(/\/transactions\/add/i);

    await setFlatpickrDate(page, 'Date', testDate);
    await page.fill('#Description', 'E2E Annual Expense');
    await page.fill('#Amount', '-50');
    await page.selectOption('#Currency', 'EUR');
    await page.fill('#CategoryName', 'E2E-Annual-Expense');
    await page.getByRole('button', { name: 'Add Transaction' }).click();
    await page.waitForURL(/\/transactions/i, { timeout: 10000 });
}

test.describe('Annual Analysis Dashboard', () => {
    test('renders dashboard layout with KPIs, charts and YoY section', async ({ authenticatedPage: page }) => {
        await seedAnnualData(page);
        await page.goto(`/Analysis/Annual?Year=${currentYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        await expect(page.locator('[data-testid="annual-kpi-income"]')).toBeVisible();
        await expect(page.locator('[data-testid="annual-kpi-expense"]')).toBeVisible();
        await expect(page.locator('[data-testid="annual-kpi-net"]')).toBeVisible();
        await expect(page.locator('[data-testid="annual-kpi-fixed-pct"]')).toBeVisible();

        const trendChart = page.locator('[data-testid="annual-trend-chart"]');
        await expect(trendChart).toBeVisible();
        await expect(trendChart).toHaveAttribute('role', 'img');

        const distributionChart = page.locator('[data-testid="annual-distribution-chart"]');
        await expect(distributionChart).toBeVisible();
        await expect(distributionChart).toHaveAttribute('role', 'img');

        await expect(page.locator('[data-testid="annual-yoy-section"]')).toBeVisible();
        await expect(page.locator('[data-testid="annual-detail-toggle"]')).toBeVisible();
    });

    test('shows empty state when no data and hides dashboard elements', async ({ authenticatedPage: page }) => {
        await page.goto(`/Analysis/Annual?Year=${emptyYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const emptyState = page.locator('[data-testid="annual-empty-state"]');
        await expect(emptyState).toBeVisible();
        await expect(emptyState).toContainText('Sin datos para este año');

        await expect(page.locator('[data-testid="annual-kpi-income"]')).toHaveCount(0);
        await expect(page.locator('[data-testid="annual-kpi-expense"]')).toHaveCount(0);
        await expect(page.locator('[data-testid="annual-kpi-net"]')).toHaveCount(0);
        await expect(page.locator('[data-testid="annual-kpi-fixed-pct"]')).toHaveCount(0);

        await expect(page.locator('[data-testid="annual-trend-chart"]')).toHaveCount(0);
        await expect(page.locator('[data-testid="annual-distribution-chart"]')).toHaveCount(0);
        await expect(page.locator('[data-testid="annual-yoy-section"]')).toHaveCount(0);
        await expect(page.locator('[data-testid="annual-detail-toggle"]')).toHaveCount(0);
    });

    test('detail tables toggle shows and hides both tables', async ({ authenticatedPage: page }) => {
        await seedAnnualData(page);
        await page.goto(`/Analysis/Annual?Year=${currentYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const toggle = page.locator('[data-testid="annual-detail-toggle"]');
        await expect(toggle).toBeVisible();
        await expect(toggle).toHaveAttribute('aria-expanded', 'false');

        const incomeTable = page.locator('[data-testid="annual-income-table"]');
        const expenseTable = page.locator('[data-testid="annual-expense-table"]');

        await expect(incomeTable).toBeHidden();
        await expect(expenseTable).toBeHidden();

        await toggle.click();

        await expect(toggle).toHaveAttribute('aria-expanded', 'true');
        await expect(incomeTable).toBeVisible();
        await expect(expenseTable).toBeVisible();

        await toggle.click();

        await expect(toggle).toHaveAttribute('aria-expanded', 'false');
        await expect(incomeTable).toBeHidden();
        await expect(expenseTable).toBeHidden();
    });

    test('row expansion shows monthly mini-bars with month labels', async ({ authenticatedPage: page }) => {
        await seedAnnualData(page);
        await page.goto(`/Analysis/Annual?Year=${currentYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        await page.locator('[data-testid="annual-detail-toggle"]').click();

        const incomeTable = page.locator('[data-testid="annual-income-table"]');
        await expect(incomeTable).toBeVisible();

        const firstRow = incomeTable.locator('tbody tr[role="button"]').first();
        await expect(firstRow).toBeVisible();
        await expect(firstRow).toHaveAttribute('aria-expanded', 'false');

        await firstRow.click();
        await expect(firstRow).toHaveAttribute('aria-expanded', 'true');

        const expansionRow = incomeTable.locator('tbody tr').nth(1);
        await expect(expansionRow).toBeVisible();
        await expect(expansionRow.locator('.mini-bar').first()).toBeVisible();

        const monthLabels = ['E', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D'];
        for (const label of monthLabels) {
            await expect(expansionRow).toContainText(label);
        }
    });

    test('year selector changes year and reloads page', async ({ authenticatedPage: page }) => {
        await page.goto(`/Analysis/Annual?Year=${currentYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const yearSelect = page.locator('#Year');
        await expect(yearSelect).toBeVisible();

        await yearSelect.selectOption(String(alternateYear));
        await expect(page).toHaveURL(new RegExp(`\\/Analysis\\/Annual\\?year=${alternateYear}`, 'i'));
        await expect(yearSelect).toHaveValue(String(alternateYear));
    });

    test('YoY section renders fallback when no previous year data', async ({ authenticatedPage: page }) => {
        // Use a far-future year to guarantee no previous-year data exists in the database.
        // CI-persisted data from previous runs (when currentYear was 2025) would otherwise
        // cause hasVariation=true, hiding the fallback.
        const guaranteedCleanYear = currentYear + 50;
        await seedAnnualData(page, guaranteedCleanYear);
        await page.goto(`/Analysis/Annual?Year=${guaranteedCleanYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const yoySection = page.locator('[data-testid="annual-yoy-section"]');
        await expect(yoySection).toBeVisible();

        const noDataFallback = page.locator('[data-testid="annual-yoy-no-data"]');
        await expect(noDataFallback).toBeVisible();
        await expect(noDataFallback).toContainText('Sin datos del año anterior');
    });
});
