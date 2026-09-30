// Guarda la sesión en el navegador (token JWT y su vencimiento).
const KEY = 'horarios.session';

export interface StoredSession {
  accessToken: string;
  expiresAt: string;
}

export function readSession(): StoredSession | null {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    const session = JSON.parse(raw) as StoredSession;
    if (!session.accessToken || new Date(session.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(KEY);
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

export function saveSession(session: StoredSession): void {
  try {
    localStorage.setItem(KEY, JSON.stringify(session));
  } catch {
    // Sin almacenamiento (modo privado estricto): la sesión dura mientras la pestaña esté abierta
  }
}

export function clearSession(): void {
  try {
    localStorage.removeItem(KEY);
  } catch {
    // nada que borrar
  }
}

export function currentToken(): string | null {
  return readSession()?.accessToken ?? null;
}
