using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Metadatos de un archivo resguardado por el sistema. El contenido binario no se guarda en la
/// base de datos: vive en el almacén de archivos y aquí queda la referencia, junto con el
/// resumen SHA-256 que permite detectar duplicados y verificar que el archivo no fue alterado.
/// </summary>
public class Documento
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    [Display(Name = "Nombre original")]
    public string NombreOriginal { get; set; } = string.Empty;

    /// <summary>Nombre con el que se guardó en disco. Es un identificador único sin relación con el original.</summary>
    [Required]
    [StringLength(100)]
    [Display(Name = "Nombre almacenado")]
    public string NombreAlmacenado { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Tipo de contenido")]
    public string TipoContenido { get; set; } = string.Empty;

    [Display(Name = "Tamaño en bytes")]
    public long TamanoBytes { get; set; }

    [Required]
    [StringLength(64)]
    [Display(Name = "Resumen SHA-256")]
    public string HashSha256 { get; set; } = string.Empty;

    [Display(Name = "Categoría")]
    public CategoriaDocumento Categoria { get; set; }

    /// <summary>Persona propietaria del archivo. Es la base del control de acceso a la descarga.</summary>
    [Display(Name = "Propietario")]
    public string? PropietarioUsuarioId { get; set; }
    public Usuario? PropietarioUsuario { get; set; }

    [Display(Name = "Fecha de carga")]
    public DateTime FechaCarga { get; set; } = DateTime.Now;

    /// <summary>Tamaño legible para las vistas, sin formatear en la propia vista.</summary>
    public string TamanoLegible => TamanoBytes < 1024
        ? TamanoBytes + " B"
        : TamanoBytes < 1048576
            ? (TamanoBytes / 1024d).ToString("N1") + " KB"
            : (TamanoBytes / 1048576d).ToString("N1") + " MB";
}
