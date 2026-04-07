import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E Configuration — MinhasFinancas
 *
 * Pre-requisites:
 *  - API running on http://localhost:5000
 *  - Frontend running on http://localhost:5173
 *  - Run: docker-compose up (from the application repo)
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: false, // serial to avoid race conditions on shared DB
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['list'],
  ],
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10_000,
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Automatic server startup (optional — comment out if starting manually)
  // webServer: [
  //   {
  //     command: 'cd ../app && docker-compose up api',
  //     url: 'http://localhost:5000/swagger',
  //     reuseExistingServer: true,
  //   },
  //   {
  //     command: 'cd ../app && docker-compose up web',
  //     url: 'http://localhost:5173',
  //     reuseExistingServer: true,
  //   },
  // ],
});
