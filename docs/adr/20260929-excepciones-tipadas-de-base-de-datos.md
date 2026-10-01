# Excepciones tipadas de base de datos

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: ef-core

## Contexto y problema

EF Core lanza `DbUpdateException` genéricas; para saber si fue un duplicado o una referencia inválida hay que
inspeccionar excepciones propias del proveedor.

## Opciones consideradas

1. Inspeccionar las excepciones de SQL Server a mano
2. `EntityFrameworkCore.Exceptions`

## Decisión

Se usa **`EntityFrameworkCore.Exceptions.SqlServer`** (`UseExceptionProcessor`) para obtener excepciones tipadas
como `UniqueConstraintException`.

## Consecuencias

- Manejo de errores de base de datos independiente del proveedor
- Un paquete adicional
