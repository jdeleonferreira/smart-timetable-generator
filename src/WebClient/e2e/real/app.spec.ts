import { expect, test, type Page } from '@playwright/test';

// Contra la API real con los datos de ejemplo del MigrationService (Development).
// La prueba de extremo a extremo de CI ya generó y publicó el horario del Periodo 1 antes de ejecutar estas pruebas.

const shot = (name: string) => `test-results/screens/real-${name}.png`;

async function signIn(page: Page, email: string, password: string) {
  await page.goto('/login');
  await page.getByLabel('Correo').fill(email);
  await page.getByLabel('Contraseña').fill(password);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page).toHaveURL(/\/plan$/);
}

test('el administrador ve el plan de estudios del documento con sus totales', async ({ page }) => {
  await signIn(page, 'admin@colegio.local', 'Admin2026');

  await expect(page.getByRole('heading', { name: /Plan de estudios 2026/ })).toBeVisible();
  await expect(page.getByTestId('total-PRE')).toHaveText('20');
  for (const grade of ['1º', '2º', '3º', '4º', '5º']) await expect(page.getByTestId(`total-${grade}`)).toHaveText('33');
  for (const grade of ['6º', '7º', '8º', '9º', '10º', '11º']) await expect(page.getByTestId(`total-${grade}`)).toHaveText('38');
  await page.screenshot({ path: shot('plan'), fullPage: true });
});

test('el horario publicado muestra cada curso con su intensidad semanal completa', async ({ page }) => {
  await signIn(page, 'coordinador@colegio.local', 'Coordinador2026');

  await page.getByRole('link', { name: 'Horarios' }).click();
  await page.getByRole('row').filter({ hasText: 'Publicado' }).first().click();
  await expect(page.getByRole('tab', { name: 'Por curso' })).toHaveAttribute('aria-selected', 'true');

  await page.getByRole('combobox', { name: 'Curso' }).click();
  await page.getByRole('option', { name: '6ºA', exact: true }).click();
  await expect(page.getByTestId('timetable-grid').getByTestId('lesson')).toHaveCount(38);
  await page.screenshot({ path: shot('horario-6A'), fullPage: true });

  await page.getByRole('combobox', { name: 'Curso' }).click();
  await page.getByRole('option', { name: 'PREA', exact: true }).click();
  await expect(page.getByTestId('timetable-grid').getByTestId('lesson')).toHaveCount(20);

  // Publicado: ya no se genera ni se publica de nuevo
  await expect(page.getByRole('button', { name: 'Generar' })).toHaveCount(0);
});

test('el docente consulta sin acciones de gestión y no entra a usuarios', async ({ page }) => {
  await signIn(page, 'docente@colegio.local', 'Docente2026');

  await expect(page.getByRole('heading', { name: /Plan de estudios 2026/ })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Aprobar' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Usuarios' })).toHaveCount(0);

  await page.getByRole('link', { name: 'Horarios' }).click();
  await page.getByRole('row').filter({ hasText: 'Publicado' }).first().click();
  await expect(page.getByRole('tab', { name: 'Por docente' })).toHaveAttribute('aria-selected', 'true');
  await expect(page.getByTestId('timetable-grid').or(page.getByText('No hay clases para mostrar.'))).toBeVisible();
  await page.screenshot({ path: shot('docente'), fullPage: true });

  await page.goto('/usuarios');
  await expect(page).toHaveURL(/\/plan$/);
});

test('una contraseña incorrecta no inicia sesión', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Correo').fill('admin@colegio.local');
  await page.getByLabel('Contraseña').fill('Incorrecta1');
  await page.getByRole('button', { name: 'Ingresar' }).click();

  await expect(page.getByRole('alert')).toContainText('Correo o contraseña incorrectos');
});
