# Identificadores fuertemente tipados con Vogen

- Estado: aceptada
- Fecha: 2026-09-29
- Etiquetas: dominio, ef-core

## Contexto y problema

Usar `Guid` para todos los identificadores permite confundir, por ejemplo, el id de un curso con el de un docente
(obsesión por los tipos primitivos). Escribir estos tipos a mano en EF Core es repetitivo.

## Opciones consideradas

1. Tipos escritos a mano con conversores de EF Core
2. Generación con Vogen

## Decisión

Cada entidad tiene su identificador `[ValueObject<Guid>]` generado por **Vogen** (`CourseId`, `TeacherId`…),
creado con `Guid.CreateVersion7()`. Los conversores de EF Core se registran en `VogenEfCoreConverters`.

## Consecuencias

- El compilador impide mezclar identificadores
- Un paquete adicional (generador de código)

## Enlaces

- [Vogen](https://github.com/SteveDunn/Vogen)
