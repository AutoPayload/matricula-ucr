using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Models;

public class Curso
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Codigo { get; set; } = string.Empty;
    [Range(1, 10)] public int Creditos { get; set; }
    [Display(Name = "Carrera")] public int CarreraId { get; set; }
    public Carrera? Carrera { get; set; }
    [Display(Name = "Docente")] public int? DocenteId { get; set; }
    public Docente? Docente { get; set; }
    [Range(1, 100)] public int Cupos { get; set; }
    public ICollection<Matricula> Matriculas { get; set; } = [];
}
