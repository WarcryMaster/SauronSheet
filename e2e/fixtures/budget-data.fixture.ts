import { test as base, expect, Page } from '@playwright/test';

/**
 * Budget data fixture for SauronSheet E2E tests.
 *
 * Provisions deterministic test data before each test using the app's own API endpoints.
 * Idempotent: safe to run multiple times without creating duplicates.
 *
 * Provisioning steps (in order):
 *   1. Login via dual-path (env-var credentials, then seeded fallback)
 *   2. Ensure category "E2E-Budget-Cat-A" exists (Expense)
 *   3. Ensure category "E2E-Budget-Cat-B" exists (Expense)
 *   4. Ensure a -25€ expense transaction exists for "E2E-Budget-Cat-B" in the current month
 *   5. Yield the authenticated, provisioned page to the test
 *
 * Auth strategy (same dual-path as other E2E specs):
 *   Priority 1 — Env-var credentials: TEST_USER_EMAIL / TEST_USER_PASSWORD (CI / override)
 *   Priority 2 — Seeded test user: e2e@saurontest.local / ***REMOVED***
 *                (pre-confirmed in Supabase auth.users — no email confirmation needed)
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
 */
async function ensureFixtureTransactionExists(page: Page): Promise<void> {
    const now        = new Date();
    const year       = now.getFullYear();
    const month      = String(now.getMonth() + 1).padStart(2, '0');
    const firstDay   = `${year}-${month}-01`;
    const lastDayNum = new Date(year, now.getMonth() + 1, 0).getDate();
    const lastDay    = `${year}-${month}-${String(lastDayNum).padStart(2, '0')}`;

    await page.goto(`/transactions?StartDate=${firstDay}&EndDate=${lastDay}`);
    await page.waitForLoadState('domcontentloaded');

    const fixtureRow = page.locator('#transactionsTable tbody tr').filter({
        has: page.locator('td', { hasText: new RegExp(`^${FIXTURE_TX_DESCRIPTION}$`) }),
    });

    if ((await fixtureRow.count()) > 0) {
        return;
    }

    await page.goto('/transactions/add');
    await page.waitForLoadState('domcontentloaded');

    await page.fill('#Date',         firstDay);
    await page.fill('#Description',  FIXTURE_TX_DESCRIPTION);
    await page.fill('#Amount',       '-25');
    await page.selectOption('#Currency', 'EUR');
    await page.fill('#CategoryName', E2E_CAT_B);

    await page.getByRole('button', { name: 'Add Transaction' }).click();
    await page.waitForURL(/\/transactions/i, { timeout: 10000 });
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
                const input = row.querySelector('input[name="BudgetId"]') as HTMLInputElement | null;
                if (input?.value) ids.push(input.value);
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
                const deleteForm = item.querySelector('form[action*="Delete"]') as HTMLFormElement | null;
                const input = deleteForm?.querySelector('input[name="categoryId"]') as HTMLInputElement | null;
                if (input?.value) ids.push(input.value);
            }
        });
        return ids;
    });

    if (e2eCategoryIds.length === 0) return;

    for (const categoryId of e2eCategoryIds) {
        await page.evaluate(async (id: string) => {
            const tokenEl = document.querySelector('[name="__RequestVerificationToken"]') as HTMLInputElement | null;
            const token   = tokenEl?.value ?? '';

            const fd = new FormData();
            fd.append('categoryId', id);
            fd.append('__RequestVerificationToken', token);

            await fetch('/categories?handler=Delete', {
                method: 'POST',
                body: fd,
                credentials: 'same-origin',
            });
        }, categoryId);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Fixture export
// ─────────────────────────────────────────────────────────────────────────────

export const test = base.extend<BudgetFixtures>({
    budgetReadyPage: async ({ page }, use) => {
        const context = page.context();
        await context.clearCookies();

        const account = resolveTestAccount();
        const authenticated = await loginWith(page, account.email, account.password);

        if (!authenticated) {
            throw new Error(
                `FIXTURE BLOCKED: login failed for ${account.email}. ` +
                `Verify the user exists in Supabase auth.users with email_confirmed_at set.`,
            );
        }

        await ensureCategoryExists(page, E2E_CAT_A);
        await ensureCategoryExists(page, E2E_CAT_B);
        await ensureFixtureTransactionExists(page);

        await use(page);
    },
});

export { expect } from '@playwright/test';
