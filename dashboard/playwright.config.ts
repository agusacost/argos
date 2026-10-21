import { defineConfig, devices } from '@playwright/test';

// Suite E2E de integración contra el stack real (docker compose up -d).
// Ver plans/TEST_INTEGRACION_DASHBOARD.md y docs/FUNCIONALIDADES_DASHBOARD.md.
const BASE_URL = process.env.E2E_BASE_URL ?? 'http://localhost:3001';

export default defineConfig({
  testDir: './e2e',
  globalSetup: './e2e/global-setup.ts',
  globalTeardown: './e2e/global-teardown.ts',
  // Estado compartido (Qdrant, una corrida k6 a la vez) → serial.
  workers: 1,
  fullyParallel: false,
  retries: 1,
  timeout: 45_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: BASE_URL,
    viewport: { width: 1440, height: 900 },
    trace: 'on-first-retry',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'api', testMatch: /api\.smoke\.spec\.ts/ },
    {
      name: 'ui',
      testIgnore: /api\.smoke\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
  ],
});
