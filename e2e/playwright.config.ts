import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E Test Configuration for SauronSheet
 * 
 * Tests the Razor Pages frontend application running on .NET 10.
 * Uses Supabase for authentication and data persistence.
 */
export default defineConfig({
  testDir: './tests',
  
  // Run tests in files in parallel
  fullyParallel: false,
  
  // Fail the build on CI if you accidentally left test.only in the source code
  forbidOnly: !!process.env.CI,
  
  // Retry on CI only
  retries: process.env.CI ? 2 : 0,
  
  // Opt out of parallel tests due to shared database state
  workers: 1,
  
  // Reporter to use
  reporter: [
    ['html', { outputFolder: './results/html' }],
    ['list'],
    ['junit', { outputFile: './results/junit/results.xml' }]
  ],
  
  // Shared settings for all the projects below
  use: {
    // Base URL for all tests (HTTP for CI compatibility — no dev cert)
    baseURL: process.env.BASE_URL || 'http://localhost:54100',
    
    // Collect trace when retrying the failed test
    trace: 'on-first-retry',
    
    // Screenshot on failure
    screenshot: 'only-on-failure',
    
    // Video on failure
    video: 'retain-on-failure',
    
    // Browser context options
    viewport: { width: 1280, height: 720 },
    
    // Accept all cookies (for development)
    acceptDownloads: true,
  },
  
  // Configure projects for major browsers
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    
    // Only run additional browsers locally (CI uses chromium only)
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
            name: 'Mobile Chrome',
            use: { ...devices['Pixel 5'] },
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
