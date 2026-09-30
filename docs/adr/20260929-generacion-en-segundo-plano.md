# Generación de horarios en segundo plano

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: horarios, api

## Contexto y problema

Generar el horario de una sede puede tardar minutos; una petición HTTP que espera ese tiempo se corta y bloquea recursos.

## Opciones consideradas

1. Respuesta directa
2. Solicitud en cola procesada en segundo plano

## Decisión

`POST /api/timetables/{id}/generate` crea una **solicitud** (`GenerationJob`) y responde 202. `GenerationJobWorker`
procesa las solicitudes en cola una a una; el cliente consulta `GET /api/generation-jobs/{id}`. Al reiniciar, las
solicitudes que quedaron en ejecución se marcan como fallidas.

## Consecuencias

- La API responde de inmediato
- El estado y el resumen del resultado quedan guardados
