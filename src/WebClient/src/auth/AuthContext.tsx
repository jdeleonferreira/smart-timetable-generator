import { useQueryClient } from '@tanstack/react-query';
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, type UserDto } from '../api/client';
import { statusOf } from '../lib/errors';
import { clearSession, readSession, saveSession } from './tokenStore';

export const ROLES = { admin: 'Admin', coordinator: 'Coordinador', teacher: 'Docente' } as const;

interface AuthState {
  user: UserDto | null;
  /** true mientras se verifica una sesión guardada. */
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAdmin: boolean;
  /** Administrador o coordinador. */
  isManager: boolean;
  /** Puede modificar el plan y los horarios de la sede (el coordinador, solo la suya). */
  canManageCampus: (campusId?: string | null) => boolean;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(() => readSession() !== null);

  const logout = useCallback(() => {
    clearSession();
    setUser(null);
    queryClient.clear();
  }, [queryClient]);

  // Recupera la sesión guardada
  useEffect(() => {
    if (!readSession()) return;
    let cancelled = false;
    api.auth.me
      .get()
      .then((me) => {
        if (!cancelled) setUser(me ?? null);
      })
      .catch(() => {
        if (!cancelled) clearSession();
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  // Si el servidor responde 401 (sesión vencida o invalidada), se cierra la sesión
  useEffect(() => {
    const onError = (error: unknown) => {
      if (statusOf(error) === 401 && readSession() !== null) logout();
    };
    const unsubscribeQueries = queryClient.getQueryCache().subscribe((event) => {
      if (event.type === 'updated' && event.action.type === 'error') onError(event.action.error);
    });
    const unsubscribeMutations = queryClient.getMutationCache().subscribe((event) => {
      if (event.type === 'updated' && event.action.type === 'error') onError(event.action.error);
    });
    return () => {
      unsubscribeQueries();
      unsubscribeMutations();
    };
  }, [queryClient, logout]);

  const login = useCallback(async (email: string, password: string) => {
    const token = await api.auth.login.post({ email, password });
    if (!token?.accessToken || !token.expiresAt) throw new Error('Respuesta de inicio de sesión incompleta');
    saveSession({ accessToken: token.accessToken, expiresAt: token.expiresAt.toISOString() });
    setUser(token.user ?? null);
  }, []);

  const value = useMemo<AuthState>(() => {
    const role = user?.role;
    const isAdmin = role === ROLES.admin;
    const isManager = isAdmin || role === ROLES.coordinator;
    return {
      user,
      loading,
      login,
      logout,
      isAdmin,
      isManager,
      canManageCampus: (campusId) => isAdmin || (role === ROLES.coordinator && !!campusId && user?.campusId === campusId)
    };
  }, [user, loading, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth debe usarse dentro de AuthProvider');
  return context;
}
