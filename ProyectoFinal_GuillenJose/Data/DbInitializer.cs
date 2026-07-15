using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Models;

namespace ProyectoFinal_GuillenJose.Data;

public static class DbInitializer
{
    public const string AdministradorRole = "Administrador";
    public const string EstudianteRole = "Estudiante";
    public const string AdminEmail = "admin@universidad.local";
    public const string AdminPassword = "Admin123!";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AdministradorRole, EstudianteRole })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = AdminEmail, Email = AdminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        if (!await userManager.IsInRoleAsync(admin, AdministradorRole))
            await userManager.AddToRoleAsync(admin, AdministradorRole);

        if (await context.Carreras.AnyAsync()) return;

        var sistemas = new Carrera { Nombre = "Ingeniería en Sistemas", Descripcion = "Desarrollo de software, datos e infraestructura tecnológica." };
        var industrial = new Carrera { Nombre = "Ingeniería Industrial", Descripcion = "Optimización de procesos productivos y administrativos." };
        var negocios = new Carrera { Nombre = "Administración de Negocios", Descripcion = "Gestión estratégica de organizaciones y emprendimientos." };
        context.Carreras.AddRange(sistemas, industrial, negocios);

        var ana = new Docente { Nombre = "Ana Rodríguez", Especialidad = "Desarrollo de Software" };
        var carlos = new Docente { Nombre = "Carlos Vargas", Especialidad = "Matemática" };
        var maria = new Docente { Nombre = "María Fernández", Especialidad = "Gestión Empresarial" };
        context.Docentes.AddRange(ana, carlos, maria);
        context.Cursos.AddRange(
            new Curso { Codigo = "SC-101", Nombre = "Programación I", Creditos = 4, Cupos = 30, Carrera = sistemas, Docente = ana },
            new Curso { Codigo = "SC-205", Nombre = "Bases de Datos", Creditos = 4, Cupos = 25, Carrera = sistemas, Docente = ana },
            new Curso { Codigo = "IN-110", Nombre = "Cálculo I", Creditos = 4, Cupos = 35, Carrera = industrial, Docente = carlos },
            new Curso { Codigo = "AD-101", Nombre = "Fundamentos de Administración", Creditos = 3, Cupos = 40, Carrera = negocios, Docente = maria });
        await context.SaveChangesAsync();
    }
}
