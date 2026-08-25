using System.ComponentModel.DataAnnotations;
using ProyectoFinal_GuillenJose.Validaciones;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Ciclo lectivo. Concentra las fechas que gobiernan la matrícula: fuera de la ventana
/// comprendida entre <see cref="InicioMatricula"/> y <see cref="FinMatricula"/> el sistema
/// rechaza cualquier confirmación, aunque el grupo tenga cupo.
/// </summary>
public class PeriodoAcademico
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código del periodo es obligatorio.")]
    [StringLength(12, MinimumLength = 4)]
    [RegularExpression("^(I|II|III)-[0-9]{4}$", ErrorMessage = "Use el formato de cuatrimestre y año, por ejemplo II-2026.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del periodo es obligatorio.")]
    [StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Inicio de lecciones")]
    public DateTime FechaInicio { get; set; }

    [DataType(DataType.Date)]
    [FechaPosteriorA(nameof(FechaInicio))]
    [Display(Name = "Fin de lecciones")]
    public DateTime FechaFin { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Apertura de matrícula")]
    public DateTime InicioMatricula { get; set; }

    [DataType(DataType.Date)]
    [FechaPosteriorA(nameof(InicioMatricula))]
    [Display(Name = "Cierre de matrícula")]
    public DateTime FinMatricula { get; set; }

    [Display(Name = "Estado")]
    public EstadoPeriodo Estado { get; set; } = EstadoPeriodo.Planificado;

    [Range(6, 24, ErrorMessage = "El tope de créditos debe estar entre 6 y 24.")]
    [Display(Name = "Máximo de créditos por estudiante")]
    public int MaximoCreditos { get; set; } = 18;

    public ICollection<Grupo> Grupos { get; set; } = [];
    public ICollection<Matricula> Matriculas { get; set; } = [];

    /// <summary>
    /// Indica si en este instante se pueden confirmar matrículas. Se evalúa contra la fecha
    /// del servidor y no contra el estado nada más, para que una ventana vencida cierre sola.
    /// </summary>
    public bool AceptaMatricula(DateTime momento) =>
        Estado == EstadoPeriodo.MatriculaAbierta
        && momento.Date >= InicioMatricula.Date
        && momento.Date <= FinMatricula.Date;
}
