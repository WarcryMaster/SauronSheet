import { test as base, Page } from '@playwright/test';

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
 * Ensures an Expense category with the given name exists for the authenticated user.
 *
 * Idempotency: navigates to /categories and performs a DOM check. If the category is
 * already listed, returns immediately without creating it.
 *
 * Creation: calls /categories?handler=Create via page.evaluate() (shares the browser's
 * auth session and anti-forgery cookie). Includes required EditForm.* placeholder values
 * because the page has two [BindProperty] models and ModelState validates both.
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

    // Call the app's category create endpoint from within the browser context.
    // The browser context shares the same auth cookies and anti-forgery state as the page.
    // EditForm.* placeholder values are required because the page model validates both
    // CreateForm and EditForm on every POST (see implementation note in file header).
    const result = await page.evaluate(async (catName: string) => {
        const tokenEl = document.querySelector('[name="__RequestVerificationToken"]') as HTMLInputElement | null;
        const token   = tokenEl?.value ?? '';

        const fd = new FormData();
        fd.append('CreateForm.Name',    catName);
        fd.append('CreateForm.Type',    '1');        // 1 = Expense
        fd.append('CreateForm.Color',   '#3498DB');
        fd.append('CreateForm.IconName', 'tag');
        // Placeholder values for EditForm — required by ModelState due to [BindProperty]
        fd.append('EditForm.Name',    '_');          // 1 char min per [StringLength(50, MinimumLength = 1)]
        fd.append('EditForm.Color',   '#3498DB');
        fd.append('EditForm.IconName', 'tag');
        fd.append('__RequestVerificationToken', token);

        const resp = await fetch('/categories?handler=Create', {
            method: 'POST',
            body:   fd,
        });

        return resp.json() as Promise<{ success: boolean; error?: string; categoryId?: string }>;
    }, name);

    if (!result.success) {
        // Tolerate "already exists" domain errors (race condition between DOM check and creation)
        const isAlreadyExists = result.error
            ? /already|duplicate|exist/i.test(result.error)
            : false;

        if (!isAlreadyExists) {
            throw new Error(`Category creation failed for "${name}": ${result.error}`);
        }
    }

    // Reload to reflect the new or existing category in the DOM
    await page.reload({ waitUntil: 'domcontentloaded' });

    // Verify the category appears in the list after reload
    await page.locator('.list-group-item').filter({
        has: page.locator('.fw-bold', { hasText: new RegExp(`^${name}$`) }),
    }).waitFor({ state: 'visible', timeout: 5000 });
}

/**
 * Ensures the deterministic fixture transaction exists for E2E-Budget-Cat-B.
 *
 * Transaction properties:
 *   - Amount      : -25 EUR (expense)
 *   - Date        : first day of the current calendar month
 *   - Category    : E2E-Budget-Cat-B
 *   - Description : "E2E-Budget-Cat-B fixture" (idempotency key)
 *
 * Idempotency: navigates to /transactions filtered to the current month and checks
 * the transactions table for a row with the fixture description.
 *
 * Creation: standard Razor Pages form POST (single [BindProperty] Input model — no
 * cross-form ModelState issue). Redirects to /transactions on success.
 */
async function ensureFixtureTransactionExists(page: Page): Promise<void> {
    const now        = new Date();
    const year       = now.getFullYear();
    const month      = String(now.getMonth() + 1).padStart(2, '0');
    const firstDay   = `${year}-${month}-01`;
    const lastDayNum = new Date(year, now.getMonth() + 1, 0).getDate();
    const lastDay    = `${year}-${month}-${String(lastDayNum).padStart(2, '0')}`;

    // DOM idempotency check: look for the fixture row in the current-month transaction list.
    // If the table is absent (no transactions yet) count() returns 0 — proceed to creation.
    await page.goto(`/transactions?StartDate=${firstDay}&EndDate=${lastDay}`);
    await page.waitForLoadState('domcontentloaded');

    const fixtureRow = page.locator('#transactionsTable tbody tr').filter({
        has: page.locator('td', { hasText: new RegExp(`^${FIXTURE_TX_DESCRIPTION}$`) }),
    });

    if ((await fixtureRow.count()) > 0) {
        return; // Already exists — nothing to do
    }

    // Navigate to the Add Transaction form and fill all fields deterministically
    await page.goto('/transactions/add');
    await page.waitForLoadState('domcontentloaded');

    await page.fill('#Date',         firstDay);
    await page.fill('#Description',  FIXTURE_TX_DESCRIPTION);
    await page.fill('#Amount',       '-25');
    await page.selectOption('#Currency', 'EUR');
    await page.fill('#CategoryName', E2E_CAT_B);   // datalist: server resolves by name

    // Submit via the form's own button (not the logout button in the header which is also type=submit)
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

    // Collect budget IDs where the category name contains "E2E-"
    const e2eBudgetIds = await page.evaluate(() => {
        const rows = document.querySelectorAll('table tbody tr');
        const ids: string[] = [];
        rows.forEach(row => {
            const categoryCell = row.querySelector('td');
            if (categoryCell?.textContent?.includes('E2E-')) {
                const input = row.querySelector('input[name="budgetId"]') as HTMLInputElement | null;
                if (input?.value) ids.push(input.value);
            }
        });
        return ids;
    });

    if (e2eBudgetIds.length === 0) return;

    // Delete each budget via the Razor Pages POST handler
    for (const budgetId of e2eBudgetIds) {
        await page.evaluate(async (id: string) => {
            const tokenEl = document.querySelector('[name="__RequestVerificationToken"]') as HTMLInputElement | null;
            const token   = tokenEl?.value ?? '';

            const fd = new FormData();
            fd.append('budgetId', id);
            fd.append('__RequestVerificationToken', token);

            await fetch('/budgets?handler=Delete', {
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

    // Collect category IDs where the name contains "E2E-"
    const e2eCategoryIds = await page.evaluate(() => {
        const items = document.querySelectorAll('.list-group-item');
        const ids: string[] = [];
        items.forEach(item => {
            const nameEl = item.querySelector('.fw-bold');
            if (nameEl?.textContent?.includes('E2E-')) {
                // Category ID is in the delete form's hidden input or a data attribute
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
        // ── Step 1: Authenticate via dual-path ────────────────────────────────
        const envEmail    = process.env.TEST_USER_EMAIL;
        const envPassword = process.env.TEST_USER_PASSWORD;

        let authenticated = false;
        if (envEmail && envPassword) {
            authenticated = await loginWith(page, envEmail, envPassword);
        }
        if (!authenticated) {
            authenticated = await loginWith(page, SEEDED_EMAIL, SEEDED_PASSWORD);
        }

        if (!authenticated) {
            throw new Error(
                'FIXTURE BLOCKED: both auth paths failed. ' +
                `Seeded user ${SEEDED_EMAIL} did not authenticate — verify the user exists in ` +
                `Supabase auth.users with email_confirmed_at set and confirmation_token = ''. ` +
                `Env vars TEST_USER_EMAIL/TEST_USER_PASSWORD are not set or invalid.`,
            );
        }

        // ── Step 2: Ensure E2E-Budget-Cat-A exists ────────────────────────────
        await ensureCategoryExists(page, E2E_CAT_A);

        // ── Step 3: Ensure E2E-Budget-Cat-B exists ────────────────────────────
        await ensureCategoryExists(page, E2E_CAT_B);

        // ── Step 4: Ensure fixture transaction exists ─────────────────────────
        await ensureFixtureTransactionExists(page);

        // ── Step 5: Yield the provisioned page to the test ───────────────────
        await use(page);
    },
});

export { expect } from '@playwright/test';
