using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Models;

public class ApplicationUser : IdentityUser
{
    [Display(Name = "Carrera")] public int? CarreraId { get; set; }
    public Carrera? Carrera { get; set; }
    public ICollection<Matricula> Matriculas { get; set; } = [];
}
