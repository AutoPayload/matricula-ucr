using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Data;
using ProyectoFinal_GuillenJose.Models;

namespace ProyectoFinal_GuillenJose.Controllers;

[Authorize]
public class CursosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CursosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ==================== Administrador CRUD ====================
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Index()
    {
        return View(await _context.Cursos.Include(c => c.Carrera).Include(c => c.Docente).OrderBy(c => c.Nombre).ToListAsync());
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var curso = await _context.Cursos.Include(c => c.Carrera).Include(c => c.Docente).FirstOrDefaultAsync(c => c.Id == id);
        return curso is null ? NotFound() : View(curso);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create([Bind("Nombre,Codigo,Creditos,CarreraId,DocenteId,Cupos")] Curso curso)
    {
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(curso);
        }
        _context.Add(curso);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var curso = await _context.Cursos.FindAsync(id);
        if (curso is null) return NotFound();
        await CargarListasAsync();
        return View(curso);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Codigo,Creditos,CarreraId,DocenteId,Cupos")] Curso curso)
    {
        if (id != curso.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(curso);
        }
        try
        {
            _context.Update(curso);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Cursos.AnyAsync(e => e.Id == curso.Id)) return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var curso = await _context.Cursos.Include(c => c.Carrera).Include(c => c.Docente).FirstOrDefaultAsync(c => c.Id == id);
        return curso is null ? NotFound() : View(curso);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso is not null)
        {
            _context.Cursos.Remove(curso);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // ==================== Estudiante flujo ====================
    [Authorize(Roles = "Estudiante")]
    public async Task<IActionResult> Disponibles()
    {
        var userId = _userManager.GetUserId(User);
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null || user.CarreraId is null)
        {
            ViewBag.CarreraNombre = "(Sin carrera asignada)";
            return View(new List<Curso>());
        }
        var carrera = await _context.Carreras.FindAsync(user.CarreraId);
        ViewBag.CarreraNombre = carrera?.Nombre ?? "(Sin carrera)";
        var cursos = await _context.Cursos
            .Include(c => c.Carrera)
            .Include(c => c.Docente)
            .Where(c => c.CarreraId == user.CarreraId)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        return View(cursos);
    }

    [Authorize(Roles = "Estudiante")]
    public async Task<IActionResult> Detalle(int? id)
    {
        if (id is null) return NotFound();
        var curso = await _context.Cursos
            .Include(c => c.Carrera)
            .Include(c => c.Docente)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (curso is null) return NotFound();
        var periodo = MatriculasController.PeriodoActual();
        ViewBag.Inscritos = await _context.Matriculas.CountAsync(m => m.CursoId == id && m.Periodo == periodo);
        ViewBag.Periodo = periodo;
        return View(curso);
    }

    private async Task CargarListasAsync()
    {
        ViewData["CarreraId"] = new SelectList(await _context.Carreras.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
        ViewData["DocenteId"] = new SelectList(await _context.Docentes.OrderBy(d => d.Nombre).ToListAsync(), "Id", "Nombre");
    }
}
