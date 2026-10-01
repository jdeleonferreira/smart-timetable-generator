# Autenticación con ASP.NET Identity y JWT, y permisos en la capa de aplicación

- Estado: aceptada
- Fecha: 2026-09-30
- Etiquetas: seguridad, api

## Contexto y problema

La API la consumen un cliente web y, más adelante, otros clientes generados con Kiota. Hay tres roles:
administrador (toda la institución), coordinador (el plan y los horarios de su sede) y docente (consulta).
Se pidió la opción más económica de implementar en la primera versión, sin servicios externos.

Los permisos del coordinador dependen de la sede del recurso (un plan o un horario), que solo se conoce al cargarlo.

## Opciones consideradas

1. Proveedor externo (Entra ID, Auth0, Keycloak)
2. ASP.NET Identity con los endpoints de `MapIdentityApi` (tokens propios, registro abierto)
3. ASP.NET Identity para las cuentas y tokens JWT emitidos por la API
4. Permisos solo con políticas en los endpoints / permisos en la capa de aplicación

## Decisión

**Cuentas**: ASP.NET Identity sobre la misma base de datos (sin costo ni dependencia externa). Los usuarios los crea
el administrador; no hay registro abierto. El correo es el nombre de usuario; cada usuario tiene un rol. El coordinador
guarda su sede; el docente, su registro de docente.

**Tokens**: JWT firmados con HMAC-SHA256 (llave de al menos 32 caracteres en `Jwt:SigningKey`, fuera del código en
producción), con vigencia de 8 horas. El token lleva el sello de seguridad del usuario y en cada petición se verifica
que el usuario siga activo y que el sello no haya cambiado: cambiar la contraseña, el rol o la sede, o desactivar al
usuario, invalida sus sesiones. Tras 5 intentos fallidos la cuenta se bloquea 5 minutos.

**Permisos en la capa de aplicación**: los comandos declaran `[Authorize(Roles = …)]` y un comportamiento de MediatR
lo aplica (respuestas 401/403 con el patrón resultado). La regla de sede se comprueba en el caso de uso, después de
cargar el recurso. En la API todos los endpoints exigen sesión salvo el inicio de sesión.

## Consecuencias

- Los permisos se prueban sin HTTP, igual que el resto de los casos de uso
- Una prueba de arquitectura exige que todo comando (salvo el inicio de sesión) tenga `[Authorize]`
- Cada petición autenticada consulta el usuario en la base (costo bajo con pocos usuarios)
- Migrar a un proveedor externo después solo cambia la emisión y validación de tokens, no los permisos
