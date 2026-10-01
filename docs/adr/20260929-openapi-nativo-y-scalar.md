# OpenAPI nativo de .NET y Scalar

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: api, openapi

## Contexto y problema

La API necesita un documento OpenAPI (del que el cliente web genera su cliente con Kiota) y una interfaz para probarla.

## Opciones consideradas

1. NSwag con Swagger UI
2. `Microsoft.AspNetCore.OpenApi` con Swagger UI
3. `Microsoft.AspNetCore.OpenApi` con Scalar

## Decisión

Se usa **`Microsoft.AspNetCore.OpenApi`** para el documento y **Scalar** como interfaz (`/scalar/v1`).

## Consecuencias

- Biblioteca oficial de .NET para el documento
- Un paquete adicional (`Scalar.AspNetCore`)

## Enlaces

- [Scalar](https://github.com/scalar/scalar)
- [OpenAPI en ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview)
