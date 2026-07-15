using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Data;
using ProyectoFinal_GuillenJose.Models;

namespace ProyectoFinal_GuillenJose.Controllers;

[Authorize(Roles = "Administrador")]
public class DocentesController : Controller
{
    private readonly ApplicationDbContext _context;
    public DocentesController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        return View(await _context.Docentes.Include(d => d.Cursos).OrderBy(d => d.Nombre).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var docente = await _context.Docentes.Include(d => d.Cursos).ThenInclude(c => c.Carrera).FirstOrDefaultAsync(d => d.Id == id);
        return docente is null ? NotFound() : View(docente);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Especialidad")] Docente docente)
    {
        if (!ModelState.IsValid) return View(docente);
        _context.Add(docente);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var docente = await _context.Docentes.FindAsync(id);
        return docente is null ? NotFound() : View(docente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Especialidad")] Docente docente)
    {
        if (id != docente.Id) return NotFound();
        if (!ModelState.IsValid) return View(docente);
        try
        {
            _context.Update(docente);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Docentes.AnyAsync(e => e.Id == docente.Id)) return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Id == id);
        return docente is null ? NotFound() : View(docente);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var docente = await _context.Docentes.FindAsync(id);
        if (docente is not null)
        {
            _context.Docentes.Remove(docente);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
