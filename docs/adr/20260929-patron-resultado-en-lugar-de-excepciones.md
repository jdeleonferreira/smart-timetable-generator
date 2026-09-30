# Patrón resultado en lugar de excepciones para el flujo

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: aplicación, errores

## Contexto y problema

Usar excepciones para casos esperados (validación, no encontrado, conflicto) es costoso, oculta qué puede fallar y
convierte las excepciones en saltos de control difíciles de seguir.

## Opciones consideradas

1. Excepciones con un manejador global
2. Patrón resultado con `ErrorOr`

## Decisión

Los casos esperados se devuelven como **`ErrorOr<T>`** con errores tipados por agregado (`TimetableErrors.TeacherBusy`).
Las excepciones quedan para situaciones realmente excepcionales. La API convierte los errores a `ProblemDetails`
con `CustomResult.Problem` (400, 404, 409).

## Consecuencias

- Flujo explícito y predecible
- Más código en los endpoints para traducir resultados

## Enlaces

- [ErrorOr](https://github.com/amantinband/error-or)
