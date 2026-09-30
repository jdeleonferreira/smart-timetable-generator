# Especificaciones en el dominio, una clase por agregado

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: dominio, especificaciones

## Contexto y problema

Las especificaciones definen cómo se carga un agregado. Si viven lejos del agregado quedan desactualizadas, y una
clase por consulta multiplica archivos difíciles de encontrar.

## Opciones consideradas

1. Especificaciones en la capa de aplicación, una clase por consulta
2. Especificaciones en el dominio, una clase por agregado con métodos de fábrica

## Decisión

Cada agregado tiene **una** clase `{Agregado}Spec : SingleResultSpecification<T>` en su carpeta del dominio, con
métodos estáticos por consulta (`StudyPlanSpec.ById`, `StudyPlanSpec.ByYearAndCampus`).

## Consecuencias

- Se encuentran todas las consultas de un agregado en un solo lugar
- El dominio depende de `Ardalis.Specification` (sin EF Core)
