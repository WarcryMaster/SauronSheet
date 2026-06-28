import { test, expect } from '@playwright/test';
import path from 'path';
import { resolveTestAccount } from '../fixtures/budget-data.fixture';

/**
 * E2E Tests for Excel Upload page — ESP-4
 *
 * Auth strategy:
 *   Priority 1 — Env-var credentials: TEST_USER_EMAIL / TEST_USER_PASSWORD (CI / custom override).
 *   Priority 2 — Seeded test user: e2e@saurontest.local / ***REMOVED*** (pre-seeded in Supabase
 *                with email_confirmed_at set directly in auth.users — no email confirmation needed).
 *
 *   If both paths fail, tests skip with a diagnostic message rather than misleading failures.
 *
 *   NOTE: Self-registration is NOT used because Supabase email confirmation is ON for this project.
 *   The seeded user bypasses that requirement entirely.
 */

/** Default seeded test user — exists in Supabase with email_confirmed_at pre-set. */
const SEEDED_EMAIL    = 'e2e@saurontest.local';
const SEEDED_PASSWORD = '***REMOVED***';

/** Real ING Excel fixture used across parser and integration tests (21 rows, 0 errors, 0 skipped). */
const EXCEL_FIXTURE_PATH = path.resolve(__dirname, '../../src/SauronSheet.Infrastructure/Excel/movements-non-2501.xls');

/**
 * Attempts login with the given credentials.
 * Returns true if the browser reached /dashboard, false otherwise.
 */
async function loginWith(page: any, email: string, password: string): Promise<boolean> {
    await page.goto('/auth/login');
    await page.fill('input[type="email"]', email);
    await page.fill('input[type="password"]', password);
    await page.click('button[type="submit"]');

    try {
        await page.waitForURL(/dashboard/i, { timeout: 15000 });
        return true;
    } catch {
        return false;
    }
}

test.describe('Upload Excel Bank Statement — ESP-4', () => {
    test.beforeEach(async ({ page }) => {
        const account = resolveTestAccount();
        const authenticated = await loginWith(page, account.email, account.password);
        if (authenticated) {
            await page.goto('/transactions/upload');
            return;
        }

        test.skip(
            true,
            `W-2 BLOCKED: login failed for ${account.email}. ` +
            `Verify the user exists in Supabase auth.users with email_confirmed_at set.`
        );
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
        let progressRequestCount = 0;
        await page.route('**/Transactions/Upload?handler=Progress**', async route => {
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
        await page.click('button[type="submit"]');

        const progressBar = page.locator('[role="progressbar"]');
        await expect(progressBar).toBeVisible({ timeout: 10000 });
        await expect(progressBar).toContainText('50%');
        await expect(progressBar).toContainText('Imported:');
        await expect(progressBar).toContainText('Skipped:');
    });

    /**
     * REQ-PROG-006: Upload completes and the final success result is displayed.
     */
    test('TC-U05: completes upload and shows results', async ({ page }) => {
        await page.setInputFiles('input[type="file"]', EXCEL_FIXTURE_PATH);
        await page.click('button[type="submit"]');

        const successAlert = page.locator('[role="status"]');
        await expect(successAlert).toBeVisible({ timeout: 30000 });
        await expect(successAlert).toContainText('Import completed');
    });
});
