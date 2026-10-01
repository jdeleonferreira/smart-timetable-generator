# Restricciones duras y preferencias en la generación

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: horarios, motor

## Contexto y problema

Si todas las reglas fueran obligatorias, un plan con más horas de las que caben volvería imposible todo el horario
y el usuario no obtendría nada útil.

## Opciones consideradas

1. Todas las reglas obligatorias
2. Reglas obligatorias más preferencias con prioridades

## Decisión

**Obligatorias**: un curso/docente/espacio por franja, máximo por día y horas seguidas por asignatura, bloques dobles,
franjas bloqueadas y fijadas, máximo diario por recurso (incluidas otras jornadas y sedes del docente).
**Preferencias**, en este orden: ubicar todas las horas, cumplir la carga diaria mínima del curso y no dejar huecos.
Lo que no cabe se informa como "sin ubicar" en la solicitud de generación.

## Consecuencias

- Siempre hay un resultado útil salvo contradicciones reales (por ejemplo, clases fijadas incompatibles)
- Las pruebas deben distinguir entre reglas obligatorias y preferencias
