using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Oferta concreta de un curso en un periodo: es aquí donde se asigna la persona docente,
/// el horario, el aula y el cupo. El estudiantado no matricula cursos, matricula grupos.
/// </summary>
public class Grupo
{
    public int Id { get; set; }

    [Display(Name = "Curso")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un curso.")]
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }

    [Display(Name = "Docente")]
    public int? DocenteId { get; set; }
    public Docente? Docente { get; set; }

    [Display(Name = "Periodo")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un periodo académico.")]
    public int PeriodoAcademicoId { get; set; }
    public PeriodoAcademico? PeriodoAcademico { get; set; }

    [Range(1, 20, ErrorMessage = "El número de grupo debe estar entre 1 y 20.")]
    [Display(Name = "Número de grupo")]
    public int NumeroGrupo { get; set; } = 1;

    [Required(ErrorMessage = "Indique el horario.")]
    [StringLength(60)]
    public string Horario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el aula.")]
    [StringLength(30)]
    public string Aula { get; set; } = string.Empty;

    [Range(5, 60, ErrorMessage = "El cupo debe estar entre 5 y 60 personas.")]
    [Display(Name = "Cupo máximo")]
    public int CupoMaximo { get; set; } = 30;

    [Display(Name = "Estado")]
    public EstadoGrupo Estado { get; set; } = EstadoGrupo.Abierto;

    /// <summary>Documento con el programa del curso que publica la persona docente.</summary>
    [Display(Name = "Programa del curso")]
    public int? ProgramaDocumentoId { get; set; }
    public Documento? ProgramaDocumento { get; set; }

    [Display(Name = "Acta cerrada")]
    public bool ActaCerrada { get; set; }

    [Display(Name = "Fecha de cierre del acta")]
    public DateTime? FechaCierreActa { get; set; }

    public ICollection<DetalleMatricula> Detalles { get; set; } = [];

    /// <summary>Identificador legible que se muestra en las listas: por ejemplo SC-101 grupo 02.</summary>
    public string Etiqueta => (Curso?.Codigo ?? "Curso") + " grupo " + NumeroGrupo.ToString("00");
}
