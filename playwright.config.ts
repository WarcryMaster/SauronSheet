import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: '.',
  outputDir: './test-results/playwright/artifacts',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  maxFailures: 1,
  reporter: [
    ['html', { outputFolder: './test-results/playwright/html' }],
    ['list'],
    ['junit', { outputFile: './test-results/playwright/junit/results.xml' }]
  ],
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:54100',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: process.env.CI ? 'retain-on-failure' : 'off',
    viewport: { width: 1280, height: 720 },
    acceptDownloads: true,
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        ...(process.env.CI ? {} : { channel: 'msedge' }),
      },
    },
  ],
  webServer: {
    command: 'dotnet run --project ./src/SauronSheet.Frontend --urls http://localhost:54100',
    url: 'http://localhost:54100',
    timeout: 120_000,
    reuseExistingServer: !process.env.CI,
  },
});
