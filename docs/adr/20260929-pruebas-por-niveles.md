# Pruebas por niveles

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: pruebas

## Contexto y problema

Las reglas del dominio, los casos de uso, la API y el motor de horarios necesitan pruebas distintas: unas rápidas y
aisladas, otras que recorran la aplicación completa.

## Opciones consideradas

1. Solo pruebas unitarias
2. Solo pruebas de extremo a extremo
3. Pruebas por niveles

## Decisión

- **Dominio** (`Domain.UnitTests`): agregados e invariantes, sin infraestructura.
- **Arquitectura** (`Architecture.Tests`): dependencias entre capas y convenciones con NetArchTest.
- **Generación** (`Scheduling.Tests`): motor CP-SAT con validadores independientes, y servicio y casos de uso reales
  sobre SQLite en memoria.
- **Integración** (`WebApi.IntegrationTests`): la API completa contra SQL Server en contenedor (Testcontainers + Respawn),
  con las rutas escritas a mano para detectar cambios en el contrato. `IWebApiMarker` permite a `WebApplicationFactory`
  localizar la API.
Las aserciones se escriben con **AwesomeAssertions**, gratuita para uso comercial y con la misma sintaxis `Should()`.

## Consecuencias

- Cada nivel detecta un tipo distinto de error
- Las pruebas de integración requieren Docker

## Enlaces

- [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions)
- [Testcontainers](https://dotnet.testcontainers.org/)
