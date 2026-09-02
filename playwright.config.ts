import { defineConfig, devices } from '@playwright/test';

// Use the already-installed Chromium 1187 browser.
// Playwright package version mismatch prevents auto-download in this environment.
const CHROMIUM_PATH = process.env.PLAYWRIGHT_CHROMIUM_PATH ||
  'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

export default defineConfig({
  testDir: './e2e-tests',
  timeout: 60_000,
  fullyParallel: false,
  reporter: [['list']],
  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'off',
    headless: true,
    executablePath: CHROMIUM_PATH,
    baseURL: process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:3000',
  },
  projects: [
    {
      name: 'Desktop',
      use: { ...devices['Desktop Chrome'], executablePath: CHROMIUM_PATH },
    },
  ],
});
