# Registrar las decisiones de arquitectura (ADR)

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: documentación

## Contexto y problema

Las decisiones de diseño se olvidan o se revierten sin saber por qué se tomaron. Hace falta dejarlas por escrito,
cerca del código, en un formato liviano que cualquiera pueda leer y mantener.

## Opciones consideradas

1. ADR en Markdown con el formato MADR, gestionados con Log4brains
2. Documentación libre sin formato
3. No documentar decisiones

## Decisión

Se usa **MADR** (Markdown Architectural Decision Records) en `docs/adr`, con nombres `AAAAMMDD-titulo.md` para evitar
conflictos al combinar ramas, y **Log4brains** para navegarlos (`log4brains preview`). Un ADR aceptado no se edita:
si la decisión cambia se escribe uno nuevo que lo reemplaza.

## Consecuencias

- Cada decisión relevante queda con su contexto, opciones y razones
- El historial de decisiones se lee en orden cronológico

## Enlaces

- [MADR](https://adr.github.io/madr/)
- [Log4brains](https://github.com/thomvaill/log4brains)
