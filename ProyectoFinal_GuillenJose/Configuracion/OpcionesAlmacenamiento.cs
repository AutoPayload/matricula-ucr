namespace ProyectoFinal_GuillenJose.Configuracion;

/// <summary>
/// Reglas del almacén de archivos: dónde se guardan, qué se acepta y hasta qué tamaño.
/// Se leen de la sección "Almacenamiento" de appsettings.json.
/// </summary>
public class OpcionesAlmacenamiento
{
    public const string Seccion = "Almacenamiento";

    /// <summary>
    /// Carpeta física del almacén, relativa a la raíz del proyecto. Queda fuera de wwwroot a
    /// propósito: ningún archivo se sirve de forma directa, todos pasan por el control de acceso.
    /// </summary>
    public string CarpetaRaiz { get; set; } = "Almacenamiento";

    public long TamanoMaximoBytes { get; set; } = 5 * 1024 * 1024;

    public string[] ExtensionesImagen { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];

    public string[] ExtensionesDocumento { get; set; } = [".pdf", ".docx", ".odt"];
}
