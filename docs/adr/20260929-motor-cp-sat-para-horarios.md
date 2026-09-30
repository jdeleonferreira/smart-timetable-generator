# Google OR-Tools CP-SAT como motor de horarios

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: horarios, motor

## Contexto y problema

Generar horarios es un problema combinatorio con restricciones duras (cruces, intensidad horaria) y preferencias
(sin huecos, carga pareja). Un algoritmo propio sería costoso de construir y no garantiza soluciones óptimas.

## Opciones consideradas

1. Heurística o algoritmo genético propio
2. Programación con restricciones: Google OR-Tools CP-SAT

## Decisión

Se usa **CP-SAT** detrás de la interfaz `ITimetableSolver` (Application) implementada en `CpSatTimetableSolver`
(Infrastructure). El problema se describe de forma independiente del motor (`SchedulingProblem`): ítems con horas,
recursos (curso, docente, espacio), franjas bloqueadas y fijadas y límites diarios. El servicio de generación arma el
problema desde el plan de estudios por jornada y guarda el resultado en el agregado `Timetable`, que vuelve a validar
los cruces.

## Consecuencias

- Soluciones óptimas o con calidad conocida para tamaños reales (24 cursos en 1–3 minutos)
- El motor se puede cambiar sin tocar el dominio
- Dependencia nativa (`Google.OrTools`)

## Enlaces

- [OR-Tools CP-SAT](https://developers.google.com/optimization/cp/cp_solver)
