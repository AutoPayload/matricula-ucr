using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Controladores;

/// <summary>
/// Mantenimiento de carreras y de su plan de estudios. La asociación de cursos a la carrera se
/// administra desde aquí porque es una propiedad del plan, no del curso: el mismo curso de
/// matemática pertenece a tres carreras en ciclos distintos.
/// </summary>
[Authorize(Policy = Politicas.SoloAdministracion)]
public class CarrerasController(
    ContextoMatricula contexto,
    ServicioBitacora bitacora,
    IOptions<OpcionesMatricula> opciones) : Controller
{
    private readonly int _tamanoPagina = opciones.Value.TamanoPagina;

    [HttpGet]
    public async Task<IActionResult> Index(string? texto, bool? activa, int pagina = 1)
    {
        ViewData["Titulo"] = "Carreras";

        var consulta = contexto.Carreras.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var termino = texto.Trim();
            consulta = consulta.Where(c => c.Nombre.Contains(termino)
                                        || c.Codigo.Contains(termino)
                                        || c.TituloOtorgado.Contains(termino));
        }

        if (activa is { } estado)
        {
            consulta = consulta.Where(c => c.Activa == estado);
        }

        ViewBag.Texto = texto;
        ViewBag.Activa = activa;

        return View(await consulta
            .OrderBy(c => c.Nombre)
            .Select(c => new FilaCarrera
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                TituloOtorgado = c.TituloOtorgado,
                CreditosPlan = c.CreditosPlan,
                Activa = c.Activa,
                CantidadCursos = c.CursosCarrera.Count,
                CantidadEstudiantes = c.Estudiantes.Count
            })
            .PaginarAsync(pagina, _tamanoPagina));
    }

    [HttpGet]
    public async Task<IActionResult> Detalles(int id)
    {
        var carrera = await contexto.Carreras
            .AsNoTracking()
            .Include(c => c.CursosCarrera).ThenInclude(cc => cc.Curso)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carrera is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = carrera.Nombre;

        return View(new ModeloPlanEstudios
        {
            Carrera = carrera,
            Plan = [.. carrera.CursosCarrera.OrderBy(cc => cc.Ciclo).ThenBy(cc => cc.Curso!.Codigo)],
            CursosDisponibles = await ListarCursosFueraDelPlanAsync(id),
            CantidadEstudiantes = await contexto.Users.CountAsync(u => u.CarreraId == id)
        });
    }

    [HttpGet]
    public IActionResult Crear()
    {
        ViewData["Titulo"] = "Nueva carrera";
        return View(new Carrera { Activa = true, CreditosPlan = 132 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Carrera carrera)
    {
        ViewData["Titulo"] = "Nueva carrera";

        if (await contexto.Carreras.AnyAsync(c => c.Codigo == carrera.Codigo))
        {
            ModelState.AddModelError(nameof(carrera.Codigo), "Ya existe una carrera con ese código.");
        }

        if (!ModelState.IsValid)
        {
            return View(carrera);
        }

        carrera.FechaRegistro = DateTime.Now;
        contexto.Carreras.Add(carrera);

        bitacora.Registrar("Crear carrera", nameof(Carrera), null, carrera.Nombre);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = $"La carrera {carrera.Nombre} quedó registrada.";
        return RedirectToAction(nameof(Detalles), new { id = carrera.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var carrera = await contexto.Carreras.FindAsync(id);

        if (carrera is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Editar {carrera.Nombre}";
        return View(carrera);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Carrera carrera)
    {
        if (id != carrera.Id)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Editar {carrera.Nombre}";

        if (await contexto.Carreras.AnyAsync(c => c.Codigo == carrera.Codigo && c.Id != id))
        {
            ModelState.AddModelError(nameof(carrera.Codigo), "Ya existe otra carrera con ese código.");
        }

        if (!ModelState.IsValid)
        {
            return View(carrera);
        }

        var original = await contexto.Carreras.FirstOrDefaultAsync(c => c.Id == id);

        if (original is null)
        {
            return NotFound();
        }

        original.Codigo = carrera.Codigo;
        original.Nombre = carrera.Nombre;
        original.Descripcion = carrera.Descripcion;
        original.TituloOtorgado = carrera.TituloOtorgado;
        original.CreditosPlan = carrera.CreditosPlan;
        original.Activa = carrera.Activa;

        bitacora.Registrar("Editar carrera", nameof(Carrera), id.ToString(), carrera.Nombre);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = "Los cambios de la carrera fueron guardados.";
        return RedirectToAction(nameof(Detalles), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var carrera = await contexto.Carreras
            .AsNoTracking()
            .Include(c => c.CursosCarrera)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carrera is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Eliminar {carrera.Nombre}";
        ViewBag.Estudiantes = await contexto.Users.CountAsync(u => u.CarreraId == id);

        return View(carrera);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(int id)
    {
        var carrera = await contexto.Carreras.FindAsync(id);

        if (carrera is null)
        {
            return NotFound();
        }

        // Una carrera con estudiantes empadronados no se borra: se desactiva. Borrarla dejaría
        // sin sentido el expediente de esas personas.
        if (await contexto.Users.AnyAsync(u => u.CarreraId == id))
        {
            carrera.Activa = false;
            bitacora.Registrar("Desactivar carrera", nameof(Carrera), id.ToString(),
                "Tiene estudiantes empadronados, se desactivó en lugar de eliminarse.");
            await contexto.SaveChangesAsync();

            TempData["Aviso"] = "La carrera tiene estudiantes empadronados, así que se desactivó " +
                                "en lugar de eliminarse.";
            return RedirectToAction(nameof(Index));
        }

        contexto.Carreras.Remove(carrera);
        bitacora.Registrar("Eliminar carrera", nameof(Carrera), id.ToString(), carrera.Nombre);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = $"La carrera {carrera.Nombre} fue eliminada.";
        return RedirectToAction(nameof(Index));
    }

    // =================================================================================
    //  Plan de estudios: asociación de cursos a la carrera
    // =================================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarCurso(int id, int cursoId, int ciclo, bool esObligatorio)
    {
        if (!await contexto.Carreras.AnyAsync(c => c.Id == id))
        {
            return NotFound();
        }

        if (await contexto.CursosCarrera.AnyAsync(cc => cc.CarreraId == id && cc.CursoId == cursoId))
        {
            TempData["Error"] = "Ese curso ya forma parte del plan de estudios.";
            return RedirectToAction(nameof(Detalles), new { id });
        }

        contexto.CursosCarrera.Add(new CursoCarrera
        {
            CarreraId = id,
            CursoId = cursoId,
            Ciclo = ciclo < 1 ? 1 : ciclo,
            EsObligatorio = esObligatorio
        });

        bitacora.Registrar("Asociar curso a carrera", nameof(CursoCarrera), id.ToString(),
            $"Curso {cursoId} en el ciclo {ciclo}.");

        await contexto.SaveChangesAsync();

        TempData["Exito"] = "El curso se agregó al plan de estudios.";
        return RedirectToAction(nameof(Detalles), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarCurso(int id, int asociacionId)
    {
        var asociacion = await contexto.CursosCarrera
            .Include(cc => cc.Curso)
            .FirstOrDefaultAsync(cc => cc.Id == asociacionId && cc.CarreraId == id);

        if (asociacion is null)
        {
            return NotFound();
        }

        contexto.CursosCarrera.Remove(asociacion);

        bitacora.Registrar("Quitar curso de carrera", nameof(CursoCarrera), id.ToString(),
            asociacion.Curso?.Codigo);

        await contexto.SaveChangesAsync();

        TempData["Exito"] = "El curso se quitó del plan de estudios.";
        return RedirectToAction(nameof(Detalles), new { id });
    }

    private async Task<List<SelectListItem>> ListarCursosFueraDelPlanAsync(int carreraId) =>
        await contexto.Cursos
            .AsNoTracking()
            .Where(c => c.Activo && !c.CursosCarrera.Any(cc => cc.CarreraId == carreraId))
            .OrderBy(c => c.Codigo)
            .Select(c => new SelectListItem($"{c.Codigo} — {c.Nombre}", c.Id.ToString()))
            .ToListAsync();
}
