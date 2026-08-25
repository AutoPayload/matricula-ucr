using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Línea de la transacción de matrícula: el grupo concreto que cursa la persona estudiante.
/// También guarda la nota final que registra el docente, de modo que el expediente académico
/// se reconstruye recorriendo estas líneas sin necesidad de una tabla de notas aparte.
/// </summary>
public class DetalleMatricula
{
    public int Id { get; set; }

    [Display(Name = "Matrícula")]
    public int MatriculaId { get; set; }
    public Matricula? Matricula { get; set; }

    [Display(Name = "Grupo")]
    public int GrupoId { get; set; }
    public Grupo? Grupo { get; set; }

    [Display(Name = "Fecha de inclusión")]
    public DateTime FechaInclusion { get; set; } = DateTime.Now;

    [Display(Name = "Estado")]
    public EstadoDetalleMatricula Estado { get; set; } = EstadoDetalleMatricula.Activo;

    [Range(0, 100, ErrorMessage = "La nota debe estar entre 0 y 100.")]
    [Display(Name = "Nota final")]
    public int? NotaFinal { get; set; }

    [Display(Name = "Fecha de registro de la nota")]
    public DateTime? FechaRegistroNota { get; set; }

    /// <summary>Nota mínima de aprobación que aplica la universidad.</summary>
    public const int NotaAprobacion = 70;

    /// <summary>Verdadero cuando la nota está registrada y alcanza la nota de aprobación.</summary>
    public bool Aprobado => NotaFinal.HasValue && NotaFinal.Value >= NotaAprobacion;
}
