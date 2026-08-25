using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Cuenta de acceso al sistema. Extiende la entidad de ASP.NET Identity con los datos que la
/// universidad necesita del estudiantado: identificación, nombre completo, carrera y fotografía.
/// El rol se administra con Identity y no como una columna de esta tabla.
/// </summary>
public class Usuario : IdentityUser
{
    [Required(ErrorMessage = "La identificación es obligatoria.")]
    [StringLength(20, MinimumLength = 9, ErrorMessage = "La identificación debe tener entre 9 y 20 caracteres.")]
    [Display(Name = "Identificación")]
    public string Identificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(80)]
    public string Apellidos { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de nacimiento")]
    public DateTime? FechaNacimiento { get; set; }

    /// <summary>Carrera en la que está empadronada la persona estudiante. El personal no la tiene.</summary>
    [Display(Name = "Carrera")]
    public int? CarreraId { get; set; }
    public Carrera? Carrera { get; set; }

    [Display(Name = "Fotografía")]
    public int? FotografiaDocumentoId { get; set; }
    public Documento? FotografiaDocumento { get; set; }

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    public ICollection<Matricula> Matriculas { get; set; } = [];
    public ICollection<Notificacion> Notificaciones { get; set; } = [];

    [Display(Name = "Nombre completo")]
    public string NombreCompleto => Nombre + " " + Apellidos;

    /// <summary>Iniciales que se dibujan cuando la persona no ha cargado fotografía.</summary>
    public string Iniciales =>
        string.Concat(
            Nombre.Length > 0 ? Nombre[..1] : "?",
            Apellidos.Length > 0 ? Apellidos[..1] : "?").ToUpperInvariant();
}
