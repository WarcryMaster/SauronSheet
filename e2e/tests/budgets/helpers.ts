import { expect, Locator, Page } from '@playwright/test';
import { setFlatpickrDate } from '../../helpers';

export function getCurrentBudgetMonth(): { year: number; monthNumber: number; month: string; firstDay: string } {
    const now = new Date();
    const year = now.getFullYear();
    const monthNumber = now.getMonth() + 1;
    const month = String(monthNumber).padStart(2, '0');

    return {
        year,
        monthNumber,
        month,
        firstDay: `${year}-${month}-01`,
    };
}

export function budgetRow(page: Page, categoryName: string): Locator {
    return page.locator('table tbody tr').filter({
        has: page.locator('td', { hasText: categoryName }),
    });
}

async function openBudgetEditPage(page: Page, categoryName: string): Promise<void> {
    await page.goto('/budgets');
    await page.waitForLoadState('domcontentloaded');

    const row = budgetRow(page, categoryName);
    await expect(row).toHaveCount(1);

    const editLink = row.first().getByRole('link', { name: 'Edit' });
    await expect(editLink).toBeVisible();

    const editHref = await editLink.getAttribute('href');
    await page.goto(editHref!);
    await expect(page).toHaveURL(/\/budgets\/edit\//i);
}

export async function ensureBudgetExists(page: Page, categoryName: string, limitAmount: string): Promise<void> {
    await page.goto('/budgets');
    await page.waitForLoadState('domcontentloaded');

    if ((await budgetRow(page, categoryName).count()) > 0) {
        return;
    }

    const currentMonth = getCurrentBudgetMonth();

    await page.goto('/budgets/create');
    await expect(page).toHaveURL(/\/budgets\/create/i);

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

    await categorySelect.selectOption({ label: categoryName });

    await page.fill('#LimitAmount', limitAmount);

    // EffectiveFrom is a Flatpickr input — use Flatpickr API
    await setFlatpickrDate(page, 'EffectiveFrom', currentMonth.firstDay);

    await page.selectOption('#PeriodGranularity', 'Monthly');

    await page.getByRole('button', { name: 'Create Budget' }).click();

    await Promise.race([
        page.waitForURL((url: URL) => url.pathname === '/budgets', { timeout: 30000 }),
        page.locator('.alert-danger').waitFor({ state: 'visible', timeout: 30000 }),
    ]).catch(() => {});

    if (new URL(page.url()).pathname !== '/budgets') {
        await page.goto('/budgets');
    }

    await expect(budgetRow(page, categoryName)).toHaveCount(1);
}

export async function ensureBudgetDeleted(page: Page, categoryName: string): Promise<void> {
    await page.goto('/budgets');
    await page.waitForLoadState('domcontentloaded');

    if ((await budgetRow(page, categoryName).count()) === 0) {
        return;
    }

    await openBudgetEditPage(page, categoryName);

    await page.getByRole('button', { name: 'Delete Permanently' }).click();

    const deleteModal = page.locator('#budgetDeleteConfirmModal');
    await expect(deleteModal).toBeVisible({ timeout: 5000 });
    await deleteModal.getByRole('button', { name: /Delete/i }).click();

    await page.waitForURL(/\/budgets(?!\/edit\/)/i, { timeout: 10000 });
    await page.waitForLoadState('domcontentloaded');

    await expect(budgetRow(page, categoryName)).toHaveCount(0);
}

export async function ensureBudgetStatus(page: Page, categoryName: string, desiredStatus: 'Active' | 'Inactive'): Promise<void> {
    await ensureBudgetExists(page, categoryName, '100.00');
    await page.goto('/budgets');
    await page.waitForLoadState('domcontentloaded');

    const row = budgetRow(page, categoryName);
    await expect(row).toHaveCount(1);

    const statusBadge = row.first().locator('.badge');
    const currentStatus = (await statusBadge.textContent())?.trim();
    if (currentStatus === desiredStatus) {
        return;
    }

    await openBudgetEditPage(page, categoryName);

    const actionButtonName = desiredStatus === 'Inactive' ? 'Deactivate Budget' : 'Activate Budget';
    const confirmButtonName = desiredStatus === 'Inactive' ? 'Deactivate' : 'Activate';

    const actionButton = page.getByRole('button', { name: actionButtonName });
    await expect(actionButton).toBeVisible();
    await actionButton.click();

    const statusModal = page.locator('#budgetStatusModal');
    await expect(statusModal).toBeVisible();
    await statusModal.getByRole('button', { name: confirmButtonName }).click();

    await page.waitForURL(/\/budgets(?!\/edit\/)/i, { timeout: 10000 });
    await page.waitForLoadState('domcontentloaded');

    await expect(budgetRow(page, categoryName).locator('.badge')).toContainText(desiredStatus);
}
