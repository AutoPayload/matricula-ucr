using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Data;
using ProyectoFinal_GuillenJose.Models;

namespace ProyectoFinal_GuillenJose.Controllers;

[Authorize(Roles = "Administrador")]
public class CarrerasController : Controller
{
    private readonly ApplicationDbContext _context;
    public CarrerasController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        return View(await _context.Carreras.OrderBy(c => c.Nombre).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var carrera = await _context.Carreras.Include(c => c.Cursos).FirstOrDefaultAsync(c => c.Id == id);
        return carrera is null ? NotFound() : View(carrera);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion")] Carrera carrera)
    {
        if (!ModelState.IsValid) return View(carrera);
        _context.Add(carrera);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var carrera = await _context.Carreras.FindAsync(id);
        return carrera is null ? NotFound() : View(carrera);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion")] Carrera carrera)
    {
        if (id != carrera.Id) return NotFound();
        if (!ModelState.IsValid) return View(carrera);
        try
        {
            _context.Update(carrera);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Carreras.AnyAsync(e => e.Id == carrera.Id)) return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var carrera = await _context.Carreras.FirstOrDefaultAsync(c => c.Id == id);
        return carrera is null ? NotFound() : View(carrera);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var carrera = await _context.Carreras.FindAsync(id);
        if (carrera is not null)
        {
            _context.Carreras.Remove(carrera);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
