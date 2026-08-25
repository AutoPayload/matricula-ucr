using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Persona docente del cuerpo académico. Se mantiene separada de la cuenta de acceso porque la
/// administración registra al docente antes de que exista un usuario en el sistema, y no todo
/// docente necesita entrar al portal. Cuando sí lo hace, <see cref="UsuarioId"/> enlaza ambos registros.
/// </summary>
public class Docente
{
    public int Id { get; set; }

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

    [Required(ErrorMessage = "Indique la especialidad.")]
    [StringLength(120)]
    public string Especialidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo institucional es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo institucional no tiene un formato válido.")]
    [StringLength(150)]
    [Display(Name = "Correo institucional")]
    public string CorreoInstitucional { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [StringLength(20)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    /// <summary>Cuenta de acceso asociada, cuando la persona docente usa el portal.</summary>
    [Display(Name = "Cuenta de acceso")]
    public string? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public ICollection<Grupo> Grupos { get; set; } = [];

    [Display(Name = "Docente")]
    public string NombreCompleto => Nombre + " " + Apellidos;
}
