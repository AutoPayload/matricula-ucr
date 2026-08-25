using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Controladores;

/// <summary>
/// Mantenimiento del calendario académico. Además del alta y la edición, aquí se abre y se
/// cierra la ventana de matrícula, que es la palanca que gobierna todo el flujo del
/// estudiantado: sin un periodo en estado de matrícula abierta nadie puede confirmar.
/// </summary>
[Authorize(Policy = Politicas.SoloAdministracion)]
public class PeriodosController(
    ContextoMatricula contexto,
    ServicioBitacora bitacora,
    ServicioNotificaciones notificaciones,
    IOptions<OpcionesMatricula> opciones) : Controller
{
    private readonly int _tamanoPagina = opciones.Value.TamanoPagina;

    [HttpGet]
    public async Task<IActionResult> Index(EstadoPeriodo? estado, int pagina = 1)
    {
        ViewData["Titulo"] = "Periodos académicos";

        var consulta = contexto.PeriodosAcademicos.AsNoTracking().AsQueryable();

        if (estado is { } valorEstado)
        {
            consulta = consulta.Where(p => p.Estado == valorEstado);
        }

        ViewBag.Estado = estado;

        return View(await consulta
            .OrderByDescending(p => p.FechaInicio)
            .Select(p => new FilaPeriodo
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin,
                InicioMatricula = p.InicioMatricula,
                FinMatricula = p.FinMatricula,
                Estado = p.Estado,
                MaximoCreditos = p.MaximoCreditos,
                CantidadGrupos = p.Grupos.Count,
                MatriculasConfirmadas = p.Matriculas.Count(m => m.Estado == EstadoMatricula.Confirmada)
            })
            .PaginarAsync(pagina, _tamanoPagina));
    }

    [HttpGet]
    public IActionResult Crear()
    {
        ViewData["Titulo"] = "Nuevo periodo";

        var hoy = DateTime.Today;

        return View(new PeriodoAcademico
        {
            InicioMatricula = hoy,
            FinMatricula = hoy.AddDays(21),
            FechaInicio = hoy.AddDays(28),
            FechaFin = hoy.AddDays(126),
            MaximoCreditos = 18,
            Estado = EstadoPeriodo.Planificado
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(PeriodoAcademico periodo)
    {
        ViewData["Titulo"] = "Nuevo periodo";

        if (await contexto.PeriodosAcademicos.AnyAsync(p => p.Codigo == periodo.Codigo))
        {
            ModelState.AddModelError(nameof(periodo.Codigo), "Ya existe un periodo con ese código.");
        }

        if (!ModelState.IsValid)
        {
            return View(periodo);
        }

        contexto.PeriodosAcademicos.Add(periodo);
        bitacora.Registrar("Crear periodo", nameof(PeriodoAcademico), null, periodo.Codigo);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = $"El periodo {periodo.Nombre} fue creado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var periodo = await contexto.PeriodosAcademicos.FindAsync(id);

        if (periodo is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Editar {periodo.Nombre}";
        return View(periodo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, PeriodoAcademico periodo)
    {
        if (id != periodo.Id)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Editar {periodo.Nombre}";

        if (await contexto.PeriodosAcademicos.AnyAsync(p => p.Codigo == periodo.Codigo && p.Id != id))
        {
            ModelState.AddModelError(nameof(periodo.Codigo), "Ya existe otro periodo con ese código.");
        }

        if (!ModelState.IsValid)
        {
            return View(periodo);
        }

        var original = await contexto.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == id);

        if (original is null)
        {
            return NotFound();
        }

        original.Codigo = periodo.Codigo;
        original.Nombre = periodo.Nombre;
        original.FechaInicio = periodo.FechaInicio;
        original.FechaFin = periodo.FechaFin;
        original.InicioMatricula = periodo.InicioMatricula;
        original.FinMatricula = periodo.FinMatricula;
        original.MaximoCreditos = periodo.MaximoCreditos;

        bitacora.Registrar("Editar periodo", nameof(PeriodoAcademico), id.ToString(), periodo.Codigo);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = "Los cambios del periodo fueron guardados.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Cambia el estado del periodo. Solo puede haber un periodo con la matrícula abierta a la
    /// vez: abrir uno cierra automáticamente el que estuviera abierto, porque de lo contrario
    /// el sistema no sabría cuál ofrecer al estudiantado.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, EstadoPeriodo estado)
    {
        var periodo = await contexto.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == id);

        if (periodo is null)
        {
            return NotFound();
        }

        if (estado == EstadoPeriodo.MatriculaAbierta)
        {
            var otros = await contexto.PeriodosAcademicos
                .Where(p => p.Id != id && p.Estado == EstadoPeriodo.MatriculaAbierta)
                .ToListAsync();

            foreach (var otro in otros)
            {
                otro.Estado = EstadoPeriodo.EnCurso;
            }

            // Si la ventana de fechas ya venció, se corre para que la apertura tenga efecto real.
            if (periodo.FinMatricula.Date < DateTime.Today)
            {
                periodo.InicioMatricula = DateTime.Today;
                periodo.FinMatricula = DateTime.Today.AddDays(14);
            }

            var estudiantes = await contexto.Users
                .Where(u => u.Activo && u.CarreraId != null)
                .Select(u => u.Id)
                .ToListAsync();

            notificaciones.EmitirEnLote(estudiantes, "Matrícula abierta",
                $"Ya puede matricular el periodo {periodo.Nombre}. " +
                $"La ventana cierra el {periodo.FinMatricula:dd 'de' MMMM}.",
                "/Cursos/Disponibles");
        }

        var anterior = periodo.Estado;
        periodo.Estado = estado;

        bitacora.Registrar("Cambiar estado del periodo", nameof(PeriodoAcademico), id.ToString(),
            $"De {anterior} a {estado}.");

        await contexto.SaveChangesAsync();

        TempData["Exito"] = estado switch
        {
            EstadoPeriodo.MatriculaAbierta => $"La matrícula de {periodo.Nombre} quedó abierta y se " +
                                              "notificó al estudiantado.",
            EstadoPeriodo.Cerrado => $"El periodo {periodo.Nombre} fue cerrado.",
            _ => $"El periodo {periodo.Nombre} pasó al estado indicado."
        };

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var periodo = await contexto.PeriodosAcademicos
            .AsNoTracking()
            .Include(p => p.Grupos)
            .Include(p => p.Matriculas)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (periodo is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Eliminar {periodo.Nombre}";
        return View(periodo);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(int id)
    {
        var periodo = await contexto.PeriodosAcademicos.FindAsync(id);

        if (periodo is null)
        {
            return NotFound();
        }

        if (await contexto.Matriculas.AnyAsync(m => m.PeriodoAcademicoId == id)
            || await contexto.Grupos.AnyAsync(g => g.PeriodoAcademicoId == id))
        {
            TempData["Error"] = "El periodo tiene grupos o matrículas asociadas y no puede eliminarse. " +
                                "Ciérrelo en lugar de borrarlo.";
            return RedirectToAction(nameof(Index));
        }

        contexto.PeriodosAcademicos.Remove(periodo);
        bitacora.Registrar("Eliminar periodo", nameof(PeriodoAcademico), id.ToString(), periodo.Codigo);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = $"El periodo {periodo.Nombre} fue eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
