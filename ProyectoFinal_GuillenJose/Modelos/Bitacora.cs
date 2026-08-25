using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Registro de auditoría. Cada operación que cambia datos deja aquí una línea con quién la hizo,
/// desde dónde y sobre qué entidad. Es la tabla que permite responder por una matrícula anulada
/// o por una nota modificada después del cierre.
/// </summary>
public class Bitacora
{
    public long Id { get; set; }

    [Display(Name = "Fecha y hora")]
    public DateTime FechaHora { get; set; } = DateTime.Now;

    [Display(Name = "Usuario")]
    public string? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    [StringLength(150)]
    [Display(Name = "Nombre de la persona usuaria")]
    public string NombreUsuario { get; set; } = "Anónimo";

    [StringLength(40)]
    public string Rol { get; set; } = "Sin rol";

    [Required]
    [StringLength(60)]
    [Display(Name = "Acción")]
    public string Accion { get; set; } = string.Empty;

    [StringLength(60)]
    public string Entidad { get; set; } = string.Empty;

    [StringLength(40)]
    [Display(Name = "Identificador de la entidad")]
    public string? EntidadId { get; set; }

    [StringLength(500)]
    public string? Detalle { get; set; }

    [StringLength(45)]
    [Display(Name = "Dirección IP")]
    public string? DireccionIp { get; set; }
}
