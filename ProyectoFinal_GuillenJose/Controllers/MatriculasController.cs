using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Data;
using ProyectoFinal_GuillenJose.Models;

namespace ProyectoFinal_GuillenJose.Controllers;

[Authorize(Roles = "Estudiante")]
public class MatriculasController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MatriculasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public static string PeriodoActual()
    {
        var ahora = DateTime.Now;
        var sem = ahora.Month <= 6 ? "I" : "II";
        return $"{sem}-{ahora.Year}";
    }

    public async Task<IActionResult> MisCursos()
    {
        var userId = _userManager.GetUserId(User);
        var matriculas = await _context.Matriculas
            .Include(m => m.Curso!).ThenInclude(c => c.Carrera)
            .Include(m => m.Curso!).ThenInclude(c => c.Docente)
            .Where(m => m.ApplicationUserId == userId)
            .OrderByDescending(m => m.FechaMatricula)
            .ToListAsync();
        ViewBag.Periodo = PeriodoActual();
        return View(matriculas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Matricular(int cursoId)
    {
        var userId = _userManager.GetUserId(User);
        var periodo = PeriodoActual();

        var existe = await _context.Matriculas.AnyAsync(m =>
            m.ApplicationUserId == userId && m.CursoId == cursoId && m.Periodo == periodo);
        if (existe)
        {
            TempData["Error"] = "Ya esta matriculado en este curso para el periodo actual.";
            return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
        }

        var curso = await _context.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId);
        if (curso is null) return NotFound();

        var inscritos = await _context.Matriculas.CountAsync(m => m.CursoId == cursoId && m.Periodo == periodo);
        if (inscritos >= curso.Cupos)
        {
            TempData["Error"] = "No hay cupos disponibles para este curso.";
            return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
        }

        var matricula = new Matricula
        {
            ApplicationUserId = userId!,
            CursoId = cursoId,
            Periodo = periodo,
            FechaMatricula = DateTime.Now
        };
        _context.Matriculas.Add(matricula);
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Matricula realizada correctamente.";
        return RedirectToAction(nameof(MisCursos));
    }
}
