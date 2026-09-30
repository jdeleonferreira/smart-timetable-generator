namespace SmartTimetableGenerator.Application.UseCases.Users.Common;

public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "User.NotFound",
        "El usuario no existe");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "User.InvalidCredentials",
        "Correo o contraseña incorrectos");

    public static readonly Error LockedOut = Error.Unauthorized(
        "User.LockedOut",
        "La cuenta está bloqueada temporalmente por varios intentos fallidos; intente más tarde");

    public static readonly Error DuplicateEmail = Error.Conflict(
        "User.DuplicateEmail",
        "Ya existe un usuario con ese correo");

    public static readonly Error InvalidRole = Error.Validation(
        "User.InvalidRole",
        "El rol debe ser Admin, Coordinador o Docente");

    public static readonly Error CampusRequired = Error.Validation(
        "User.CampusRequired",
        "Un coordinador debe tener una sede asignada");

    public static readonly Error TeacherRequired = Error.Validation(
        "User.TeacherRequired",
        "Un docente debe estar vinculado a su registro de docente");

    public static readonly Error TeacherAlreadyLinked = Error.Conflict(
        "User.TeacherAlreadyLinked",
        "Ese docente ya tiene un usuario");

    public static readonly Error LastAdmin = Error.Conflict(
        "User.LastAdmin",
        "Debe quedar al menos un administrador activo");

    public static readonly Error WrongPassword = Error.Validation(
        "User.WrongPassword",
        "La contraseña actual no es correcta");

    public static Error PasswordRejected(string description) => Error.Validation(
        "User.PasswordRejected",
        description);
}
