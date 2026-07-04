import { test, expect, type Page } from '@playwright/test';
import { resolveTestAccount } from '../fixtures/budget-data.fixture';

test.use({ storageState: { cookies: [], origins: [] } });

async function loginForLocalizationSpec(page: Page): Promise<void> {
    const account = resolveTestAccount();

    await page.goto('/auth/login');
    await page.fill('[data-testid="login-email"]', account.email);
    await page.fill('[data-testid="login-password"]', account.password);
    await page.locator('[data-testid="login-submit"]').click();
    await page.waitForURL(/\/dashboard/i, { timeout: 15000 });
}

async function switchLanguage(page: Page, language: 'es' | 'en'): Promise<void> {
    await page.locator('#desktopNav [data-testid="lang-switcher"]').click();
    await page.locator(`#desktopNav [data-testid="lang-switcher-option-${language}"]`).waitFor({ state: 'visible' });
    await page.locator(`#desktopNav [data-testid="lang-switcher-option-${language}"]`).click();
    await expect(page.locator('html')).toHaveAttribute('lang', language);
}

test.describe('Import errors and system categories localization', () => {
    test.beforeEach(async ({ page }) => {
        await loginForLocalizationSpec(page);
    });

    test('TC-I18N-IMPORT-ES: parser/import error is localized in Spanish', async ({ page }) => {
        await switchLanguage(page, 'es');
        await page.goto('/transactions/upload');

        await page.setInputFiles('input[type="file"]', {
            name: 'invalid-format.xls',
            mimeType: 'application/vnd.ms-excel',
            buffer: Buffer.from('this is not a valid excel binary', 'utf-8')
        });

        await page.locator('[data-testid="upload-submit"]').click();

        const importFailure = page.locator('#upload-progress .alert-danger');
        await expect(importFailure).toBeVisible({ timeout: 30000 });
        await expect(importFailure).toContainText('El archivo no es un documento Excel válido. Sube un archivo .xls o .xlsx genuino.');
    });

    test('TC-I18N-IMPORT-EN: parser/import error is localized in English', async ({ page }) => {
        await switchLanguage(page, 'en');
        await page.goto('/transactions/upload');

        await page.setInputFiles('input[type="file"]', {
            name: 'invalid-format.xls',
            mimeType: 'application/vnd.ms-excel',
            buffer: Buffer.from('this is not a valid excel binary', 'utf-8')
        });

        await page.locator('[data-testid="upload-submit"]').click();

        const importFailure = page.locator('#upload-progress .alert-danger');
        await expect(importFailure).toBeVisible({ timeout: 30000 });
        await expect(importFailure).toContainText('The file is not a valid Excel document. Please upload a genuine .xls or .xlsx file.');
    });

    test('TC-I18N-CATEGORY-SELECTORS: category list remains locale-agnostic via data-testid', async ({ page }) => {
        await switchLanguage(page, 'es');
        await page.goto('/categories');

        const categoriesInSpanish = page.locator('[data-testid="system-category-name"], [data-testid="custom-category-name"]');
        await expect(categoriesInSpanish.first()).toBeVisible();

        await switchLanguage(page, 'en');
        await page.goto('/categories');

        const categoriesInEnglish = page.locator('[data-testid="system-category-name"], [data-testid="custom-category-name"]');
        await expect(categoriesInEnglish.first()).toBeVisible();

        const spanishCount = await categoriesInSpanish.count();
        const englishCount = await categoriesInEnglish.count();
        expect(englishCount).toBeGreaterThan(0);
        expect(spanishCount).toBe(englishCount);
    });

    // TC-I18N-CATEGORY-SYSTEM-ES-EN removed: system default categories were excluded from the
    // categories UI in Chunk 3 (see GetCategoriesQueryHandler — only user-scoped categories
    // are loaded via GetByUserIdAsync). The localization-by-slug logic (GetDisplayName in
    // Index.cshtml) is exercised indirectly by TC-I18N-CATEGORY-SELECTORS.
});
