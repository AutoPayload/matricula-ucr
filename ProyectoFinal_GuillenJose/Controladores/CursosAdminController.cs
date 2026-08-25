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
/// Mantenimiento del catálogo de cursos y de la cadena de prerrequisitos. El nombre del
/// controlador lleva el sufijo de administración para no chocar con el catálogo que consulta
/// el estudiantado, que vive en <see cref="CursosController"/>.
/// </summary>
[Authorize(Policy = Politicas.SoloAdministracion)]
[Route("Administracion/Cursos/{action=Index}/{id?}")]
public class CursosAdminController(
    ContextoMatricula contexto,
    ServicioBitacora bitacora,
    IOptions<OpcionesMatricula> opciones) : Controller
{
    private readonly int _tamanoPagina = opciones.Value.TamanoPagina;

    [HttpGet]
    public async Task<IActionResult> Index(string? texto, int? creditos, ModalidadCurso? modalidad,
                                           bool? activo, int pagina = 1)
    {
        ViewData["Titulo"] = "Cursos";

        var consulta = contexto.Cursos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var termino = texto.Trim();
            consulta = consulta.Where(c => c.Nombre.Contains(termino) || c.Codigo.Contains(termino));
        }

        if (creditos is { } valorCreditos)
        {
            consulta = consulta.Where(c => c.Creditos == valorCreditos);
        }

        if (modalidad is { } valorModalidad)
        {
            consulta = consulta.Where(c => c.Modalidad == valorModalidad);
        }

        if (activo is { } valorActivo)
        {
            consulta = consulta.Where(c => c.Activo == valorActivo);
        }

        ViewBag.Texto = texto;
        ViewBag.Creditos = creditos;
        ViewBag.Modalidad = modalidad;
        ViewBag.Activo = activo;

        return View(await consulta
            .OrderBy(c => c.Codigo)
            .Select(c => new FilaCurso
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Creditos = c.Creditos,
                HorasSemanales = c.HorasSemanales,
                Modalidad = c.Modalidad,
                Activo = c.Activo,
                CantidadCarreras = c.CursosCarrera.Count,
                CantidadGrupos = c.Grupos.Count,
                CantidadRequisitos = c.Requisitos.Count
            })
            .PaginarAsync(pagina, _tamanoPagina));
    }

    [HttpGet]
    public async Task<IActionResult> Detalles(int id)
    {
        var curso = await contexto.Cursos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        if (curso is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"{curso.Codigo} · {curso.Nombre}";

        return View(new ModeloFichaCurso
        {
            Curso = curso,
            Requisitos = await contexto.Requisitos.AsNoTracking()
                .Include(r => r.CursoRequisito)
                .Where(r => r.CursoId == id).ToListAsync(),
            EsRequisitoDe = await contexto.Requisitos.AsNoTracking()
                .Include(r => r.Curso)
                .Where(r => r.CursoRequisitoId == id).ToListAsync(),
            Carreras = await contexto.CursosCarrera.AsNoTracking()
                .Include(cc => cc.Carrera)
                .Where(cc => cc.CursoId == id).ToListAsync(),
            Grupos = await contexto.Grupos.AsNoTracking()
                .Include(g => g.Docente)
                .Include(g => g.PeriodoAcademico)
                .Where(g => g.CursoId == id)
                .OrderByDescending(g => g.PeriodoAcademico!.FechaInicio)
                .ThenBy(g => g.NumeroGrupo)
                .ToListAsync(),
            CursosCandidatos = await contexto.Cursos.AsNoTracking()
                .Where(c => c.Id != id && c.Activo && !c.EsRequisitoDe.Any(r => r.CursoId == id))
                .OrderBy(c => c.Codigo)
                .Select(c => new SelectListItem($"{c.Codigo} — {c.Nombre}", c.Id.ToString()))
                .ToListAsync()
        });
    }

    [HttpGet]
    public IActionResult Crear()
    {
        ViewData["Titulo"] = "Nuevo curso";
        return View(new Curso { Activo = true, Creditos = 3, HorasSemanales = 3 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Curso curso)
    {
        ViewData["Titulo"] = "Nuevo curso";

        if (await contexto.Cursos.AnyAsync(c => c.Codigo == curso.Codigo))
        {
            ModelState.AddModelError(nameof(curso.Codigo), "Ya existe un curso con ese código.");
        }

        if (!ModelState.IsValid)
        {
            return View(curso);
        }

        contexto.Cursos.Add(curso);
        bitacora.Registrar("Crear curso", nameof(Curso), null, $"{curso.Codigo} — {curso.Nombre}");
        await contexto.SaveChangesAsync();

        TempData["Exito"] = $"El curso {curso.Codigo} quedó registrado.";
        return RedirectToAction(nameof(Detalles), new { id = curso.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var curso = await contexto.Cursos.FindAsync(id);

        if (curso is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Editar {curso.Codigo}";
        return View(curso);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Curso curso)
    {
        if (id != curso.Id)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Editar {curso.Codigo}";

        if (await contexto.Cursos.AnyAsync(c => c.Codigo == curso.Codigo && c.Id != id))
        {
            ModelState.AddModelError(nameof(curso.Codigo), "Ya existe otro curso con ese código.");
        }

        if (!ModelState.IsValid)
        {
            return View(curso);
        }

        var original = await contexto.Cursos.FirstOrDefaultAsync(c => c.Id == id);

        if (original is null)
        {
            return NotFound();
        }

        original.Codigo = curso.Codigo;
        original.Nombre = curso.Nombre;
        original.Descripcion = curso.Descripcion;
        original.Creditos = curso.Creditos;
        original.HorasSemanales = curso.HorasSemanales;
        original.Modalidad = curso.Modalidad;
        original.Activo = curso.Activo;

        bitacora.Registrar("Editar curso", nameof(Curso), id.ToString(), curso.Codigo);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = "Los cambios del curso fueron guardados.";
        return RedirectToAction(nameof(Detalles), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var curso = await contexto.Cursos
            .AsNoTracking()
            .Include(c => c.Grupos)
            .Include(c => c.CursosCarrera)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (curso is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Eliminar {curso.Codigo}";
        return View(curso);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(int id)
    {
        var curso = await contexto.Cursos.FindAsync(id);

        if (curso is null)
        {
            return NotFound();
        }

        var tieneHistoria = await contexto.Grupos.AnyAsync(g => g.CursoId == id);

        if (tieneHistoria)
        {
            curso.Activo = false;
            bitacora.Registrar("Desactivar curso", nameof(Curso), id.ToString(),
                "Tiene grupos históricos, se desactivó en lugar de eliminarse.");
            await contexto.SaveChangesAsync();

            TempData["Aviso"] = "El curso tiene grupos abiertos o históricos, así que se desactivó " +
                                "en lugar de eliminarse.";
            return RedirectToAction(nameof(Index));
        }

        contexto.Requisitos.RemoveRange(
            contexto.Requisitos.Where(r => r.CursoId == id || r.CursoRequisitoId == id));
        contexto.CursosCarrera.RemoveRange(contexto.CursosCarrera.Where(cc => cc.CursoId == id));
        contexto.Cursos.Remove(curso);

        bitacora.Registrar("Eliminar curso", nameof(Curso), id.ToString(), curso.Codigo);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = $"El curso {curso.Codigo} fue eliminado.";
        return RedirectToAction(nameof(Index));
    }

    // =================================================================================
    //  Prerrequisitos
    // =================================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarRequisito(int id, int cursoRequisitoId, int notaMinima)
    {
        if (id == cursoRequisitoId)
        {
            TempData["Error"] = "Un curso no puede ser requisito de sí mismo.";
            return RedirectToAction(nameof(Detalles), new { id });
        }

        if (await contexto.Requisitos.AnyAsync(r => r.CursoId == id && r.CursoRequisitoId == cursoRequisitoId))
        {
            TempData["Error"] = "Ese requisito ya estaba declarado.";
            return RedirectToAction(nameof(Detalles), new { id });
        }

        // Se rechaza el ciclo directo: si A ya exige B, B no puede exigir A.
        if (await contexto.Requisitos.AnyAsync(r => r.CursoId == cursoRequisitoId && r.CursoRequisitoId == id))
        {
            TempData["Error"] = "No se puede declarar el requisito porque generaría una dependencia circular.";
            return RedirectToAction(nameof(Detalles), new { id });
        }

        contexto.Requisitos.Add(new Requisito
        {
            CursoId = id,
            CursoRequisitoId = cursoRequisitoId,
            NotaMinima = notaMinima is < 0 or > 100 ? DetalleMatricula.NotaAprobacion : notaMinima
        });

        bitacora.Registrar("Agregar requisito", nameof(Requisito), id.ToString(),
            $"Requiere el curso {cursoRequisitoId}.");

        await contexto.SaveChangesAsync();

        TempData["Exito"] = "El requisito quedó declarado.";
        return RedirectToAction(nameof(Detalles), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarRequisito(int id, int requisitoId)
    {
        var requisito = await contexto.Requisitos
            .FirstOrDefaultAsync(r => r.Id == requisitoId && r.CursoId == id);

        if (requisito is null)
        {
            return NotFound();
        }

        contexto.Requisitos.Remove(requisito);
        bitacora.Registrar("Quitar requisito", nameof(Requisito), id.ToString(), requisitoId.ToString());
        await contexto.SaveChangesAsync();

        TempData["Exito"] = "El requisito fue eliminado.";
        return RedirectToAction(nameof(Detalles), new { id });
    }
}
