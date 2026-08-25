namespace ProyectoFinal_GuillenJose.Configuracion;

/// <summary>
/// Parámetros del token JWT que protege la API interna consumida por las vistas con AJAX.
/// Se leen de la sección "Jwt" de appsettings.json.
/// </summary>
public class OpcionesJwt
{
    public const string Seccion = "Jwt";

    /// <summary>Quién emite el token.</summary>
    public string Emisor { get; set; } = string.Empty;

    /// <summary>Para quién se emite.</summary>
    public string Audiencia { get; set; } = string.Empty;

    /// <summary>Clave de firma simétrica. Debe tener al menos 32 caracteres.</summary>
    public string ClaveSecreta { get; set; } = string.Empty;

    /// <summary>Vigencia del token en minutos.</summary>
    public int MinutosDeVigencia { get; set; } = 60;
}
