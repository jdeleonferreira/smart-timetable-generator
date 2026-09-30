# Mapeo manual en lugar de AutoMapper

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: aplicación

## Contexto y problema

Los mapeadores automáticos ahorran código en casos simples, pero fallan en tiempo de ejecución cuando falta un campo
y complican los casos no triviales.

## Opciones consideradas

1. AutoMapper
2. Mapeo manual con proyecciones y `record`

## Decisión

Los DTO son `record` definidos junto a su consulta y el mapeo es **manual** (`Select(...)` o constructor).

## Consecuencias

- Los errores de mapeo aparecen al compilar
- Algo más de código en cada consulta
