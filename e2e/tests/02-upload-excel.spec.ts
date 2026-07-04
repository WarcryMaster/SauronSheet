import { test, expect } from '@playwright/test';
import path from 'path';

/**
 * E2E Tests for Excel Upload page — ESP-4
 *
 * Auth is handled by Playwright's storageState (from auth.setup.ts).
 * The page is already authenticated — no login step needed.
 */

/** Real ING Excel fixture used across parser and integration tests (21 rows, 0 errors, 0 skipped). */
const EXCEL_FIXTURE_PATH = path.resolve(__dirname, '../../src/SauronSheet.Infrastructure/Excel/movements-non-2501.xls');

test.describe('Upload Excel Bank Statement — ESP-4', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/transactions/upload');
        // Wait for the page content to be visible. Using a server-rendered element
        // avoids depending on Alpine internals (which may or may not be initialized).
        await expect(page.getByTestId('excel-format-guide')).toBeVisible({ timeout: 10000 });
    });

    /**
     * ESP-4a: File input must have accept=".xls,.xlsx" — not ".pdf".
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
     */
    test('TC-U02: format guide is visible with Movimientos sheet and 7-column header', async ({ page }) => {
        const formatGuide = page.locator('[data-testid="excel-format-guide"]');
        await expect(formatGuide).toBeVisible();

        await expect(formatGuide).toContainText('Movimientos');

        await expect(formatGuide).toContainText('F. VALOR');
        await expect(formatGuide).toContainText('IMPORTE');
        await expect(formatGuide).toContainText('SALDO');
    });

    /**
     * ESP-4b (triangulation): Page title and heading must not reference PDF.
     */
    test('TC-U03: page title does not contain PDF wording', async ({ page }) => {
        const title = await page.title();
        expect(title).not.toMatch(/PDF/i);

        const heading = page.locator('h3');
        await expect(heading).not.toContainText('PDF', { ignoreCase: true });
    });

    /**
     * REQ-PROG-011: Real-time progress bar is visible during upload with ARIA attributes and counts.
     *
     * The upload is intercepted via page.route to serve a mocked progress bar.
     */
    test('TC-U04: shows progress bar during upload', async ({ page }) => {
        // Intercept the first progress poll to serve a mocked progress bar.
        let progressRequestCount = 0;
        await page.route(/\/[Tt]ransactions\/[Uu]pload\?handler=Progress(&|$)/, async route => {
            progressRequestCount++;
            if (progressRequestCount === 1) {
                await route.fulfill({
                    status: 200,
                    contentType: 'text/html',
                    body: `
                        <div id="progress-container" role="progressbar"
                             aria-valuenow="50" aria-valuemin="0" aria-valuemax="100"
                             aria-label="Import progress: 50%">
                            <p class="small text-muted mb-1">Processing file 1 of 1: statement.xls</p>
                            <div class="progress" style="height: 20px;">
                                <div class="progress-bar" style="width: 50%">50%</div>
                            </div>
                            <p class="small mt-1">5/10 rows | Imported: 3 | Skipped: 2</p>
                        </div>
                    `
                });
                return;
            }
            await route.continue();
        });

        const fileInput = page.locator('input[type="file"]');
        await fileInput.setInputFiles(EXCEL_FIXTURE_PATH);

        await expect(page.getByText('1 file(s) selected:')).toBeVisible();
        await expect(page.getByText('movements-non-2501.xls')).toBeVisible();
        await expect(page.getByTestId('upload-submit')).toBeEnabled();

        await page.getByTestId('upload-submit').click();

        const progressBar = page.locator('[role="progressbar"]');
        await expect(progressBar).toBeVisible({ timeout: 15000 });
        await expect(progressBar).toContainText('50%');
        await expect(progressBar).toContainText('Imported:');
        await expect(progressBar).toContainText('Skipped:');
    });

    /**
     * REQ-PROG-006: Upload completes and the final success result is displayed.
     *
     * Uses the same user-observable flow as a real user: select file, wait until
     * the file appears in the selected list, and submit.
     */
    test('TC-U05: completes upload and shows results', async ({ page }) => {
        const fileInput = page.locator('input[type="file"]');
        await fileInput.setInputFiles(EXCEL_FIXTURE_PATH);

        await expect(page.getByText('1 file(s) selected:')).toBeVisible();
        await expect(page.getByText('movements-non-2501.xls')).toBeVisible();
        await expect(page.getByTestId('upload-submit')).toBeEnabled();

        await page.getByTestId('upload-submit').click();

        // Two [role="status"] elements exist: upload spinner + result alert.
        // Target only the success result alert.
        const successAlert = page.locator('.alert-success[role="status"]');
        await expect(successAlert).toBeVisible({ timeout: 30000 });
        await expect(successAlert).toContainText('Import completed');
    });
});
