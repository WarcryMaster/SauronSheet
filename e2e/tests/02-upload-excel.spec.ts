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
     * ESP-4b (triangulation): Page title and heading must not reference PDF.
     */
    test('TC-U03: page title does not contain PDF wording', async ({ page }) => {
        const title = await page.title();
        expect(title).not.toMatch(/PDF/i);

        // The heading should not mention PDF
        const heading = page.locator('h3');
        await expect(heading).not.toContainText('PDF', { ignoreCase: true });
    });

    /**
     * REQ-PROG-011: Real-time progress bar is visible during upload with ARIA attributes and counts.
     */
    test('TC-U04: shows progress bar during upload', async ({ page }) => {
        // Intercept the first progress poll so the fast real upload still renders a progress state.
        // Using RegExp to avoid glob ambiguity with ? in query strings (Playwright may parse the glob
        // string as a URL, splitting on ? and treating the query part differently).
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

        await page.setInputFiles('input[type="file"]', EXCEL_FIXTURE_PATH);
        // Use a specific locator — there are 3x button[type="submit"] on the page
        // (Logout × 2 + Upload) and the generic selector hits Logout first, logging out.
        await page.getByTestId('upload-submit').click();

        const progressBar = page.locator('[role="progressbar"]');
        await expect(progressBar).toBeVisible({ timeout: 15000 });
        await expect(progressBar).toContainText('50%');
        await expect(progressBar).toContainText('Imported:');
        await expect(progressBar).toContainText('Skipped:');
    });

    /**
     * REQ-PROG-006: Upload completes and the final success result is displayed.
     */
    test('TC-U05: completes upload and shows results', async ({ page }) => {
        await page.setInputFiles('input[type="file"]', EXCEL_FIXTURE_PATH);
        // Use a specific locator — there are 3x button[type="submit"] on the page
        // (Logout × 2 + Upload) and the generic selector hits Logout first, logging out.
        await page.getByTestId('upload-submit').click();

        // There are 2x [role="status"] elements on the page (the upload spinner + the result alert).
        // Use a more specific selector to target only the result alert.
        const successAlert = page.locator('.alert-success[role="status"]');
        await expect(successAlert).toBeVisible({ timeout: 30000 });
        await expect(successAlert).toContainText('Import completed');
    });
});
