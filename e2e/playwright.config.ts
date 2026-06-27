import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E Test Configuration for SauronSheet
 * 
 * Tests the Razor Pages frontend application running on .NET 10.
 * Uses Supabase for authentication and data persistence.
 */
export default defineConfig({
  testDir: './tests',
  outputDir: '../test-results/playwright/artifacts',
  
  // Run tests in files in parallel
  fullyParallel: false,
  
  // Fail the build on CI if you accidentally left test.only in the source code
  forbidOnly: !!process.env.CI,
  
  // Retry on CI only
  retries: process.env.CI ? 2 : 0,
  
  // Opt out of parallel tests due to shared database state
  workers: 1,
  
  // Stop immediately after the first failure to save CI time
  maxFailures: 1,
  
  // Reporter to use
  reporter: [
    ['html', { outputFolder: '../test-results/playwright/html' }],
    ['list'],
    ['junit', { outputFile: '../test-results/playwright/junit/results.xml' }]
  ],
  
  // Shared settings for all the projects below
  use: {
    // Base URL for all tests (HTTP for CI compatibility — no dev cert)
    baseURL: process.env.BASE_URL || 'http://localhost:54100',
    
    // Collect trace when retrying the failed test
    trace: 'on-first-retry',
    
    // Screenshot on failure
    screenshot: 'only-on-failure',
    
    // Video on failure (requires ffmpeg — only on CI where browser packages include it)
    video: process.env.CI ? 'retain-on-failure' : 'off',
    
    // Browser context options
    viewport: { width: 1280, height: 720 },
    
    // Accept all cookies (for development)
    acceptDownloads: true,
  },
  
  // Configure projects for major browsers
  projects: [
    {
      name: 'chromium',
      // Use installed Edge/Chrome as fallback when the Playwright headless shell
      // is not available locally (e.g. fresh checkout without `playwright install`).
      // On CI, the channel is left unset so the downloaded headless shell is used.
      use: {
        ...devices['Desktop Chrome'],
        ...(process.env.CI ? {} : { channel: 'msedge' }),
      },
    },
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },

    // Only run additional browsers locally (CI uses Chromium + Mobile Chrome only)
    ...(!process.env.CI
      ? [
          {
            name: 'firefox',
            use: { ...devices['Desktop Firefox'] },
          },
          {
            name: 'webkit',
            use: { ...devices['Desktop Safari'] },
          },
          {
            name: 'Mobile Safari',
            use: { ...devices['iPhone 12'] },
          },
        ]
      : []),
  ],
  
  // Web server to start before running tests
  webServer: {
    command: 'dotnet run --project ../src/SauronSheet.Frontend --urls http://localhost:54100',
    url: 'http://localhost:54100',
    timeout: 120_000,
    reuseExistingServer: !process.env.CI,
  },
});
