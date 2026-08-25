using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Materia del catálogo institucional. Un curso existe con independencia de la carrera: se vincula
/// a una o varias por medio de <see cref="CursoCarrera"/>, de manera que los cursos de servicio
/// (matemática, comunicación, inglés) no se dupliquen en cada plan de estudios.
/// </summary>
public class Curso
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código del curso es obligatorio.")]
    [StringLength(12, MinimumLength = 4, ErrorMessage = "El código debe tener entre 4 y 12 caracteres.")]
    [RegularExpression("^[A-Z]{2,4}-[0-9]{3,4}$", ErrorMessage = "Use el formato de sigla y número, por ejemplo SC-101.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del curso es obligatorio.")]
    [StringLength(120, MinimumLength = 5, ErrorMessage = "El nombre debe tener entre 5 y 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(600, ErrorMessage = "La descripción no puede superar los 600 caracteres.")]
    [DataType(DataType.MultilineText)]
    public string Descripcion { get; set; } = string.Empty;

    [Range(1, 6, ErrorMessage = "Un curso vale entre 1 y 6 créditos.")]
    [Display(Name = "Créditos")]
    public int Creditos { get; set; }

    [Range(2, 12, ErrorMessage = "Las horas semanales deben estar entre 2 y 12.")]
    [Display(Name = "Horas por semana")]
    public int HorasSemanales { get; set; }

    [Display(Name = "Modalidad")]
    public ModalidadCurso Modalidad { get; set; } = ModalidadCurso.Presencial;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    public ICollection<CursoCarrera> CursosCarrera { get; set; } = [];
    public ICollection<Grupo> Grupos { get; set; } = [];

    /// <summary>Cursos que este curso exige tener aprobados.</summary>
    public ICollection<Requisito> Requisitos { get; set; } = [];

    /// <summary>Cursos que exigen a este curso como requisito.</summary>
    public ICollection<Requisito> EsRequisitoDe { get; set; } = [];
}
