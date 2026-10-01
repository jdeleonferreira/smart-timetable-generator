# Minimal APIs con TypedResults

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: api

## Contexto y problema

Los endpoints deben devolver exactamente los códigos de estado que anuncian en OpenAPI; con `Results` es fácil
anunciar un 201 y devolver un 200.

## Opciones consideradas

1. Minimal APIs con `Results`
2. Minimal APIs con `TypedResults`
3. Controladores MVC

## Decisión

Se usan **Minimal APIs** agrupadas por recurso (`MapApiGroup`) con **`TypedResults`** y los métodos de extensión
`ProducesGet`, `ProducesPost`, `ProducesPut` y `ProducesDelete` para documentar las respuestas de forma consistente.

## Consecuencias

- El compilador ayuda a que la documentación coincida con la respuesta
- Endpoints delgados que solo envían comandos o consultas
