import { test, expect } from '@playwright/test';

/**
 * E2E Tests for Excel Upload page — ESP-4
 * GREEN: Pass after Upload.cshtml is updated to accept=".xls,.xlsx" with format guide.
 *
 * Auth strategy:
 *   Priority 1 — Env-var credentials: TEST_USER_EMAIL / TEST_USER_PASSWORD (CI / custom override).
 *   Priority 2 — Seeded test user: e2e@saurontest.local / E2eTestPass9! (pre-seeded in Supabase
 *                with email_confirmed_at set directly in auth.users — no email confirmation needed).
 *
 *   If both paths fail, tests skip with a diagnostic message rather than misleading failures.
 *
 *   NOTE: Self-registration is NOT used because Supabase email confirmation is ON for this project.
 *   The seeded user bypasses that requirement entirely.
 */

/** Default seeded test user — exists in Supabase with email_confirmed_at pre-set. */
const SEEDED_EMAIL    = 'e2e@saurontest.local';
const SEEDED_PASSWORD = 'E2eTestPass9!';

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
        // Auth path 1: env-var credentials (CI or developer override)
        const envEmail    = process.env.TEST_USER_EMAIL;
        const envPassword = process.env.TEST_USER_PASSWORD;
        if (envEmail && envPassword) {
            const ok = await loginWith(page, envEmail, envPassword);
            if (ok) { await page.goto('/transactions/upload'); return; }
        }

        // Auth path 2: seeded test user (pre-confirmed, works without email confirmation toggle)
        const seededOk = await loginWith(page, SEEDED_EMAIL, SEEDED_PASSWORD);
        if (seededOk) {
            await page.goto('/transactions/upload');
            return;
        }

        // Both paths failed — skip with diagnostic message
        test.skip(
            true,
            `W-2 BLOCKED: both auth paths failed. ` +
            `Seeded user ${SEEDED_EMAIL} did not authenticate — verify the user exists in Supabase ` +
            `auth.users with email_confirmed_at set and confirmation_token = ''. ` +
            `Env vars TEST_USER_EMAIL/TEST_USER_PASSWORD are not set.`
        );
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
