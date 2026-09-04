import { defineConfig, devices } from '@playwright/test';

// executablePath is a LaunchOption, not a UseOption — pass it through launchOptions.
// Use the already-installed Chromium browser; overridable via env var.
const CHROMIUM_PATH = process.env.PLAYWRIGHT_CHROMIUM_PATH ||
  'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

const withLaunchOptions = {
  launchOptions: { executablePath: CHROMIUM_PATH },
};

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
    baseURL: process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:3000',
    ...withLaunchOptions,
  },
  projects: [
    {
      name: 'Desktop',
      use: {
        ...devices['Desktop Chrome'],
        ...withLaunchOptions,
      },
    },
  ],
});
