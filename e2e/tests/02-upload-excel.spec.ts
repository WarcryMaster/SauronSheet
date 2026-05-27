import { test, expect } from '@playwright/test';

/**
 * E2E Tests for Excel Upload page — ESP-4
 * GREEN: Pass after Upload.cshtml is updated to accept=".xls,.xlsx" with format guide.
 *
 * Auth strategy (W-2 remediation attempt):
 *   Priority 1 — Self-registration: registers a unique test user via /auth/register,
 *                which auto-logs in on success (email confirmation must be OFF).
 *   Priority 2 — Env-var credentials: fallback to TEST_USER_EMAIL / TEST_USER_PASSWORD
 *                if self-registration is unavailable (e.g. email confirmation is ON).
 *
 *   If BOTH paths fail, tests skip with a clear diagnostic message rather than
 *   producing misleading failures.
 */

/** Generate a unique e-mail address safe for re-use across runs. */
function uniqueTestEmail(): string {
    return `e2e.sauron.${Date.now()}@gmail.com`;
}

/**
 * Attempts to authenticate via self-registration (/auth/register).
 * Returns true if the browser reached /dashboard after registration.
 * Returns false if the page shows an error (email confirmation ON, rate limit, etc.).
 */
async function tryRegisterAndLogin(page: any): Promise<boolean> {
    const email = uniqueTestEmail();
    const password = 'E2eTestPass9!';

    await page.goto('/auth/register');
    await page.fill('#email', email);
    await page.fill('#password', password);
    await page.fill('#confirmPassword', password);
    await page.click('button[type="submit"]');

    try {
        // If auto-login after registration is active, we land on /dashboard
        await page.waitForURL(/dashboard/i, { timeout: 12000 });
        return true;
    } catch {
        // Registration failed (error shown on page) or redirect never happened
        return false;
    }
}

/**
 * Attempts login with existing credentials from environment variables.
 * Returns true on success, false otherwise.
 */
async function tryEnvLogin(page: any): Promise<boolean> {
    const testEmail = process.env.TEST_USER_EMAIL;
    const testPassword = process.env.TEST_USER_PASSWORD;
    if (!testEmail || !testPassword) return false;

    await page.goto('/auth/login');
    await page.fill('input[type="email"]', testEmail);
    await page.fill('input[type="password"]', testPassword);
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
        // Auth path 1: self-register a fresh unique user (works when email confirmation is OFF)
        const registeredOk = await tryRegisterAndLogin(page);
        if (registeredOk) {
            await page.goto('/transactions/upload');
            return;
        }

        // Auth path 2: use pre-seeded env-var credentials
        const loggedInOk = await tryEnvLogin(page);
        if (loggedInOk) {
            await page.goto('/transactions/upload');
            return;
        }

        // Both paths failed — skip with diagnostic message
        const registerError = await page.locator('.alert-danger').textContent().catch(() => '(not found)');
        test.skip(
            true,
            `W-2 BLOCKED: both auth paths failed. ` +
            `Self-register error on page: "${registerError}". ` +
            `Env vars TEST_USER_EMAIL/TEST_USER_PASSWORD not set. ` +
            `Root cause: Supabase email confirmation is ON — self-registration does not create a ` +
            `session until the confirmation link is clicked. ` +
            `Resolution: disable email confirmation in Supabase Auth settings, ` +
            `or provide TEST_USER_EMAIL / TEST_USER_PASSWORD for a pre-verified account.`
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
