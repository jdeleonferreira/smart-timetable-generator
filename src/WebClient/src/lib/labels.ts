// Textos en español para los valores de la API.
import type { DeliveryMode, SpaceType, StudyPlanStatus } from '../api/client';

export const ROLE_LABELS: Record<string, string> = {
  Admin: 'Administrador',
  Coordinador: 'Coordinador',
  Docente: 'Docente'
};

export const DELIVERY_LABELS: Record<DeliveryMode, string> = {
  Regular: 'Regular',
  Transversal: 'Transversal',
  CounterShift: 'Contrajornada'
};

export const DELIVERY_HINTS: Record<DeliveryMode, string> = {
  Regular: 'Se dicta en la jornada del curso',
  Transversal: 'Sin horas propias; se trabaja dentro de otra asignatura',
  CounterShift: 'Se dicta en otra jornada de la sede'
};

export const PLAN_STATUS_LABELS: Record<StudyPlanStatus, string> = {
  Draft: 'Borrador',
  Approved: 'Aprobado'
};

export const TIMETABLE_STATUS_LABELS: Record<string, string> = {
  Draft: 'Borrador',
  Published: 'Publicado',
  Archived: 'Archivado'
};

export const JOB_STATUS_LABELS: Record<string, string> = {
  Queued: 'En cola',
  Running: 'Generando',
  Succeeded: 'Completado',
  PartiallySucceeded: 'Completado con clases sin ubicar',
  Infeasible: 'Sin solución posible',
  Failed: 'Falló',
  Cancelled: 'Cancelado'
};

export const SPACE_TYPE_LABELS: Partial<Record<SpaceType, string>> = {
  Classroom: 'Salón',
  Library: 'Biblioteca',
  ComputerLab: 'Sala de informática',
  Laboratory: 'Laboratorio',
  SportsField: 'Cancha',
  Auditorium: 'Auditorio',
  Other: 'Otro'
};
