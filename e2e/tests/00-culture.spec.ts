import { test, expect, type Page } from '@playwright/test';

/**
 * E2E Tests for language switching and culture persistence (REQ-LOC-020, REQ-LOC-080).
 *
 * This spec deliberately overrides the project-wide storageState: it clears
 * cookies before each test so it can verify culture selection from a clean
 * state and confirm cookie persistence.
 */

/**
 * Click the language switcher and select the given language.
 *
 * On mobile viewports (< lg), the nav is inside an MDB offcanvas panel.
 * This helper opens the offcanvas first if needed, so the selector works on
 * both Desktop Chrome and Mobile Chrome/Safari.
 */
async function switchCulture(page: Page, language: 'es' | 'en'): Promise<void> {
  // Determine viewport from page context (deterministic — not affected by DOM timing).
  const viewport = page.viewportSize();
  const isMobile = viewport !== null && viewport.width < 992; // Bootstrap lg breakpoint

  if (isMobile) {
    const hamburger = page.locator('.navbar-toggler');
    await hamburger.click();
    await page.locator('#mobileOffcanvas').waitFor({ state: 'visible', timeout: 5000 });

    // MDB dropdowns don't open inside offcanvas — click the form submit button
    // directly via JS instead of navigating the dropdown menu.
    await page.evaluate((lang) => {
      const btn = document.querySelector<HTMLButtonElement>(
        `[data-testid="lang-switcher-option-${lang}"]`
      );
      btn?.click();
    }, language);

    // Wait for the culture cookie POST to complete and redirect back.
    await page.waitForURL(/\/auth\/login/, { timeout: 10000 });
    return;
  }

  // Desktop: normal dropdown interaction.
  await page.locator('#desktopNav [data-testid="lang-switcher"]').click();
  const option = page.locator(`#desktopNav [data-testid="lang-switcher-option-${language}"]`);
  await option.waitFor({ state: 'visible' });
  await option.click();
}

test.describe('Language Switcher', () => {
  test.beforeEach(async ({ page, context }) => {
    await context.clearCookies();
    await page.goto('/auth/login');
  });

  test('TC-CULTURE-001: Default culture is English', async ({ page }) => {
    await expect(page.locator('html')).toHaveAttribute('lang', 'en');
    await expect(page.locator('[data-testid="login-submit"]')).toContainText('Sign in');
  });

  test('TC-CULTURE-002: Switch to Spanish updates UI and sets cookie', async ({ page, context }) => {
    await switchCulture(page, 'es');

    await expect(page).toHaveURL(/\/auth\/login/);
    await expect(page.locator('html')).toHaveAttribute('lang', 'es');
    await expect(page.locator('[data-testid="login-submit"]')).toContainText('Iniciar sesión');

    const cookies = await context.cookies();
    const cultureCookie = cookies.find((c) => c.name === '.AspNetCore.Culture');
    expect(cultureCookie).toBeDefined();
    expect(cultureCookie!.value).toContain('es-ES');
    expect(cultureCookie!.expires).toBeGreaterThan(Math.floor(Date.now() / 1000) + 86400 * 300);
  });

  test('TC-CULTURE-003: Switch back to English updates UI', async ({ page }) => {
    await switchCulture(page, 'es');
    await expect(page.locator('html')).toHaveAttribute('lang', 'es');

    await switchCulture(page, 'en');

    await expect(page.locator('html')).toHaveAttribute('lang', 'en');
    await expect(page.locator('[data-testid="login-submit"]')).toContainText('Sign in');
  });

  test('TC-CULTURE-004: Culture cookie persists across sessions', async ({ page, context }) => {
    await switchCulture(page, 'es');
    await expect(page.locator('html')).toHaveAttribute('lang', 'es');

    // Simulate a new session by clearing only the auth cookies and reloading.
    const cookies = await context.cookies();
    const cultureCookie = cookies.find((c) => c.name === '.AspNetCore.Culture');
    expect(cultureCookie).toBeDefined();

    await context.clearCookies();
    if (cultureCookie) {
      await context.addCookies([cultureCookie]);
    }

    await page.goto('/auth/login');
    await expect(page.locator('html')).toHaveAttribute('lang', 'es');
    await expect(page.locator('[data-testid="login-submit"]')).toContainText('Iniciar sesión');
  });
});
