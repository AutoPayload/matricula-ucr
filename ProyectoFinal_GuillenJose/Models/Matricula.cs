using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Models;

public class Matricula
{
    public int Id { get; set; }
    [Required] public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser? ApplicationUser { get; set; }
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }
    [Required, StringLength(20)] public string Periodo { get; set; } = string.Empty;
    [Display(Name = "Fecha de matrícula")] public DateTime FechaMatricula { get; set; }
}
