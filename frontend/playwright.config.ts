import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  use: {
    baseURL: 'http://localhost:3000',
    headless: true,
    trace: 'on-first-retry',
  },
  webServer: process.env.CI ? undefined : undefined,
  timeout: 30_000,
});
