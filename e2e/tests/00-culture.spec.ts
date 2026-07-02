import { test, expect } from '@playwright/test';

/**
 * E2E Tests for language switching and culture persistence (REQ-LOC-020, REQ-LOC-080).
 *
 * This spec deliberately overrides the project-wide storageState: it clears
 * cookies before each test so it can verify culture selection from a clean
 * state and confirm cookie persistence.
 */

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
    await page.locator('#desktopNav [data-testid="lang-switcher"]').click();
    const spanishOption = page.locator('#desktopNav [data-testid="lang-switcher-option-es"]');
    await spanishOption.waitFor({ state: 'visible' });
    await spanishOption.click();

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
    await page.locator('#desktopNav [data-testid="lang-switcher"]').click();
    await page.locator('#desktopNav [data-testid="lang-switcher-option-es"]').waitFor({ state: 'visible' });
    await page.locator('#desktopNav [data-testid="lang-switcher-option-es"]').click();
    await expect(page.locator('html')).toHaveAttribute('lang', 'es');

    await page.locator('#desktopNav [data-testid="lang-switcher"]').click();
    await page.locator('#desktopNav [data-testid="lang-switcher-option-en"]').waitFor({ state: 'visible' });
    await page.locator('#desktopNav [data-testid="lang-switcher-option-en"]').click();

    await expect(page.locator('html')).toHaveAttribute('lang', 'en');
    await expect(page.locator('[data-testid="login-submit"]')).toContainText('Sign in');
  });

  test('TC-CULTURE-004: Culture cookie persists across sessions', async ({ page, context }) => {
    await page.locator('#desktopNav [data-testid="lang-switcher"]').click();
    await page.locator('#desktopNav [data-testid="lang-switcher-option-es"]').waitFor({ state: 'visible' });
    await page.locator('#desktopNav [data-testid="lang-switcher-option-es"]').click();
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
