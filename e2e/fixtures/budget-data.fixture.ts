import { test as base, expect, Page } from '@playwright/test';
import { setFlatpickrDate } from '../helpers';

/**
 * Budget data fixture for SauronSheet E2E tests.
 *
 * Provisions deterministic test data before each test using the app's own UI.
 * Idempotent: safe to run multiple times without creating duplicates.
 *
 * Provisioning steps (in order):
 *   1. Ensure category "E2E-Budget-Cat-A" exists (Expense)
 *   2. Ensure category "E2E-Budget-Cat-B" exists (Expense)
 *   3. Ensure a -25€ expense transaction exists for "E2E-Budget-Cat-B" in the current month
 *   4. Yield the authenticated, provisioned page to the test
 *
 * Auth is handled by Playwright's storageState (from auth.setup.ts).
 * The page is already authenticated — no login step needed in the fixture.
 *
 * Implementation note — Categories API:
 *   The /categories page has two [BindProperty] models (CreateForm, EditForm). ASP.NET
 *   Core validates ALL bound properties on POST, so submitting only CreateForm.* fields
 *   causes ModelState.IsValid=false because EditForm.Name fails [Required].
 *   The fixture calls the endpoint via page.evaluate() and includes placeholder EditForm.*
 *   values to satisfy ModelState. This is a known production-code issue (tracked separately).
 */

/** Default seeded test user — exists in Supabase with email_confirmed_at pre-set. */
const SEEDED_EMAIL    = 'e2e@saurontest.local';
const SEEDED_PASSWORD = '***REMOVED***';

/** Deterministic category names shared with the budget test suite. */
export const E2E_CAT_A = 'E2E-Budget-Cat-A';
export const E2E_CAT_B = 'E2E-Budget-Cat-B';

/** Deterministic description used to identify the fixture transaction for idempotency. */
const FIXTURE_TX_DESCRIPTION = 'E2E-Budget-Cat-B fixture';

interface BudgetFixtures {
    budgetReadyPage: Page;
}

/**
 * Attempts login with the given credentials.
 * MUST be called on a clean context (no prior session cookies).
 * Returns true if the browser reached /dashboard, false otherwise.
 */
async function loginWith(page: Page, email: string, password: string): Promise<boolean> {
    await page.goto('/auth/login');
    await page.fill('[data-testid="login-email"]', email);
    await page.fill('[data-testid="login-password"]', password);
    await page.locator('[data-testid="login-submit"]').click();
    try {
        await page.waitForURL(/dashboard/i, { timeout: 15000 });
        return true;
    } catch {
        return false;
    }
}

/**
 * Resolves the test account credentials.
 *
 * Safety rule: env-var credentials are ONLY honoured when CI=true.
 * Local runs MUST always use the seeded test user to prevent
 * accidental pollution of real user accounts with test data.
 */
export function resolveTestAccount(): { email: string; password: string } {
    const isCI = process.env.CI === 'true' || process.env.CI === '1';

    if (isCI) {
        const envEmail = process.env.TEST_USER_EMAIL;
        const envPassword = process.env.TEST_USER_PASSWORD;
        if (envEmail && envPassword) {
            return { email: envEmail, password: envPassword };
        }
    }

    return { email: SEEDED_EMAIL, password: SEEDED_PASSWORD };
}

/**
 * Logs in using the resolved test account for the current environment.
 */
export async function loginAsTestAccount(page: Page): Promise<void> {
    const account = resolveTestAccount();
    const authenticated = await loginWith(page, account.email, account.password);

    if (!authenticated) {
        throw new Error(
            `FIXTURE BLOCKED: login failed for ${account.email}. ` +
            `Verify the user exists in Supabase auth.users with email_confirmed_at set.`,
        );
    }
}

/**
 * Ensures an Expense category with the given name exists for the authenticated user.
 *
 * Idempotency: navigates to /categories and performs a DOM check. If the category is
 * already listed, returns immediately without creating it.
 *
 * Creation: uses the UI modal form to create the category (real user interaction).
 */
async function ensureCategoryExists(page: Page, name: string): Promise<void> {
    await page.goto('/categories');
    await page.waitForLoadState('domcontentloaded');

    // DOM idempotency check: exact name match against category list items.
    const existing = page.locator('.list-group-item').filter({
        has: page.locator('.fw-bold', { hasText: new RegExp(`^${name}$`) }),
    });

    if ((await existing.count()) > 0) {
        return; // Already exists — nothing to do
    }

    // Create category via UI modal form (real user interaction)
    await page.getByRole('button', { name: 'Add New Category' }).click();
    
    const createModal = page.locator('#createCategoryModal');
    await expect(createModal).toBeVisible();

    await page.fill('#createName', name);
    await page.selectOption('#createType', '1');
    await page.selectOption('#createIcon', 'tag');
    await page.fill('#createColor', '#3498DB');

    const submitBtn = page.locator('#createSubmitBtn');
    await expect(submitBtn).toBeEnabled({ timeout: 5000 });

    await submitBtn.click();

    await expect(createModal).toBeHidden({ timeout: 10000 });
    await page.waitForLoadState('domcontentloaded');
    
    await page.locator('.list-group-item').filter({
        has: page.locator('.fw-bold', { hasText: new RegExp(`^${name}$`) }),
    }).waitFor({ state: 'visible', timeout: 5000 }).catch(async () => {
        const errorAlert = page.locator('#createError:not(.d-none)');
        if (await errorAlert.isVisible()) {
            const errorMsg = await errorAlert.textContent();
            throw new Error(`Category creation failed: ${errorMsg}`);
        }
        throw new Error(`Category "${name}" did not appear after creation`);
    });
}

/**
 * Ensures the deterministic fixture transaction exists for E2E-Budget-Cat-B.
 *
 * NOTE: kept because the shared budget fixture still relies on a current-month
 * expense transaction for downstream dashboard/budget scenarios.
 */
async function ensureFixtureTransactionExists(page: Page): Promise<void> {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const firstDay = `${year}-${month}-01`;
    const safeDayNumber = Math.min(now.getDate(), 15);
    const safeDay = `${year}-${month}-${String(safeDayNumber).padStart(2, '0')}`;
    const lastDayNum = new Date(year, now.getMonth() + 1, 0).getDate();
    const lastDay = `${year}-${month}-${String(lastDayNum).padStart(2, '0')}`;
    const filteredTransactionsUrl = `/transactions?StartDate=${firstDay}&EndDate=${lastDay}`;

    await page.goto(filteredTransactionsUrl);
    await page.waitForLoadState('domcontentloaded');

    const fixtureRow = page.locator('#transactionsTable tbody tr').filter({
        has: page.locator('td', { hasText: FIXTURE_TX_DESCRIPTION }),
    });

    if ((await fixtureRow.count()) > 0) {
        return;
    }

    await page.goto('/transactions/add');
    await page.waitForLoadState('domcontentloaded');

    await setFlatpickrDate(page, 'Date', safeDay);
    await page.fill('#Description', FIXTURE_TX_DESCRIPTION);
    await page.fill('#Amount', '-25');
    await page.selectOption('#Currency', 'EUR');
    await page.fill('#CategoryName', E2E_CAT_B);

    await page.getByRole('button', { name: 'Add Transaction' }).click();

    const redirectResult = await Promise.race([
        page.waitForURL(/\/transactions(?:\?|$)/i, { timeout: 10000 })
            .then(() => ({ success: true as const })),
        page.locator('.alert-danger, .validation-summary-errors, .field-validation-error').first()
            .waitFor({ state: 'visible', timeout: 10000 })
            .then(() => ({ success: false as const, type: 'error' as const })),
        new Promise<{ success: false; type: 'timeout' }>((resolve) =>
            setTimeout(() => resolve({ success: false, type: 'timeout' }), 10000)),
    ]);

    if (!redirectResult.success) {
        if (redirectResult.type === 'error') {
            const errorText = await page.locator('.alert-danger, .validation-summary-errors, .field-validation-error')
                .first()
                .textContent()
                .catch(() => null);
            throw new Error(`Fixture transaction creation failed: ${errorText?.trim() || 'validation error on form'}`);
        }

        throw new Error(`Fixture transaction creation failed: did not redirect to /transactions (current URL: ${page.url()})`);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Cleanup helpers — call from test.afterAll to remove E2E artifacts
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Deletes all budgets whose category name matches the E2E pattern.
 * Navigates to /budgets, collects IDs from the DOM, then POSTs delete for each.
 * Tolerant: missing CSRF token or empty list → silent no-op.
 */
export async function cleanupE2EBudgets(page: Page): Promise<void> {
    await page.goto('/budgets');
    await page.waitForLoadState('domcontentloaded');

    const e2eBudgetIds = await page.evaluate(() => {
        const rows = document.querySelectorAll('table tbody tr');
        const ids: string[] = [];
        rows.forEach(row => {
            const categoryCell = row.querySelector('td');
            if (categoryCell?.textContent?.includes('E2E-')) {
                const editLink = row.querySelector('a[href^="/budgets/edit/"]') as HTMLAnchorElement | null;
                const href = editLink?.getAttribute('href') ?? '';
                const match = href.match(/\/budgets\/edit\/([0-9a-fA-F-]{36})$/);
                if (match?.[1]) {
                    ids.push(match[1]);
                }
            }
        });
        return ids;
    });

    if (e2eBudgetIds.length === 0) return;

    // Delete each budget permanently via the Razor Pages POST handler
    for (const budgetId of e2eBudgetIds) {
        await page.evaluate(async (id: string) => {
            const tokenEl = document.querySelector('[name="__RequestVerificationToken"]') as HTMLInputElement | null;
            const token   = tokenEl?.value ?? '';

            const fd = new FormData();
            fd.append('BudgetId', id);
            fd.append('__RequestVerificationToken', token);

            await fetch(`/budgets/edit/${id}?handler=Delete`, {
                method: 'POST',
                body: fd,
                credentials: 'same-origin',
            });
        }, budgetId);
    }
}

/**
 * Deletes all deterministic E2E fixture transactions for the current month.
 * Repeats until the first page no longer contains matches so paginated residue
 * from previous runs is also removed.
 */
export async function cleanupE2ETransactions(page: Page): Promise<void> {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const firstDay = `${year}-${month}-01`;
    const lastDayNum = new Date(year, now.getMonth() + 1, 0).getDate();
    const lastDay = `${year}-${month}-${String(lastDayNum).padStart(2, '0')}`;
    const filteredTransactionsUrl = `/transactions?StartDate=${firstDay}&EndDate=${lastDay}`;

    for (let attempt = 0; attempt < 20; attempt++) {
        await page.goto(filteredTransactionsUrl);
        await page.waitForLoadState('domcontentloaded');

        const matchingRows = page.locator('#transactionsTable tbody tr').filter({
            has: page.locator('td', { hasText: FIXTURE_TX_DESCRIPTION }),
        });

        if ((await matchingRows.count()) === 0) {
            return;
        }

        const deleteButton = matchingRows.first().getByRole('button', { name: 'Delete' });
        await expect(deleteButton).toBeVisible();

        page.once('dialog', async dialog => {
            await dialog.accept();
        });

        await Promise.all([
            page.waitForURL(/\/transactions(?:\?|$)/i, { timeout: 10000 }),
            deleteButton.click(),
        ]);

        await page.waitForLoadState('domcontentloaded');
    }

    throw new Error('E2E cleanup failed: fixture transactions still remained after repeated delete attempts.');
}

/**
 * Deletes all categories whose name matches the E2E pattern.
 * Navigates to /categories, collects IDs from the DOM, then POSTs delete for each.
 * Tolerant: missing category or CSRF → silent no-op.
 */
export async function cleanupE2ECategories(page: Page): Promise<void> {
    await page.goto('/categories');
    await page.waitForLoadState('domcontentloaded');

    const e2eCategoryIds = await page.evaluate(() => {
        const items = document.querySelectorAll('.list-group-item');
        const ids: string[] = [];
        items.forEach(item => {
            const nameEl = item.querySelector('.fw-bold');
            if (nameEl?.textContent?.includes('E2E-')) {
                const deleteButton = item.querySelector('button[onclick*="showDeleteConfirm("]') as HTMLButtonElement | null;
                const onclickValue = deleteButton?.getAttribute('onclick') ?? '';
                const match = onclickValue.match(/showDeleteConfirm\('([0-9a-fA-F-]{36})',/);
                if (match?.[1]) ids.push(match[1]);
            }
        });
        return ids;
    });

    if (e2eCategoryIds.length === 0) return;

    for (const categoryId of e2eCategoryIds) {
        await page.evaluate(async (id: string) => {
            const tokenEl = document.querySelector('[name="__RequestVerificationToken"]') as HTMLInputElement | null;
            const token   = tokenEl?.value ?? '';

            await fetch(`/categories?handler=Delete&categoryId=${encodeURIComponent(id)}`, {
                method: 'POST',
                headers: {
                    RequestVerificationToken: token,
                },
                credentials: 'same-origin',
            });
        }, categoryId);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Fixture export
// ─────────────────────────────────────────────────────────────────────────────

/** Shared storage state file path for auth setup. */
export const AUTH_FILE = '.auth/user.json';

export const test = base.extend<BudgetFixtures>({
    budgetReadyPage: async ({ page }, use) => {
        // Page is already authenticated via storageState (from auth.setup.ts).
        // Only provision the deterministic test data.

        await ensureCategoryExists(page, E2E_CAT_A);
        await ensureCategoryExists(page, E2E_CAT_B);
        await ensureFixtureTransactionExists(page);

        await use(page);
    },
});

export { expect } from '@playwright/test';
