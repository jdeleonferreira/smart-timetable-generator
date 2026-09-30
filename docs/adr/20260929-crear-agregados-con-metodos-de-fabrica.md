# Crear los agregados con métodos de fábrica

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: dominio, ef-core

## Contexto y problema

Un agregado debe estar siempre en un estado válido, EF Core tiene que poder materializarlo desde la base de datos
y la creación desde la aplicación debe poder disparar eventos de dominio.

## Opciones consideradas

1. Métodos de fábrica estáticos (`Create`) con constructor privado
2. Constructores públicos
3. Propiedades `required init`

## Decisión

Se usan **métodos de fábrica** (`Campus.Create`, `StudyPlan.CreateCopy`) y un constructor privado sin parámetros
para EF Core. Las entidades hijas se crean con métodos `internal` desde su agregado.

## Consecuencias

- EF Core materializa sin disparar eventos de dominio
- No se puede crear un agregado inválido desde fuera
- Hay que usar `null!` en propiedades que el método de fábrica siempre asigna
