# Smart Timetable Generator

📝 Overview
Smart Timetable Generator (STG) es una aplicación AI-driven diseñada para automatizar la generación de horarios escolares.
Su objetivo es reducir drásticamente el tiempo que los coordinadores académicos dedican a planificar, al tiempo que respeta las reglas curriculares, la disponibilidad de docentes y las capacidades de aulas.

STG combina:

Un motor de optimización de alto rendimiento para garantizar que las restricciones se cumplan.

Un módulo de IA que permite expresar reglas y preferencias en lenguaje natural y explica decisiones y conflictos.

🎯 Key Features

Curriculum-based Scheduling
Importa la malla curricular (IH) para cada grado y distribuye automáticamente las clases en la semana.

Resource-aware Allocation
Tiene en cuenta disponibilidad de docentes, tamaño y tipo de aulas, y necesidades especiales (por ejemplo, laboratorios o bloques dobles).

AI-assisted Rules
Permite a los usuarios definir reglas con lenguaje natural como

“Química en 10° y 11° debe ir en bloques dobles y nunca en la última hora”
y STG las traduce a restricciones formales para el solver.

Conflict Explanations
La IA explica los conflictos y las decisiones tomadas por el motor de optimización, ayudando a comprender el porqué de cada asignación.

Quick Fix Suggestions (futuro cercano)
Proporciona opciones automáticas para resolver huecos o choques de recursos.

Multi-view Schedules
Presenta los horarios generados en vistas por Grado, Docente o Aula, con posibilidad de exportar a Excel o PDF.
