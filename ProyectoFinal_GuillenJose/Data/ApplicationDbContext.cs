using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Models;

namespace ProyectoFinal_GuillenJose.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Curso>().HasIndex(c => c.Codigo).IsUnique();
        builder.Entity<Matricula>().HasIndex(m => new { m.ApplicationUserId, m.CursoId, m.Periodo }).IsUnique();
        builder.Entity<ApplicationUser>().HasOne(u => u.Carrera).WithMany(c => c.Estudiantes).HasForeignKey(u => u.CarreraId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Curso>().HasOne(c => c.Carrera).WithMany(c => c.Cursos).HasForeignKey(c => c.CarreraId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Curso>().HasOne(c => c.Docente).WithMany(d => d.Cursos).HasForeignKey(c => c.DocenteId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Matricula>().HasOne(m => m.ApplicationUser).WithMany(u => u.Matriculas).HasForeignKey(m => m.ApplicationUserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Matricula>().HasOne(m => m.Curso).WithMany(c => c.Matriculas).HasForeignKey(m => m.CursoId).OnDelete(DeleteBehavior.Restrict);
    }
}
