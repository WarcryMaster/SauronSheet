import { test, expect } from '@playwright/test';

/**
 * E2E Tests for Excel Upload page — ESP-4
 * RED: These tests fail against the unmodified Upload.cshtml (accept=".pdf", no format guide).
 * GREEN: Pass after Upload.cshtml is updated to accept=".xls,.xlsx" with format guide.
 */

test.describe('Upload Excel Bank Statement — ESP-4', () => {
    test.beforeEach(async ({ page }) => {
        // Upload page requires auth; skip if credentials are absent
        const testEmail = process.env.TEST_USER_EMAIL;
        const testPassword = process.env.TEST_USER_PASSWORD;
        test.skip(!testEmail || !testPassword, 'TEST_USER_EMAIL and TEST_USER_PASSWORD must be set');

        // Log in first
        await page.goto('/auth/login');
        await page.fill('input[type="email"]', testEmail!);
        await page.fill('input[type="password"]', testPassword!);
        await page.click('button[type="submit"]');
        await page.waitForURL(/dashboard/i, { timeout: 15000 });

        // Navigate to the upload page
        await page.goto('/transactions/upload');
    });

    /**
     * ESP-4a: File input must have accept=".xls,.xlsx" — not ".pdf".
     * RED: currently the input has accept=".pdf" → this test fails.
     */
    test('TC-U01: file input accepts only .xls and .xlsx', async ({ page }) => {
        const fileInput = page.locator('input[type="file"]');
        await expect(fileInput).toBeVisible();

        const acceptValue = await fileInput.getAttribute('accept');
        expect(acceptValue).toBe('.xls,.xlsx');
        expect(acceptValue).not.toContain('.pdf');
    });

    /**
     * ESP-4b: Format guide must be visible above the fold without additional scrolling.
     * The guide must mention: sheet "Movimientos", 7 columns, data starts at row 5.
     * RED: no format guide block exists in the current HTML → locator finds nothing.
     */
    test('TC-U02: format guide is visible with Movimientos sheet and 7-column header', async ({ page }) => {
        // The guide section should be visible without scrolling at 1280x720 viewport
        const formatGuide = page.locator('[data-testid="excel-format-guide"]');
        await expect(formatGuide).toBeVisible();

        // Must mention the required sheet name
        await expect(formatGuide).toContainText('Movimientos');

        // Must mention the exact 7-column header
        await expect(formatGuide).toContainText('F. VALOR');
        await expect(formatGuide).toContainText('IMPORTE');
        await expect(formatGuide).toContainText('SALDO');
    });

    /**
     * ESP-4b (triangulation): Page title and heading must say "Excel", not "PDF".
     * RED: current heading says "Upload Bank Statement" and title says "Upload PDF".
     */
    test('TC-U03: page title does not contain PDF wording', async ({ page }) => {
        const title = await page.title();
        expect(title).not.toMatch(/PDF/i);

        // The h1 heading should not mention PDF
        const heading = page.locator('h1');
        await expect(heading).not.toContainText('PDF', { ignoreCase: true });
    });
});
