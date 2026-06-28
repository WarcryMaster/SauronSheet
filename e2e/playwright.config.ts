import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E Test Configuration for SauronSheet
 * 
 * Tests the Razor Pages frontend application running on .NET 10.
 * Uses Supabase for authentication and data persistence.
 *
 * Auth strategy:
 *   The `setup` project logs in once and saves storage state to `.auth/user.json`.
 *   All test projects depend on `setup` and use `storageState` to skip the login step,
 *   saving ~3-4s per test file.
 *   The login spec (01-login.spec.ts) clears cookies via `context.clearCookies()`
 *   in its beforeEach to start unauthenticated.
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
    // ── Auth setup — logs in once, saves cookies for all test projects ──
    {
      name: 'setup',
      testDir: '.',
      testMatch: 'auth.setup.ts',
    },

    // ── Chromium test projects ──
    {
      name: 'chromium',
      dependencies: ['setup'],
      // Use installed Edge/Chrome as fallback when the Playwright headless shell
      // is not available locally (e.g. fresh checkout without `playwright install`).
      // On CI, the channel is left unset so the downloaded headless shell is used.
      use: {
        ...devices['Desktop Chrome'],
        storageState: '.auth/user.json',
        ...(process.env.CI ? {} : { channel: 'msedge' }),
      },
    },
    {
      name: 'Mobile Chrome',
      dependencies: ['setup'],
      use: { ...devices['Pixel 5'], storageState: '.auth/user.json' },
    },

    // Only run additional browsers locally (CI uses Chromium + Mobile Chrome only)
    ...(!process.env.CI
      ? [
          {
            name: 'firefox',
            dependencies: ['setup'],
            use: { ...devices['Desktop Firefox'], storageState: '.auth/user.json' },
          },
          {
            name: 'webkit',
            dependencies: ['setup'],
            use: { ...devices['Desktop Safari'], storageState: '.auth/user.json' },
          },
          {
            name: 'Mobile Safari',
            dependencies: ['setup'],
            use: { ...devices['iPhone 12'], storageState: '.auth/user.json' },
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
