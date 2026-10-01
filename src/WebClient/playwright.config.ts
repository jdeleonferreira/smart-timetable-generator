import { defineConfig, devices } from '@playwright/test';

// Dos grupos de pruebas:
//  - mocked: la interfaz con respuestas de la API simuladas (no necesita el backend).
//  - real:   contra la API real; se ejecutan cuando E2E_BASE_URL apunta a la aplicación en marcha
//            (la prueba de extremo a extremo de CI levanta SQL Server, la API y `vite preview`).
const realBaseUrl = process.env.E2E_BASE_URL;
const executablePath = process.env.CHROMIUM_PATH || undefined;

export default defineConfig({
  testDir: 'e2e',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    locale: 'es-CO',
    viewport: { width: 1440, height: 900 },
    launchOptions: { executablePath }
  },
  projects: [
    { name: 'mocked', testDir: 'e2e/mocked', use: { ...devices['Desktop Chrome'], baseURL: 'http://localhost:4173', viewport: { width: 1440, height: 900 }, launchOptions: { executablePath } } },
    ...(realBaseUrl
      ? [{ name: 'real', testDir: 'e2e/real', use: { ...devices['Desktop Chrome'], baseURL: realBaseUrl, viewport: { width: 1440, height: 900 }, launchOptions: { executablePath } } }]
      : [])
  ],
  webServer: realBaseUrl
    ? undefined
    : { command: 'npm run build && npx vite preview --port 4173 --strictPort', url: 'http://localhost:4173', reuseExistingServer: !process.env.CI, timeout: 120_000 }
});
