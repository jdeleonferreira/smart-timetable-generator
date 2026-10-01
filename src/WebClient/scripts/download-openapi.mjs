// Descarga el documento OpenAPI de la API en ejecución a openapi.json (luego: npm run api:generate).
// Uso: API_URL=https://localhost:7255 npm run api:openapi
import { writeFile } from 'node:fs/promises';

process.env.NODE_TLS_REJECT_UNAUTHORIZED ??= '0'; // certificado de desarrollo
const apiUrl = process.env.API_URL ?? 'https://localhost:7255';
const response = await fetch(`${apiUrl}/openapi/v1.json`);
if (!response.ok) throw new Error(`No se pudo leer ${apiUrl}/openapi/v1.json: ${response.status}`);
const document = await response.json();
await writeFile('openapi.json', JSON.stringify(document, null, 2) + '\n');
console.log(`openapi.json actualizado desde ${apiUrl}`);
