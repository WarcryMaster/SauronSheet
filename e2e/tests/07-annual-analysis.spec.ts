/**
 * E2E Tests for Annual Analysis (annual-analysis PR 3)
 *
 * Covers the new UI layout:
 *   - Income section (bg-success-subtle) with 3 summary cards + filtered table
 *   - Expense section (bg-danger-subtle) with 3 summary cards + filtered table
 *   - Neto card centered between sections
 *   - MonthsWithData card showing "X / 12 meses con datos" or "Año completo"
 *   - YoY variation badges hidden when no previous year data
 *   - Empty state for year with no data
 *   - Year selector reloads page with new year
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
    test('renders the new layout with income/expense sections and all cards', async ({ authenticatedPage: page }) => {
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

        await page.fill('#Description', 'E2E Annual Analysis layout');
        await page.fill('#Amount', '-50');
        await page.selectOption('#Currency', 'EUR');
        await page.fill('#CategoryName', 'E2E-Annual-Category');

        await page.getByRole('button', { name: 'Add Transaction' }).click();
        await page.waitForURL(/\/transactions/i, { timeout: 10000 });

        // ── Navigate to Annual Analysis ──
        await page.goto('/Analysis/Annual');
        await expect(page).toHaveURL(/\/Analysis\/Annual/i);

        // ── Income section (bg-success-subtle) ──
        const incomeSection = page.locator('[data-testid="annual-income-section"]');
        await expect(incomeSection).toBeVisible();
        await expect(incomeSection).toContainText('Ingresos');

        // Income summary cards: Fixed, Variable, Total
        const incomeFixedCard = incomeSection.locator('[data-testid="income-fixed-card"]');
        await expect(incomeFixedCard).toBeVisible();
        await expect(incomeFixedCard).toContainText('Fijo');

        const incomeVariableCard = incomeSection.locator('[data-testid="income-variable-card"]');
        await expect(incomeVariableCard).toBeVisible();
        await expect(incomeVariableCard).toContainText('Variable');

        const incomeTotalCard = incomeSection.locator('[data-testid="income-total-card"]');
        await expect(incomeTotalCard).toBeVisible();
        await expect(incomeTotalCard).toContainText('Total');

        // Income table — only income rows
        const incomeTable = page.locator('[data-testid="annual-income-table"]');
        await expect(incomeTable).toBeVisible();
        // Our -50 transaction should be here (it's an expense, so it shouldn't be in income table)
        // The income table should have the "Sin clasificar" row if any income exists

        // ── Expense section (bg-danger-subtle) ──
        const expenseSection = page.locator('[data-testid="annual-expense-section"]');
        await expect(expenseSection).toBeVisible();
        await expect(expenseSection).toContainText('Gastos');

        // Expense summary cards: Fixed, Variable, Total
        const expenseFixedCard = expenseSection.locator('[data-testid="expense-fixed-card"]');
        await expect(expenseFixedCard).toBeVisible();
        await expect(expenseFixedCard).toContainText('Fijo');

        const expenseVariableCard = expenseSection.locator('[data-testid="expense-variable-card"]');
        await expect(expenseVariableCard).toBeVisible();
        await expect(expenseVariableCard).toContainText('Variable');

        const expenseTotalCard = expenseSection.locator('[data-testid="expense-total-card"]');
        await expect(expenseTotalCard).toBeVisible();
        await expect(expenseTotalCard).toContainText('Total');

        // Expense table — should contain our -50 transaction (Gasto Variable)
        const expenseTable = page.locator('[data-testid="annual-expense-table"]');
        await expect(expenseTable).toBeVisible();
        await expect(expenseTable).toContainText('Sin clasificar');
        await expect(expenseTable).toContainText('Gasto Variable');

        // ── Neto card between sections ──
        const netoCard = page.locator('[data-testid="annual-neto-card"]');
        await expect(netoCard).toBeVisible();
        await expect(netoCard).toContainText('Neto');

        // ── MonthsWithData card ──
        const monthsCard = page.locator('[data-testid="annual-months-card"]');
        await expect(monthsCard).toBeVisible();
        await expect(monthsCard).toContainText(/meses con datos|Año completo/i);

        // ── YoY badges should be hidden (no previous year data) ──
        const yoyBadges = page.locator('[data-testid^="yoy-badge-"]');
        await expect(yoyBadges).toHaveCount(0);
    });

    test('shows empty state for year with no data', async ({ authenticatedPage: page }) => {
        await page.goto(`/Analysis/Annual?year=${emptyYear}`);
        await expect(page).toHaveURL(new RegExp(`\\/Analysis\\/Annual\\?year=${emptyYear}`, 'i'));

        const emptyState = page.locator('[data-testid="annual-empty-state"]');
        await expect(emptyState).toBeVisible();
        await expect(emptyState).toContainText('Sin datos para este año');
        await expect(page.locator('[data-testid="annual-income-table"]')).toHaveCount(0);
        await expect(page.locator('[data-testid="annual-expense-table"]')).toHaveCount(0);
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
