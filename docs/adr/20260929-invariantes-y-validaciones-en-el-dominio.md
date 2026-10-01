# Validar invariantes en el dominio

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: dominio

## Contexto y problema

Las reglas como "una asignatura no puede repetirse en un grado" o "un docente no puede tener dos clases a la vez"
deben cumplirse siempre, sin depender de que la API o la base de datos las revisen.

## Opciones consideradas

1. Validar solo en la API (FluentValidation)
2. Validar solo con restricciones de base de datos
3. Invariantes en el dominio, validación de forma en la API

## Decisión

Los **agregados validan sus invariantes** con cláusulas de guarda (`ThrowIf…`) para errores de programación y
devuelven `ErrorOr` para reglas de negocio. Las longitudes máximas se definen como constantes del dominio y la
configuración de EF Core las reutiliza. FluentValidation valida solo la forma de las peticiones.

## Consecuencias

- Las reglas de negocio se prueban directamente en `Domain.UnitTests`
- Una sola fuente para los límites de texto
