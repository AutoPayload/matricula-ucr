using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Models;

public class Carrera
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Descripcion { get; set; } = string.Empty;
    public ICollection<Curso> Cursos { get; set; } = [];
    public ICollection<ApplicationUser> Estudiantes { get; set; } = [];
}
