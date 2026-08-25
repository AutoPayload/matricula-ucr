using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Aviso dirigido a una persona usuaria. El sistema los emite cuando se abre la matrícula,
/// cuando se confirma una inscripción y cuando el docente publica una nota.
/// </summary>
public class Notificacion
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Destinatario")]
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario? Usuario { get; set; }

    [Required]
    [StringLength(120)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [StringLength(400)]
    public string Mensaje { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Enlace { get; set; }

    [Display(Name = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [Display(Name = "Leída")]
    public bool Leida { get; set; }
}
