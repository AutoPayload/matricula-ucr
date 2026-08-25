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
/// Mantenimiento de la oferta de grupos. Es la pantalla donde se asigna la persona docente a
/// cada curso del periodo, que es una de las funciones que pide el enunciado del proyecto.
///
/// Al guardar se verifica que la persona docente no quede con dos grupos en el mismo horario:
/// es un choque que la oficina de registro descubriría tarde y que aquí se atrapa de una vez.
/// </summary>
[Authorize(Policy = Politicas.SoloAdministracion)]
public class GruposController(
    ContextoMatricula contexto,
    ServicioBitacora bitacora,
    IOptions<OpcionesMatricula> opciones) : Controller
{
    private readonly int _tamanoPagina = opciones.Value.TamanoPagina;

    [HttpGet]
    public async Task<IActionResult> Index(int? periodoId, int? docenteId, string? texto,
                                           EstadoGrupo? estado, int pagina = 1)
    {
        ViewData["Titulo"] = "Grupos";

        var consulta = contexto.Grupos.AsNoTracking().AsQueryable();

        if (periodoId is { } valorPeriodo)
        {
            consulta = consulta.Where(g => g.PeriodoAcademicoId == valorPeriodo);
        }

        if (docenteId is { } valorDocente)
        {
            consulta = consulta.Where(g => g.DocenteId == valorDocente);
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var termino = texto.Trim();
            consulta = consulta.Where(g => g.Curso!.Nombre.Contains(termino)
                                        || g.Curso.Codigo.Contains(termino)
                                        || g.Aula.Contains(termino));
        }

        if (estado is { } valorEstado)
        {
            consulta = consulta.Where(g => g.Estado == valorEstado);
        }

        await CargarListasAsync();

        ViewBag.PeriodoId = periodoId;
        ViewBag.DocenteId = docenteId;
        ViewBag.Texto = texto;
        ViewBag.EstadoFiltro = estado;

        return View(await consulta
            .OrderByDescending(g => g.PeriodoAcademico!.FechaInicio)
            .ThenBy(g => g.Curso!.Codigo)
            .ThenBy(g => g.NumeroGrupo)
            .Select(g => new FilaGrupo
            {
                Id = g.Id,
                CodigoCurso = g.Curso!.Codigo,
                NombreCurso = g.Curso.Nombre,
                NumeroGrupo = g.NumeroGrupo,
                Periodo = g.PeriodoAcademico!.Codigo,
                Docente = g.Docente == null ? "Sin asignar" : g.Docente.Nombre + " " + g.Docente.Apellidos,
                Horario = g.Horario,
                Aula = g.Aula,
                CupoMaximo = g.CupoMaximo,
                Estado = g.Estado,
                ActaCerrada = g.ActaCerrada,
                Inscritos = g.Detalles.Count(d => d.Estado == EstadoDetalleMatricula.Activo
                                               && d.Matricula!.Estado != EstadoMatricula.Anulada)
            })
            .PaginarAsync(pagina, _tamanoPagina));
    }

    [HttpGet]
    public async Task<IActionResult> Crear(int? periodoId)
    {
        ViewData["Titulo"] = "Nuevo grupo";
        await CargarListasAsync();

        var periodo = periodoId
            ?? (await contexto.PeriodosAcademicos
                .OrderByDescending(p => p.FechaInicio)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync() ?? 0);

        return View(new Grupo
        {
            PeriodoAcademicoId = periodo,
            NumeroGrupo = 1,
            CupoMaximo = 25,
            Estado = EstadoGrupo.Abierto
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Grupo grupo)
    {
        ViewData["Titulo"] = "Nuevo grupo";

        await ValidarGrupoAsync(grupo, null);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(grupo);
        }

        contexto.Grupos.Add(grupo);
        bitacora.Registrar("Crear grupo", nameof(Grupo), null,
            $"Curso {grupo.CursoId}, grupo {grupo.NumeroGrupo}, periodo {grupo.PeriodoAcademicoId}.");
        await contexto.SaveChangesAsync();

        TempData["Exito"] = "El grupo fue creado y ya aparece en la oferta del periodo.";
        return RedirectToAction(nameof(Index), new { periodoId = grupo.PeriodoAcademicoId });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var grupo = await contexto.Grupos.FindAsync(id);

        if (grupo is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = "Editar grupo";
        await CargarListasAsync();
        return View(grupo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Grupo grupo)
    {
        if (id != grupo.Id)
        {
            return NotFound();
        }

        ViewData["Titulo"] = "Editar grupo";

        await ValidarGrupoAsync(grupo, id);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(grupo);
        }

        var original = await contexto.Grupos.FirstOrDefaultAsync(g => g.Id == id);

        if (original is null)
        {
            return NotFound();
        }

        var inscritos = await contexto.DetallesMatricula
            .CountAsync(d => d.GrupoId == id
                          && d.Estado == EstadoDetalleMatricula.Activo
                          && d.Matricula!.Estado != EstadoMatricula.Anulada);

        if (grupo.CupoMaximo < inscritos)
        {
            ModelState.AddModelError(nameof(grupo.CupoMaximo),
                $"El grupo ya tiene {inscritos} personas matriculadas; el cupo no puede ser menor.");
            await CargarListasAsync();
            return View(grupo);
        }

        original.CursoId = grupo.CursoId;
        original.DocenteId = grupo.DocenteId;
        original.PeriodoAcademicoId = grupo.PeriodoAcademicoId;
        original.NumeroGrupo = grupo.NumeroGrupo;
        original.Horario = grupo.Horario;
        original.Aula = grupo.Aula;
        original.CupoMaximo = grupo.CupoMaximo;
        original.Estado = grupo.Estado;

        bitacora.Registrar("Editar grupo", nameof(Grupo), id.ToString(),
            $"Docente {grupo.DocenteId}, cupo {grupo.CupoMaximo}.");

        await contexto.SaveChangesAsync();

        TempData["Exito"] = "Los cambios del grupo fueron guardados.";
        return RedirectToAction(nameof(Index), new { periodoId = grupo.PeriodoAcademicoId });
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var grupo = await contexto.Grupos
            .AsNoTracking()
            .Include(g => g.Curso)
            .Include(g => g.Docente)
            .Include(g => g.PeriodoAcademico)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grupo is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = "Eliminar grupo";
        ViewBag.Inscritos = await contexto.DetallesMatricula.CountAsync(d => d.GrupoId == id);

        return View(grupo);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(int id)
    {
        var grupo = await contexto.Grupos.FindAsync(id);

        if (grupo is null)
        {
            return NotFound();
        }

        if (await contexto.DetallesMatricula.AnyAsync(d => d.GrupoId == id))
        {
            grupo.Estado = EstadoGrupo.Cancelado;
            bitacora.Registrar("Cancelar grupo", nameof(Grupo), id.ToString(),
                "Tiene matrículas asociadas, se canceló en lugar de eliminarse.");
            await contexto.SaveChangesAsync();

            TempData["Aviso"] = "El grupo tiene personas matriculadas, así que se canceló " +
                                "en lugar de eliminarse.";
            return RedirectToAction(nameof(Index));
        }

        contexto.Grupos.Remove(grupo);
        bitacora.Registrar("Eliminar grupo", nameof(Grupo), id.ToString());
        await contexto.SaveChangesAsync();

        TempData["Exito"] = "El grupo fue eliminado.";
        return RedirectToAction(nameof(Index));
    }

    // =================================================================================
    //  Apoyos privados
    // =================================================================================

    private async Task ValidarGrupoAsync(Grupo grupo, int? idExcluido)
    {
        var repetido = await contexto.Grupos.AnyAsync(g => g.CursoId == grupo.CursoId
                                                        && g.PeriodoAcademicoId == grupo.PeriodoAcademicoId
                                                        && g.NumeroGrupo == grupo.NumeroGrupo
                                                        && g.Id != idExcluido);

        if (repetido)
        {
            ModelState.AddModelError(nameof(grupo.NumeroGrupo),
                "Ese curso ya tiene un grupo con ese número en el periodo indicado.");
        }

        if (grupo.DocenteId is not null)
        {
            var choque = await contexto.Grupos
                .Include(g => g.Curso)
                .FirstOrDefaultAsync(g => g.DocenteId == grupo.DocenteId
                                       && g.PeriodoAcademicoId == grupo.PeriodoAcademicoId
                                       && g.Horario == grupo.Horario
                                       && g.Estado != EstadoGrupo.Cancelado
                                       && g.Id != idExcluido);

            if (choque is not null)
            {
                ModelState.AddModelError(nameof(grupo.Horario),
                    $"Esa persona docente ya tiene {choque.Curso?.Codigo} en el mismo horario.");
            }
        }
    }

    private async Task CargarListasAsync()
    {
        ViewData["Cursos"] = await contexto.Cursos
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Codigo)
            .Select(c => new SelectListItem($"{c.Codigo} — {c.Nombre}", c.Id.ToString()))
            .ToListAsync();

        ViewData["Docentes"] = await contexto.Docentes
            .AsNoTracking()
            .Where(d => d.Activo)
            .OrderBy(d => d.Apellidos)
            .Select(d => new SelectListItem($"{d.Apellidos}, {d.Nombre} — {d.Especialidad}", d.Id.ToString()))
            .ToListAsync();

        ViewData["Periodos"] = await contexto.PeriodosAcademicos
            .AsNoTracking()
            .OrderByDescending(p => p.FechaInicio)
            .Select(p => new SelectListItem(p.Nombre, p.Id.ToString()))
            .ToListAsync();
    }
}
