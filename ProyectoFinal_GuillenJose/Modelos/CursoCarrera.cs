using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Asociación entre un curso y una carrera. Resuelve la relación de muchos a muchos y además
/// guarda en qué ciclo del plan se ubica el curso y si es obligatorio o electivo.
/// </summary>
public class CursoCarrera
{
    public int Id { get; set; }

    [Display(Name = "Carrera")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una carrera.")]
    public int CarreraId { get; set; }
    public Carrera? Carrera { get; set; }

    [Display(Name = "Curso")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un curso.")]
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }

    [Range(1, 12, ErrorMessage = "El ciclo debe estar entre 1 y 12.")]
    [Display(Name = "Ciclo del plan")]
    public int Ciclo { get; set; } = 1;

    [Display(Name = "Es obligatorio")]
    public bool EsObligatorio { get; set; } = true;
}
