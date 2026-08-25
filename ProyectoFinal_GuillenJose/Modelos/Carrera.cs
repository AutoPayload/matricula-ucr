using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Oferta académica de la universidad. Agrupa cursos por medio de <see cref="CursoCarrera"/>
/// y es el criterio con el que el estudiantado ve el catálogo que le corresponde.
/// </summary>
public class Carrera
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código de la carrera es obligatorio.")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "El código debe tener entre 2 y 10 caracteres.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la carrera es obligatorio.")]
    [StringLength(120, MinimumLength = 5, ErrorMessage = "El nombre debe tener entre 5 y 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(600, ErrorMessage = "La descripción no puede superar los 600 caracteres.")]
    [DataType(DataType.MultilineText)]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el título que otorga la carrera.")]
    [StringLength(150)]
    [Display(Name = "Título que otorga")]
    public string TituloOtorgado { get; set; } = string.Empty;

    [Range(60, 220, ErrorMessage = "El plan de estudios debe tener entre 60 y 220 créditos.")]
    [Display(Name = "Créditos del plan")]
    public int CreditosPlan { get; set; }

    [Display(Name = "Activa")]
    public bool Activa { get; set; } = true;

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public ICollection<CursoCarrera> CursosCarrera { get; set; } = [];
    public ICollection<Usuario> Estudiantes { get; set; } = [];
}
