const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const ctx = await browser.newContext({ baseURL: 'http://localhost:54100' });
  const page = await ctx.newPage();

  // Login
  await page.goto('/auth/login');
  await page.fill('input[type="email"]', 'e2e@saurontest.local');
  await page.fill('input[type="password"]', 'E2eTestPass9!');
  await page.click('button[type="submit"]');
  await page.waitForURL(/dashboard/i, { timeout: 15000 });

  await page.goto('/categories');
  await page.waitForLoadState('domcontentloaded');

  await page.click('button[aria-label="Add new category"]');
  await page.locator('#createName').waitFor({ state: 'visible', timeout: 5000 });
  await page.fill('#createName', 'E2E-Debug-Cat');
  await page.selectOption('#createType', '1');
  await page.locator('#createIcon').selectOption({ index: 1 });
  await page.waitForTimeout(500);

  // Capture request body
  page.on('request', req => {
    if (req.method() === 'POST') {
      const body = req.postData();
      console.log('POST body (first 800 chars):', body ? body.substring(0,800) : 'null/empty');
    }
  });

  page.on('response', async resp => {
    if (resp.request().method() === 'POST') {
      const body = await resp.text().catch(() => '<err>');
      console.log('Response:', resp.status(), body.substring(0,200));
    }
  });

  await page.click('#createSubmitBtn');
  await page.waitForTimeout(3000);
  await browser.close();
})().catch(e => console.error(e));
