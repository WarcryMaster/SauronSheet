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
const invalidFutureYear = currentYear + 1;

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

    test('invalid year redirects to latest available year with data', async ({ authenticatedPage: page }) => {
        await seedAnnualData(page, alternateYear);

        await page.goto(`/Analysis/Annual?Year=${invalidFutureYear}`);
        await expect(page).toHaveURL(new RegExp(`/Analysis/Annual\\?Year=${alternateYear}$`, 'i'));

        await expect(page.locator('[data-testid="annual-kpi-income"]')).toBeVisible();
        await expect(page.locator('[data-testid="annual-kpi-expense"]')).toBeVisible();
        await expect(page.locator('[data-testid="annual-kpi-net"]')).toBeVisible();
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
        await seedAnnualData(page, alternateYear);
        await page.goto(`/Analysis/Annual?Year=${currentYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const yearSelect = page.locator('#Year');
        await expect(yearSelect).toBeVisible();

        await yearSelect.selectOption(String(alternateYear));
        await expect(yearSelect).toHaveValue(String(alternateYear));
        await expect(page.locator('[data-testid="annual-kpi-income"]')).toBeVisible();
    });

    test('YoY section renders fallback when no previous year data', async ({ authenticatedPage: page }) => {
        await seedAnnualData(page, currentYear);
        await page.goto(`/Analysis/Annual?Year=${currentYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const yearSelect = page.locator('#Year');
        await expect(yearSelect).toBeVisible();

        const earliestYear = await yearSelect.locator('option').first().getAttribute('value');
        if (earliestYear) {
            await yearSelect.selectOption(earliestYear);
            await expect(yearSelect).toHaveValue(earliestYear);
        }

        const yoySection = page.locator('[data-testid="annual-yoy-section"]');
        await expect(yoySection).toBeVisible();

        const noDataFallback = page.locator('[data-testid="annual-yoy-no-data"]');
        await expect(noDataFallback).toBeVisible();
        await expect(noDataFallback).toContainText('Sin datos del año anterior');
    });

    // ── T2 E2E Tests (Task 5.7) ──

    test('multi-year chart section renders with canvas element when multiple years of data exist', async ({ authenticatedPage: page }) => {
        // Seed data across 2 years to trigger multi-year comparison (requires 2+ years)
        const firstYear = currentYear - 4;
        const secondYear = currentYear - 3;
        await seedAnnualData(page, firstYear);
        await seedAnnualData(page, secondYear);
        await page.goto(`/Analysis/Annual?Year=${secondYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const section = page.locator('[data-testid="annual-multiyear-section"]');
        await expect(section).toBeVisible();

        const canvas = page.locator('[data-testid="annual-multiyear-chart"]');
        await expect(canvas).toBeVisible();
        await expect(canvas).toHaveAttribute('role', 'img');
    });

    test('monthly evolution chart renders with canvas', async ({ authenticatedPage: page }) => {
        const testYear = currentYear - 2;
        await seedAnnualData(page, testYear);
        await page.goto(`/Analysis/Annual?Year=${testYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const section = page.locator('[data-testid="annual-monthly-section"]');
        await expect(section).toBeVisible();

        const canvas = page.locator('[data-testid="annual-monthly-chart"]');
        await expect(canvas).toBeVisible();
        await expect(canvas).toHaveAttribute('role', 'img');
    });

    test('category distribution donut renders with category section', async ({ authenticatedPage: page }) => {
        const testYear = currentYear - 5;
        await seedAnnualData(page, testYear);
        await page.goto(`/Analysis/Annual?Year=${testYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const section = page.locator('[data-testid="annual-category-section"]');
        await expect(section).toBeVisible();

        const canvas = page.locator('[data-testid="annual-category-chart"]');
        await expect(canvas).toBeVisible();
    });

    test('timeline and top movements sections visible with transaction data', async ({ authenticatedPage: page }) => {
        const testYear = currentYear - 6;
        // Seed twice to create enough transactions for meaningful timeline + top movements
        await seedAnnualData(page, testYear);
        await seedAnnualData(page, testYear);
        await page.goto(`/Analysis/Annual?Year=${testYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        await expect(page.locator('[data-testid="annual-timeline-section"]')).toBeVisible();
        await expect(page.locator('[data-testid="annual-top-movements-section"]')).toBeVisible();
    });

    // ── T3 E2E Tests (Task 5.8) ──

    test('anomalies section renders after loading year with data', async ({ authenticatedPage: page }) => {
        const testYear = currentYear - 7;
        await seedAnnualData(page, testYear);
        await seedAnnualData(page, testYear);

        await page.goto(`/Analysis/Annual?Year=${testYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        await expect(page.locator('[data-testid="annual-anomalies-section"]')).toBeVisible();
    });

    test('discoveries and achievements sections render', async ({ authenticatedPage: page }) => {
        const testYear = currentYear - 7;
        await seedAnnualData(page, testYear);
        await seedAnnualData(page, testYear);

        await page.goto(`/Analysis/Annual?Year=${testYear}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        await expect(page.locator('[data-testid="annual-discoveries-section"]')).toBeVisible();
        await expect(page.locator('[data-testid="annual-achievements-section"]')).toBeVisible();
    });

    test('predictions section visible for multi-year data', async ({ authenticatedPage: page }) => {
        const year1 = currentYear - 9;
        const year2 = currentYear - 8;
        await seedAnnualData(page, year1);
        await seedAnnualData(page, year2);

        await page.goto(`/Analysis/Annual?Year=${year2}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        await expect(page.locator('[data-testid="annual-predictions-section"]')).toBeVisible();
    });

    test('historical comparison section visible', async ({ authenticatedPage: page }) => {
        const year1 = currentYear - 9;
        const year2 = currentYear - 8;
        await seedAnnualData(page, year1);
        await seedAnnualData(page, year2);

        await page.goto(`/Analysis/Annual?Year=${year2}`);
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        await expect(page.locator('[data-testid="annual-historical-comparison-section"]')).toBeVisible();
    });
});
