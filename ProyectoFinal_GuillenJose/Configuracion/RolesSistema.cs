namespace ProyectoFinal_GuillenJose.Configuracion;

/// <summary>
/// Nombres de los roles y de las políticas de autorización. Se centralizan en constantes para
/// que un cambio de nombre no obligue a revisar cada atributo repartido por los controladores.
/// </summary>
public static class RolesSistema
{
    public const string Administrador = "Administrador";
    public const string Docente = "Docente";
    public const string Estudiante = "Estudiante";

    public static readonly string[] Todos = [Administrador, Docente, Estudiante];
}

/// <summary>Políticas que agrupan uno o varios roles.</summary>
public static class Politicas
{
    public const string SoloAdministracion = "SoloAdministracion";
    public const string SoloDocencia = "SoloDocencia";
    public const string SoloEstudiantado = "SoloEstudiantado";
    public const string PersonalAcademico = "PersonalAcademico";
}

/// <summary>
/// Nombres de los esquemas de autenticación. Se declaran como constantes porque los atributos
/// de autorización solo admiten expresiones constantes, y IdentityConstants expone campos de
/// solo lectura que no sirven para ese uso.
/// </summary>
public static class Esquemas
{
    /// <summary>Cookie que emite ASP.NET Identity al iniciar sesión en el sitio.</summary>
    public const string Cookie = "Identity.Application";

    /// <summary>Token JWT que firma las llamadas a la API interna.</summary>
    public const string Token = "Bearer";
}
