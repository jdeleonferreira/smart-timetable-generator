# EF Core y especificaciones en los casos de uso

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: aplicación, ef-core

## Contexto y problema

Un repositorio genérico sobre EF Core agrega una capa con poco valor e impide proyectar consultas a DTO de forma
eficiente, pero cargar agregados completos (con sus hijos) sin una convención lleva a errores.

## Opciones consideradas

1. Repositorios y especificaciones
2. EF Core con especificaciones
3. EF Core sin abstracciones

## Decisión

Los comandos y consultas usan **EF Core directamente** a través de `IApplicationDbContext`, y cargan los agregados
con **especificaciones** (`WithSpecification(TimetableSpec.ById(id))`). Las consultas proyectan a DTO.

## Consecuencias

- Consultas eficientes y agregados cargados siempre de la misma forma
- La capa de aplicación depende de EF Core; los casos de uso se prueban con una base real (SQLite o SQL Server)
