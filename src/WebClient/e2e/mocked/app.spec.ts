import { expect, test } from '@playwright/test';
import { IDS, mockApi, signIn } from './mockApi';

// La interfaz con la API simulada: inicio de sesión, plan de estudios, horarios y permisos por rol.

const shot = (name: string) => `test-results/screens/${name}.png`;

test('credenciales incorrectas muestran el mensaje de la API', async ({ page }) => {
  await mockApi(page, 'Admin', { loginFails: true });
  await signIn(page, 'Admin');

  await expect(page.getByRole('alert')).toContainText('Correo o contraseña incorrectos');
  await expect(page).toHaveURL(/\/login$/);
});

test('sin sesión, cualquier página lleva al inicio de sesión', async ({ page }) => {
  await mockApi(page, 'Admin');
  await page.goto('/horarios');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Horarios escolares' })).toBeVisible();
  await page.screenshot({ path: shot('01-login') });
});

test('el plan de estudios se ve como en el documento: áreas, asignaturas, grados y totales', async ({ page }) => {
  await mockApi(page, 'Admin');
  await signIn(page, 'Admin');

  await expect(page).toHaveURL(/\/plan$/);
  await expect(page.getByRole('heading', { name: 'Plan de estudios 2026 - Sede Principal' })).toBeVisible();
  await expect(page.getByText('Borrador', { exact: true })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: '6º' })).toBeVisible();
  await expect(page.getByTestId('total-6º')).toHaveText('14');
  await expect(page.getByTestId('total-7º')).toHaveText('13');
  await expect(page.getByTestId('cell-Aritmética-6º')).toHaveText('5');
  await expect(page.getByTestId('cell-Competencia ciudadana-6º')).toHaveText('T*');
  await expect(page.getByTestId('cell-Geometría-7º')).toContainText('CJ');
  await expect(page.getByText('En contrajornada')).toBeVisible();
  await page.screenshot({ path: shot('02-plan'), fullPage: true });

  // Por periodo: la IH ajustada y el total del periodo
  await page.getByText('Periodo 2', { exact: true }).click();
  await expect(page.getByTestId('cell-Aritmética-6º')).toHaveText('4');
  await expect(page.getByTestId('total-6º')).toHaveText('13');
});

test('el coordinador agrega una asignatura a un grado desde la celda vacía', async ({ page }) => {
  const api = await mockApi(page, 'Coordinador');
  await signIn(page, 'Coordinador');

  await page.getByTestId('cell-Competencia ciudadana-7º').click();
  const modal = page.getByRole('dialog');
  await expect(modal).toContainText('Competencia ciudadana · Séptimo');
  await modal.getByLabel('Intensidad horaria semanal').fill('3');
  await modal.getByLabel('Máx. horas por día').fill('1');
  await page.screenshot({ path: shot('03-editor') });
  await modal.getByRole('button', { name: 'Agregar' }).click();

  await expect(page.getByText('Competencia ciudadana en 7º guardada')).toBeVisible();
  const added = api.requests.find((r) => r.method === 'POST' && r.path.endsWith('/items'));
  expect(added?.body).toMatchObject({ gradeId: IDS.seventh, subjectId: IDS.citizenship, deliveryMode: 'Regular', weeklyHours: 3 });
  const distribution = api.requests.find((r) => r.path.endsWith(`/items/${IDS.newItem}/distribution`));
  expect(distribution?.body).toMatchObject({ maxHoursPerDay: 1, maxConsecutiveHours: null });
});

test('el coordinador aprueba el plan y queda cerrado a cambios', async ({ page }) => {
  await mockApi(page, 'Coordinador');
  await signIn(page, 'Coordinador');

  await page.getByRole('button', { name: 'Aprobar' }).click();

  await expect(page.getByText('Aprobado', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Reabrir' })).toBeVisible();
  await page.getByTestId('cell-Competencia ciudadana-7º').click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
});

test('en otra sede el coordinador solo consulta', async ({ page }) => {
  await mockApi(page, 'Coordinador');
  await signIn(page, 'Coordinador');

  await page.getByRole('combobox', { name: 'Sede' }).click();
  await page.getByRole('option', { name: 'Sede Norte' }).click();

  await expect(page.getByText('No hay plan de estudios 2026 para Sede Norte')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Crear plan de estudios' })).toHaveCount(0);
  await expect(page.getByText('Solo consulta en esta sede')).toBeVisible();
});

test('el horario se genera, se ve por curso, por docente e institucional, y se publica', async ({ page }) => {
  const api = await mockApi(page, 'Admin');
  await signIn(page, 'Admin');

  await page.getByRole('link', { name: 'Horarios' }).click();
  await page.getByText('Horario Sede Principal - Periodo 1 2026').click();
  await expect(page).toHaveURL(new RegExp(`/horarios/${IDS.timetable}$`));

  // Vista por curso: lunes a viernes, 5 franjas con hora
  const grid = page.getByTestId('timetable-grid');
  await expect(grid.getByRole('columnheader')).toHaveText(['Hora', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes']);
  await expect(grid.getByText('1ª hora')).toBeVisible();
  await expect(grid.getByText('7:00–7:45')).toBeVisible();
  await expect(grid.getByTestId('lesson')).toHaveCount(25);
  await page.screenshot({ path: shot('04-horario-curso'), fullPage: true });

  // Generar: espera el resultado
  await page.getByRole('button', { name: 'Generar' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Generar' }).click();
  await expect(page.getByTestId('generation-status')).toContainText('Completado', { timeout: 15_000 });
  await expect(page.getByTestId('generation-status')).toContainText('50 clases ubicadas');
  expect(api.requests.some((r) => r.path.endsWith('/generate'))).toBe(true);

  // Por docente
  await page.getByRole('tab', { name: 'Por docente' }).click();
  await page.getByRole('combobox', { name: 'Docente' }).click();
  await page.getByRole('option', { name: 'Ana Gómez' }).click();
  const anaLessons = page.getByTestId('timetable-grid').getByTestId('lesson');
  await expect(anaLessons.first()).toContainText('Aritmética');
  await expect(anaLessons.filter({ hasNotText: 'Aritmética' })).toHaveCount(0);

  // Institucional: un día, una columna por curso
  await page.getByRole('tab', { name: 'Institucional' }).click();
  await expect(page.getByTestId('timetable-grid').getByRole('columnheader')).toHaveText(['Hora', '6ºA', '7ºA']);
  await page.screenshot({ path: shot('05-horario-institucional'), fullPage: true });

  await page.getByRole('button', { name: 'Publicar' }).click();
  await expect(page.getByText('Horario publicado: ya lo ven los docentes')).toBeVisible();
});

test('el docente ve el plan y su horario publicado, sin acciones de gestión', async ({ page }) => {
  const api = await mockApi(page, 'Docente');
  api.timetableStatus = 'Published';
  await signIn(page, 'Docente');

  await expect(page.getByRole('heading', { name: 'Plan de estudios 2026 - Sede Principal' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Aprobar' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Usuarios' })).toHaveCount(0);
  await page.getByTestId('cell-Aritmética-6º').click();
  await expect(page.getByRole('dialog')).toHaveCount(0);

  await page.getByRole('link', { name: 'Horarios' }).click();
  await page.getByText('Horario Sede Principal - Periodo 1 2026').click();
  await expect(page.getByRole('button', { name: 'Generar' })).toHaveCount(0);
  await expect(page.getByRole('tab', { name: 'Por docente' })).toHaveAttribute('aria-selected', 'true');
  await expect(page.getByTestId('timetable-grid').getByTestId('lesson').first()).toContainText('Aritmética');
  await page.screenshot({ path: shot('06-docente'), fullPage: true });
});

test('solo el administrador ve y crea usuarios', async ({ page }) => {
  const api = await mockApi(page, 'Admin');
  await signIn(page, 'Admin');

  await page.getByRole('link', { name: 'Usuarios' }).click();
  await expect(page.getByRole('cell', { name: 'Coordinadora Ruiz', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Nuevo usuario' }).click();

  const modal = page.getByRole('dialog');
  await modal.getByRole('combobox', { name: 'Rol' }).click();
  await page.getByRole('option', { name: 'Coordinador' }).click();
  await modal.getByLabel('Nombre completo').fill('Coordinador Norte');
  await modal.getByLabel('Correo').fill('norte@colegio.local');
  await modal.getByRole('combobox', { name: 'Sede que coordina' }).click();
  await page.getByRole('option', { name: 'Sede Norte' }).click();
  await modal.getByLabel('Contraseña inicial').fill('Norte2026');
  await page.screenshot({ path: shot('07-usuarios') });
  await modal.getByRole('button', { name: 'Crear' }).click();

  await expect(page.getByText('Usuario norte@colegio.local creado')).toBeVisible();
  const created = api.requests.find((r) => r.method === 'POST' && r.path === '/api/users');
  expect(created?.body).toMatchObject({ role: 'Coordinador', campusId: IDS.otherCampus, teacherId: null, email: 'norte@colegio.local' });
});

test('el coordinador no entra a usuarios', async ({ page }) => {
  await mockApi(page, 'Coordinador');
  await signIn(page, 'Coordinador');
  await expect(page).toHaveURL(/\/plan$/);

  await page.goto('/usuarios');
  await expect(page).toHaveURL(/\/plan$/);
});
