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
/// Panel de la oficina de registro: indicadores del periodo, listado de matrículas y bitácora
/// de auditoría. Los gráficos se dibujan con SVG generado por el propio sistema y se refrescan
/// sin recargar la página consultando la API interna.
/// </summary>
[Authorize(Policy = Politicas.SoloAdministracion)]
public class PanelController(
    ContextoMatricula contexto,
    ServicioEstadisticas estadisticas,
    ServicioMatricula servicioMatricula,
    IOptions<OpcionesMatricula> opciones) : Controller
{
    private readonly int _tamanoPagina = opciones.Value.TamanoPagina;

    [HttpGet]
    public async Task<IActionResult> Index(int? periodoId)
    {
        ViewData["Titulo"] = "Panel de registro";

        var periodo = periodoId is null
            ? await servicioMatricula.ObtenerPeriodoVigenteAsync()
            : await contexto.PeriodosAcademicos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodoId);

        if (periodo is null)
        {
            return View(new TableroAdministrativo { PeriodoNombre = "Sin periodo configurado" });
        }

        ViewBag.Periodos = await contexto.PeriodosAcademicos
            .AsNoTracking()
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync();

        return View(await estadisticas.ObtenerTableroAsync(periodo.Id));
    }

    /// <summary>Listado de matrículas del periodo, con filtro por estado y por texto libre.</summary>
    [HttpGet]
    public async Task<IActionResult> Matriculas(int? periodoId, EstadoMatricula? estado,
                                                string? texto, int pagina = 1)
    {
        ViewData["Titulo"] = "Matrículas";

        var consulta = contexto.Matriculas.AsNoTracking().AsQueryable();

        if (periodoId is { } valorPeriodo)
        {
            consulta = consulta.Where(m => m.PeriodoAcademicoId == valorPeriodo);
        }

        if (estado is { } valorEstado)
        {
            consulta = consulta.Where(m => m.Estado == valorEstado);
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var termino = texto.Trim();
            consulta = consulta.Where(m => m.Estudiante!.Nombre.Contains(termino)
                                        || m.Estudiante.Apellidos.Contains(termino)
                                        || m.Estudiante.Identificacion.Contains(termino)
                                        || (m.NumeroComprobante != null && m.NumeroComprobante.Contains(termino)));
        }

        ViewBag.Periodos = await contexto.PeriodosAcademicos
            .AsNoTracking()
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync();

        ViewBag.PeriodoId = periodoId;
        ViewBag.EstadoFiltro = estado;
        ViewBag.Texto = texto;

        return View(await consulta
            .OrderByDescending(m => m.FechaCreacion)
            .Select(m => new FilaMatricula
            {
                Id = m.Id,
                NumeroComprobante = m.NumeroComprobante,
                Estudiante = m.Estudiante!.Nombre + " " + m.Estudiante.Apellidos,
                Identificacion = m.Estudiante.Identificacion,
                Carrera = m.Estudiante.Carrera == null ? "Sin carrera" : m.Estudiante.Carrera.Nombre,
                Periodo = m.PeriodoAcademico!.Codigo,
                Estado = m.Estado,
                TotalCreditos = m.TotalCreditos,
                MontoTotal = m.MontoTotal,
                FechaConfirmacion = m.FechaConfirmacion,
                CantidadCursos = m.Detalles.Count(d => d.Estado == EstadoDetalleMatricula.Activo)
            })
            .PaginarAsync(pagina, _tamanoPagina));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnularMatricula(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["Error"] = "Indique el motivo de la anulación.";
            return RedirectToAction(nameof(Matriculas));
        }

        var resultado = await servicioMatricula.AnularAsync(id, motivo.Trim());

        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Matriculas));
    }

    /// <summary>Bitácora de auditoría con filtro por acción, por entidad y por rango de fechas.</summary>
    [HttpGet]
    public async Task<IActionResult> Bitacora(string? accion, string? entidad,
                                              DateTime? desde, DateTime? hasta, int pagina = 1)
    {
        ViewData["Titulo"] = "Bitácora";

        var consulta = contexto.Bitacoras.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(accion))
        {
            consulta = consulta.Where(b => b.Accion.Contains(accion));
        }

        if (!string.IsNullOrWhiteSpace(entidad))
        {
            consulta = consulta.Where(b => b.Entidad == entidad);
        }

        if (desde is { } fechaDesde)
        {
            consulta = consulta.Where(b => b.FechaHora >= fechaDesde.Date);
        }

        if (hasta is { } fechaHasta)
        {
            consulta = consulta.Where(b => b.FechaHora < fechaHasta.Date.AddDays(1));
        }

        ViewBag.Accion = accion;
        ViewBag.Entidad = entidad;
        ViewBag.Desde = desde;
        ViewBag.Hasta = hasta;

        ViewBag.Entidades = await contexto.Bitacoras
            .AsNoTracking()
            .Select(b => b.Entidad)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        return View(await consulta
            .OrderByDescending(b => b.FechaHora)
            .PaginarAsync(pagina, _tamanoPagina * 3));
    }
}
