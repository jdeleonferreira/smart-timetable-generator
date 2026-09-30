import type { Page, Route } from '@playwright/test';

// Respuestas simuladas de la API con la misma forma que los DTO reales (JSON en camelCase, enums como texto).

export const IDS = {
  year: '11111111-0000-0000-0000-000000000001',
  period1: '11111111-0000-0000-0000-000000000011',
  period2: '11111111-0000-0000-0000-000000000012',
  campus: '22222222-0000-0000-0000-000000000001',
  otherCampus: '22222222-0000-0000-0000-000000000002',
  morning: '22222222-0000-0000-0000-000000000011',
  afternoon: '22222222-0000-0000-0000-000000000012',
  plan: '33333333-0000-0000-0000-000000000001',
  sixth: '44444444-0000-0000-0000-000000000006',
  seventh: '44444444-0000-0000-0000-000000000007',
  math: '55555555-0000-0000-0000-000000000001',
  humanities: '55555555-0000-0000-0000-000000000002',
  social: '55555555-0000-0000-0000-000000000003',
  arithmetic: '66666666-0000-0000-0000-000000000001',
  geometry: '66666666-0000-0000-0000-000000000002',
  spanish: '66666666-0000-0000-0000-000000000003',
  english: '66666666-0000-0000-0000-000000000004',
  citizenship: '66666666-0000-0000-0000-000000000005',
  timetable: '77777777-0000-0000-0000-000000000001',
  job: '88888888-0000-0000-0000-000000000001',
  newItem: 'bbbbbbbb-0000-0000-0000-000000000001',
  course6A: '99999999-0000-0000-0000-000000000061',
  course7A: '99999999-0000-0000-0000-000000000071',
  teacherAna: 'aaaaaaaa-0000-0000-0000-000000000001',
  teacherLuis: 'aaaaaaaa-0000-0000-0000-000000000002'
};

export type Role = 'Admin' | 'Coordinador' | 'Docente';

export const USERS: Record<Role, object> = {
  Admin: { id: 'u-admin', email: 'admin@colegio.local', fullName: 'Administrador', role: 'Admin', campusId: null, teacherId: null, isActive: true },
  Coordinador: { id: 'u-coord', email: 'coordinador@colegio.local', fullName: 'Coordinadora Ruiz', role: 'Coordinador', campusId: IDS.campus, teacherId: null, isActive: true },
  Docente: { id: 'u-doc', email: 'docente@colegio.local', fullName: 'Ana Gómez', role: 'Docente', campusId: null, teacherId: IDS.teacherAna, isActive: true }
};

const item = (id: string, gradeId: string, subjectId: string, hours: number, extra: object = {}) => ({
  id,
  gradeId,
  subjectId,
  deliveryMode: 'Regular',
  weeklyHours: hours,
  targetShiftId: null,
  integratedIntoSubjectId: null,
  note: null,
  maxHoursPerDay: 2,
  maxConsecutiveHours: 2,
  requiredSpaceType: null,
  periodHours: [],
  ...extra
});

export function planDetail(status: 'Draft' | 'Approved' = 'Draft') {
  return {
    id: IDS.plan,
    academicYearId: IDS.year,
    year: 2026,
    campusId: IDS.campus,
    campusName: 'Sede Principal',
    name: 'Plan de estudios 2026 - Sede Principal',
    status,
    approvedAt: status === 'Approved' ? '2026-02-01T12:00:00Z' : null,
    notes: 'Las asignaturas con * se integran en otras áreas.',
    periods: [
      { id: IDS.period1, number: 1, name: 'Periodo 1' },
      { id: IDS.period2, number: 2, name: 'Periodo 2' }
    ],
    shifts: [
      { id: IDS.morning, name: 'Mañana' },
      { id: IDS.afternoon, name: 'Tarde' }
    ],
    grades: [
      { id: IDS.sixth, name: 'Sexto', shortName: '6º', order: 6, weeklyTotal: 14, counterShiftTotal: 0, periodTotals: [
        { academicPeriodId: IDS.period1, weeklyTotal: 14, counterShiftTotal: 0 },
        { academicPeriodId: IDS.period2, weeklyTotal: 13, counterShiftTotal: 0 }
      ] },
      { id: IDS.seventh, name: 'Séptimo', shortName: '7º', order: 7, weeklyTotal: 13, counterShiftTotal: 2, periodTotals: [
        { academicPeriodId: IDS.period1, weeklyTotal: 13, counterShiftTotal: 2 },
        { academicPeriodId: IDS.period2, weeklyTotal: 13, counterShiftTotal: 2 }
      ] }
    ],
    areas: [
      { id: IDS.math, name: 'Matemáticas', order: 0, subjects: [
        { id: IDS.arithmetic, name: 'Aritmética', code: 'ARI', order: 0, isActive: true, items: [
          item('i1', IDS.sixth, IDS.arithmetic, 5, { periodHours: [{ academicPeriodId: IDS.period2, weeklyHours: 4 }] }),
          item('i2', IDS.seventh, IDS.arithmetic, 5)
        ] },
        { id: IDS.geometry, name: 'Geometría', code: 'GEO', order: 1, isActive: true, items: [
          item('i3', IDS.sixth, IDS.geometry, 1),
          item('i8', IDS.seventh, IDS.geometry, 2, { deliveryMode: 'CounterShift', targetShiftId: IDS.afternoon })
        ] }
      ] },
      { id: IDS.humanities, name: 'Humanidades', order: 1, subjects: [
        { id: IDS.spanish, name: 'Lengua castellana', code: 'LEN', order: 0, isActive: true, items: [
          item('i4', IDS.sixth, IDS.spanish, 5),
          item('i5', IDS.seventh, IDS.spanish, 5)
        ] },
        { id: IDS.english, name: 'Inglés', code: 'ING', order: 1, isActive: true, items: [
          item('i6', IDS.sixth, IDS.english, 3),
          item('i7', IDS.seventh, IDS.english, 3)
        ] }
      ] },
      { id: IDS.social, name: 'Ciencias Sociales', order: 2, subjects: [
        { id: IDS.citizenship, name: 'Competencia ciudadana', code: 'CCI', order: 0, isActive: true, items: [
          item('i9', IDS.sixth, IDS.citizenship, 0, { deliveryMode: 'Transversal', integratedIntoSubjectId: IDS.spanish, note: 'Se trabaja en Lengua' })
        ] }
      ] }
    ]
  };
}

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];
const TIMES = [['07:00:00', '07:45:00'], ['07:45:00', '08:30:00'], ['08:30:00', '09:15:00'], ['09:45:00', '10:30:00'], ['10:30:00', '11:15:00']];
const SUBJECTS = [
  { id: IDS.arithmetic, name: 'Aritmética', code: 'ARI', teacher: [IDS.teacherAna, 'Ana Gómez'] },
  { id: IDS.spanish, name: 'Lengua castellana', code: 'LEN', teacher: [IDS.teacherLuis, 'Luis Pérez'] },
  { id: IDS.english, name: 'Inglés', code: 'ING', teacher: [IDS.teacherLuis, 'Luis Pérez'] }
];

export function lessons() {
  const result: object[] = [];
  const courses = [
    [IDS.course6A, '6ºA'],
    [IDS.course7A, '7ºA']
  ];
  courses.forEach(([courseId, courseName], c) =>
    DAYS.forEach((day, d) =>
      TIMES.forEach(([start, end], p) => {
        const subject = SUBJECTS[(d + p + c) % SUBJECTS.length];
        // El mismo docente no puede estar en dos cursos a la vez: 7ºA desplaza una franja
        result.push({
          id: `l-${c}-${d}-${p}`,
          day,
          periodNumber: p + 1,
          start,
          end,
          shiftId: IDS.morning,
          shiftName: 'Mañana',
          courseId,
          courseName,
          subjectId: subject.id,
          subjectName: subject.name,
          subjectCode: subject.code,
          teacherId: subject.teacher[0],
          teacherName: subject.teacher[1],
          spaceId: null,
          spaceName: c === 0 ? 'Salón 101' : 'Salón 201',
          isLocked: false
        });
      })
    )
  );
  return result;
}

export interface MockState {
  requests: { method: string; path: string; body: unknown }[];
  jobPolls: number;
  timetableStatus: 'Draft' | 'Published';
  planStatus: 'Draft' | 'Approved';
}

const json = (route: Route, body: unknown, status = 200) =>
  route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });

/** Simula la API para el rol indicado. Devuelve el registro de peticiones de escritura. */
export async function mockApi(page: Page, role: Role, options: { loginFails?: boolean } = {}): Promise<MockState> {
  const state: MockState = { requests: [], jobPolls: 0, timetableStatus: 'Draft', planStatus: 'Draft' };

  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    const method = request.method();
    if (method !== 'GET') state.requests.push({ method, path, body: request.postDataJSON?.() ?? null });

    if (path === '/api/auth/login') {
      if (options.loginFails)
        return json(route, { title: 'Correo o contraseña incorrectos', status: 401 }, 401);
      return json(route, { accessToken: `token-${role}`, tokenType: 'Bearer', expiresAt: new Date(Date.now() + 8 * 3600_000).toISOString(), user: USERS[role] });
    }
    if (path === '/api/auth/me') return json(route, USERS[role]);
    if (path === '/api/catalog/academic-years')
      return json(route, [{ id: IDS.year, year: 2026, name: '2026', startDate: '2026-01-26', endDate: '2026-11-27', status: 'Active', periods: [
        { id: IDS.period1, number: 1, name: 'Periodo 1', startDate: '2026-01-26', endDate: '2026-06-19' },
        { id: IDS.period2, number: 2, name: 'Periodo 2', startDate: '2026-07-13', endDate: '2026-11-27' }
      ] }]);
    if (path === '/api/catalog/campuses')
      return json(route, [
        { id: IDS.campus, name: 'Sede Principal', shifts: [{ id: IDS.morning, name: 'Mañana', days: 'MondayToFriday', classPeriods: 5 }] },
        { id: IDS.otherCampus, name: 'Sede Norte', shifts: [] }
      ]);
    if (path === '/api/catalog/courses')
      return json(route, [
        { id: IDS.course6A, name: '6ºA', grade: 'Sexto', gradeOrder: 6, campusId: IDS.campus, shiftId: IDS.morning, homeRoomId: null },
        { id: IDS.course7A, name: '7ºA', grade: 'Séptimo', gradeOrder: 7, campusId: IDS.campus, shiftId: IDS.morning, homeRoomId: null }
      ]);
    if (path === '/api/catalog/teachers')
      return json(route, [
        { id: IDS.teacherAna, fullName: 'Ana Gómez', email: 'ana@colegio.local', maxWeeklyHours: 22, maxDailyHours: 6, areas: ['Matemáticas'], isActive: true },
        { id: IDS.teacherLuis, fullName: 'Luis Pérez', email: 'luis@colegio.local', maxWeeklyHours: 22, maxDailyHours: 6, areas: ['Humanidades'], isActive: true }
      ]);
    if (path === '/api/study-plans' && method === 'GET') {
      const campusId = url.searchParams.get('campusId');
      return json(route, campusId && campusId !== IDS.campus ? [] : [
        { id: IDS.plan, academicYearId: IDS.year, year: 2026, campusId: IDS.campus, campusName: 'Sede Principal', name: 'Plan de estudios 2026 - Sede Principal', status: state.planStatus, approvedAt: null, subjectCount: 9 }
      ]);
    }
    if (path === `/api/study-plans/${IDS.plan}` && method === 'GET') return json(route, planDetail(state.planStatus));
    if (path === `/api/study-plans/${IDS.plan}/approve`) {
      state.planStatus = 'Approved';
      return route.fulfill({ status: 204 });
    }
    if (path === `/api/study-plans/${IDS.plan}/items` && method === 'POST') return json(route, { id: IDS.newItem }, 201);
    if (path.startsWith(`/api/study-plans/${IDS.plan}/items/`)) return route.fulfill({ status: 204 });
    if (path === '/api/timetables' && method === 'GET') {
      const all = [{ id: IDS.timetable, name: 'Horario Sede Principal - Periodo 1 2026', status: state.timetableStatus, academicYearId: IDS.year, campusId: IDS.campus, academicPeriodId: IDS.period1, lessonCount: 50, publishedAt: null }];
      return json(route, role === 'Docente' && state.timetableStatus === 'Draft' ? [] : all);
    }
    if (path === `/api/timetables/${IDS.timetable}/lessons`) return json(route, lessons());
    if (path === `/api/timetables/${IDS.timetable}/generate`) return json(route, { jobId: IDS.job }, 202);
    if (path === `/api/timetables/${IDS.timetable}/publish`) {
      state.timetableStatus = 'Published';
      return route.fulfill({ status: 204 });
    }
    if (path === `/api/generation-jobs/${IDS.job}`) {
      state.jobPolls++;
      const finished = state.jobPolls > 1;
      return json(route, {
        id: IDS.job, timetableId: IDS.timetable, status: finished ? 'Succeeded' : 'Running', isFinished: finished,
        requestedAt: new Date().toISOString(), startedAt: new Date().toISOString(), finishedAt: finished ? new Date().toISOString() : null,
        timeLimitSeconds: 180, placedLessons: finished ? 50 : 0, unplacedLessons: 0,
        message: finished ? '- Jornada Mañana: solución óptima en 12.3 s, 6 asignaturas por curso.' : null
      });
    }
    if (path === '/api/users' && method === 'GET') return json(route, Object.values(USERS));
    if (path === '/api/users' && method === 'POST') return json(route, { id: 'u-new' }, 201); // el id de usuario es texto

    return json(route, { title: `Sin simulación para ${method} ${path}` }, 404);
  });

  return state;
}

/** Inicia sesión por la pantalla de ingreso. */
export async function signIn(page: Page, role: Role) {
  await page.goto('/login');
  await page.getByLabel('Correo').fill((USERS[role] as { email: string }).email);
  await page.getByLabel('Contraseña').fill('Clave2026');
  await page.getByRole('button', { name: 'Ingresar' }).click();
}
