import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// La API se sirve por el mismo origen (/api) mediante el proxy de Vite: no hace falta CORS.
// Aspire entrega la dirección de la API en API_URL (o services__api__https__0 / services__api__http__0).
const apiUrl =
  process.env.API_URL ??
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  'https://localhost:7255';

const proxy = { '/api': { target: apiUrl, changeOrigin: true, secure: false } };

export default defineConfig({
  plugins: [react()],
  server: { proxy, port: process.env.PORT ? Number(process.env.PORT) : 5173 },
  preview: { proxy, port: 4173 },
  build: { chunkSizeWarningLimit: 1200 },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    css: false
  }
});
