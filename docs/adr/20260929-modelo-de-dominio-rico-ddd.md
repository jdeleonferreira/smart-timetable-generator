# Modelo de dominio rico con patrones tácticos de DDD

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: dominio, ddd

## Contexto y problema

El generador de horarios tiene reglas de negocio complejas (plan de estudios, cruces, cargas, calendario).
Un modelo anémico dispersa esas reglas en servicios y las hace difíciles de proteger.

## Opciones consideradas

1. Modelo anémico (entidades solo con datos)
2. Modelo rico con agregados, entidades, objetos de valor y eventos de dominio

## Decisión

Se usa un **modelo rico**: cada agregado (`AggregateRoot<TId>`) protege sus invariantes y expone métodos con
nombres del negocio (`StudyPlan.AddItem`, `Timetable.MoveLesson`). Las entidades hijas solo se modifican a través de su
agregado, los objetos de valor son inmutables y los cambios relevantes se publican como eventos de dominio.
El lenguaje del dominio se documenta en `docs/domain.md`.

## Consecuencias

- Las reglas viven en un solo lugar y se prueban sin base de datos
- Requiere más diseño inicial que un modelo anémico
- Las pruebas de arquitectura verifican que los tipos del dominio hereden de las clases base correctas
