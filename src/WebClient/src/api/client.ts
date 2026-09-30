import type { AuthenticationProvider, RequestInformation } from '@microsoft/kiota-abstractions';
import { DefaultRequestAdapter } from '@microsoft/kiota-bundle';
import { createApiClient } from './generated/apiClient';
import { currentToken } from '../auth/tokenStore';

/** Agrega el token de la sesión a cada petición. */
const bearerAuthentication: AuthenticationProvider = {
  authenticateRequest: async (request: RequestInformation) => {
    const token = currentToken();
    if (token) request.headers.tryAdd('Authorization', `Bearer ${token}`);
  }
};

function createClient() {
  const adapter = new DefaultRequestAdapter(bearerAuthentication);
  // Misma dirección que la página: en desarrollo Vite reenvía /api a la API (sin CORS)
  adapter.baseUrl = window.location.origin;
  return createApiClient(adapter).api;
}

/** Cliente de la API generado con Kiota (src/api/generated, se regenera con npm run api:generate). */
export const api = createClient();

export * from './generated/models';
