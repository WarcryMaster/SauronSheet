/**
 * E2E Tests for Annual Analysis (annual-analysis PR 3)
 *
 * Covers tasks 4.4-4.5:
 *   - Table renders with data and summary block is visible
 *   - Empty state appears for a year with no data
 *   - Changing the year selector reloads the page with the new year
 *
 * E2E Testing Policy (from AGENTS.md):
 *   - MUST act as real user: page.click(), page.fill(), page.selectOption()
 *   - Wait for elements to be visible before interacting
 *   - DO NOT use page.evaluate() to execute direct JavaScript (Flatpickr date input is the documented exception)
 *   - DO NOT use fetch() inside page.evaluate()
 */

import { test, expect } from '../fixtures/auth.fixture';

const currentYear = new Date().getFullYear();
const emptyYear = 1999;
const alternateYear = 2024;

test.describe('Annual Analysis', () => {
    test('renders table with data and summary block', async ({ authenticatedPage: page }) => {
        // ── Add a deterministic transaction for the current year via the UI ──
        await page.goto('/transactions/add');
        await expect(page).toHaveURL(/\/transactions\/add/i);

        // Flatpickr date input — use Flatpickr API as required by project conventions
        const firstDayOfYear = `${currentYear}-01-15`;
        await page.evaluate((dateStr) => {
            const el = document.getElementById('Date') as HTMLInputElement;
            const fp = (el as any)._flatpickr;
            fp.setDate(dateStr, true);
        }, firstDayOfYear);

        await page.fill('#Description', 'E2E Annual Analysis fixture');
        await page.fill('#Amount', '-50');
        await page.selectOption('#Currency', 'EUR');
        await page.fill('#CategoryName', 'E2E-Annual-Category');

        await page.getByRole('button', { name: 'Add Transaction' }).click();
        await page.waitForURL(/\/transactions/i, { timeout: 10000 });

        // ── Navigate to Annual Analysis and verify data state ──
        await page.goto('/Analysis/Annual');
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const table = page.locator('table');
        await expect(table).toBeVisible();
        await expect(table).toContainText('Sin clasificar');
        await expect(table).toContainText('Gasto Variable');

        const summaryBlock = page.locator('[data-testid="annual-summary"]');
        await expect(summaryBlock).toBeVisible();
        await expect(summaryBlock).toContainText('Neto');
    });

    test('shows empty state for year with no data', async ({ authenticatedPage: page }) => {
        await page.goto(`/Analysis/Annual?year=${emptyYear}`);
        await expect(page).toHaveURL(new RegExp(`\\/Analysis\\/Annual\\?year=${emptyYear}`, 'i'));

        const emptyState = page.locator('[data-testid="annual-empty-state"]');
        await expect(emptyState).toBeVisible();
        await expect(emptyState).toContainText('Sin datos para este año');
        await expect(page.locator('table')).toHaveCount(0);
    });

    test('changing year selector reloads page with new year', async ({ authenticatedPage: page }) => {
        await page.goto('/Analysis/Annual');
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        const yearSelect = page.locator('#Year');
        await expect(yearSelect).toBeVisible();

        await yearSelect.selectOption(String(alternateYear));
        await expect(page).toHaveURL(new RegExp(`\\/Analysis\\/Annual\\?year=${alternateYear}`, 'i'));

        // The selector preserves the selected value after reload
        await expect(yearSelect).toHaveValue(String(alternateYear));
    });
});
