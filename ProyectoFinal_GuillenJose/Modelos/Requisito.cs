using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Prerrequisito académico: para matricular <see cref="CursoId"/> hay que tener aprobado
/// <see cref="CursoRequisitoId"/>. Se resuelve como auto-referencia sobre la tabla de cursos.
/// </summary>
public class Requisito
{
    public int Id { get; set; }

    [Display(Name = "Curso")]
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }

    [Display(Name = "Curso requisito")]
    public int CursoRequisitoId { get; set; }
    public Curso? CursoRequisito { get; set; }

    [Range(0, 100, ErrorMessage = "La nota mínima debe estar entre 0 y 100.")]
    [Display(Name = "Nota mínima exigida")]
    public int NotaMinima { get; set; } = 70;
}
