import { Center, Loader } from '@mantine/core';
import type { ReactNode } from 'react';
import { createBrowserRouter, Navigate, Outlet, RouterProvider, useLocation } from 'react-router';
import { AuthProvider, useAuth } from './auth/AuthContext';
import { AppLayout } from './components/AppLayout';
import { SchoolProvider } from './context/SchoolContext';
import { AssignmentsPage } from './pages/AssignmentsPage';
import { LoginPage } from './pages/LoginPage';
import { MyAvailabilityPage } from './pages/MyAvailabilityPage';
import { StudyPlanPage } from './pages/StudyPlanPage';
import { SubjectsPage } from './pages/SubjectsPage';
import { TeachersPage } from './pages/TeachersPage';
import { TimetablePage } from './pages/TimetablePage';
import { TimetablesPage } from './pages/TimetablesPage';
import { UsersPage } from './pages/UsersPage';

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading)
    return (
      <Center mih="100vh">
        <Loader />
      </Center>
    );
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  return <SchoolProvider>{children}</SchoolProvider>;
}

function RequireAdmin({ children }: { children: ReactNode }) {
  const { isAdmin } = useAuth();
  return isAdmin ? children : <Navigate to="/" replace />;
}

function RequireManager({ children }: { children: ReactNode }) {
  const { isManager } = useAuth();
  return isManager ? children : <Navigate to="/" replace />;
}

function Root() {
  return (
    <AuthProvider>
      <Outlet />
    </AuthProvider>
  );
}

const router = createBrowserRouter([
  {
    element: <Root />,
    children: [
      { path: '/login', element: <LoginPage /> },
      {
        element: (
          <RequireAuth>
            <AppLayout />
          </RequireAuth>
        ),
        children: [
          { index: true, element: <Navigate to="/plan" replace /> },
          { path: '/plan', element: <StudyPlanPage /> },
          { path: '/horarios', element: <TimetablesPage /> },
          { path: '/horarios/:timetableId', element: <TimetablePage /> },
          { path: '/asignacion', element: <AssignmentsPage /> },
          {
            path: '/materias',
            element: (
              <RequireAdmin>
                <SubjectsPage />
              </RequireAdmin>
            )
          },
          {
            path: '/docentes',
            element: (
              <RequireManager>
                <TeachersPage />
              </RequireManager>
            )
          },
          { path: '/disponibilidad', element: <MyAvailabilityPage /> },
          {
            path: '/usuarios',
            element: (
              <RequireAdmin>
                <UsersPage />
              </RequireAdmin>
            )
          },
          { path: '*', element: <Navigate to="/plan" replace /> }
        ]
      }
    ]
  }
]);

export function App() {
  return <RouterProvider router={router} />;
}
