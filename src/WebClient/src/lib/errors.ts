// Convierte los errores del cliente Kiota (ProblemDetails, validación o sin cuerpo) en un mensaje para el usuario.

interface ApiErrorLike {
  responseStatusCode?: number;
  title?: string | null;
  detail?: string | null;
  message?: string;
  errors?: { additionalData?: Record<string, unknown> } | null;
}

export function statusOf(error: unknown): number | undefined {
  return (error as ApiErrorLike | undefined)?.responseStatusCode;
}

export function errorMessage(error: unknown): string {
  if (!error) return 'Ocurrió un error inesperado';
  const e = error as ApiErrorLike;

  const validation = e.errors?.additionalData;
  if (validation && Object.keys(validation).length > 0) {
    const messages = Object.values(validation).flatMap((v) => (Array.isArray(v) ? v : [v])).map(String);
    return messages.join(' ');
  }

  if (e.title) return e.detail ? `${e.title}. ${e.detail}` : e.title;

  switch (e.responseStatusCode) {
    case 401:
      return 'Su sesión terminó; inicie sesión de nuevo';
    case 403:
      return 'No tiene permiso para esta acción';
    case 404:
      return 'No se encontró lo que busca';
  }

  if (error instanceof TypeError) return 'No hay conexión con el servidor';
  return e.message || 'Ocurrió un error inesperado';
}
