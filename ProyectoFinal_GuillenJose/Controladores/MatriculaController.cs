using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
/// Matrícula de la persona estudiante. Corresponde a la pantalla 5 del prototipo, ampliada con
/// el concepto de borrador: la persona arma su matrícula agregando y quitando grupos, y solo
/// cuando confirma se emite el comprobante y se sella la transacción.
/// </summary>
[Authorize(Policy = Politicas.SoloEstudiantado)]
public class MatriculaController(
    ServicioMatricula servicioMatricula,
    ContextoMatricula contexto,
    UserManager<Usuario> gestorUsuarios,
    IOptions<OpcionesMatricula> opciones) : Controller
{
    private readonly OpcionesMatricula _opciones = opciones.Value;

    [HttpGet]
    public async Task<IActionResult> MisCursos()
    {
        ViewData["Titulo"] = "Mis cursos";

        var estudianteId = gestorUsuarios.GetUserId(User)!;
        var periodo = await servicioMatricula.ObtenerPeriodoVigenteAsync();

        if (periodo is null)
        {
            return View(new ModeloMisCursos { CreditosMinimos = _opciones.CreditosMinimos });
        }

        var matricula = await contexto.Matriculas
            .AsNoTracking()
            .Include(m => m.PeriodoAcademico)
            .Include(m => m.Detalles).ThenInclude(d => d.Grupo!).ThenInclude(g => g.Curso)
            .Include(m => m.Detalles).ThenInclude(d => d.Grupo!).ThenInclude(g => g.Docente)
            .FirstOrDefaultAsync(m => m.EstudianteId == estudianteId && m.PeriodoAcademicoId == periodo.Id);

        var lineas = matricula?.Detalles
            .Where(d => d.Estado == EstadoDetalleMatricula.Activo)
            .OrderBy(d => d.Grupo!.Curso!.Codigo)
            .ToList() ?? [];

        var creditos = lineas.Sum(l => l.Grupo?.Curso?.Creditos ?? 0);

        return View(new ModeloMisCursos
        {
            Matricula = matricula,
            Periodo = periodo,
            Lineas = lineas,
            TotalCreditos = creditos,
            MontoEstimado = (creditos * _opciones.CostoPorCredito) + _opciones.CargoAdministrativo,
            CreditosMinimos = _opciones.CreditosMinimos,
            VentanaAbierta = periodo.AceptaMatricula(DateTime.Now),
            Historial = await contexto.Matriculas
                .AsNoTracking()
                .Include(m => m.PeriodoAcademico)
                .Where(m => m.EstudianteId == estudianteId && m.PeriodoAcademicoId != periodo.Id)
                .OrderByDescending(m => m.PeriodoAcademico!.FechaInicio)
                .ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(int grupoId, string? retorno)
    {
        var estudianteId = gestorUsuarios.GetUserId(User)!;
        var resultado = await servicioMatricula.AgregarGrupoAsync(estudianteId, grupoId);

        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;

        return RedirigirA(retorno);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar(int detalleId, string? retorno)
    {
        var estudianteId = gestorUsuarios.GetUserId(User)!;
        var resultado = await servicioMatricula.QuitarGrupoAsync(estudianteId, detalleId);

        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;

        return RedirigirA(retorno);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar()
    {
        var estudianteId = gestorUsuarios.GetUserId(User)!;
        var periodo = await servicioMatricula.ObtenerPeriodoVigenteAsync();

        if (periodo is null)
        {
            TempData["Error"] = "No hay un periodo académico configurado.";
            return RedirectToAction(nameof(MisCursos));
        }

        var resultado = await servicioMatricula.ConfirmarAsync(estudianteId, periodo.Id);

        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;

        return RedirectToAction(nameof(MisCursos));
    }

    /// <summary>
    /// Entrega el comprobante en PDF de una matrícula confirmada. La verificación de propiedad
    /// se hace aquí además de en el controlador de documentos, porque el número de matrícula es
    /// más fácil de adivinar que el de documento.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Comprobante(int id)
    {
        var estudianteId = gestorUsuarios.GetUserId(User)!;

        var matricula = await contexto.Matriculas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.EstudianteId == estudianteId);

        if (matricula?.ComprobanteDocumentoId is null)
        {
            TempData["Error"] = "Esa matrícula todavía no tiene comprobante generado.";
            return RedirectToAction(nameof(MisCursos));
        }

        return RedirectToAction("Descargar", "Documentos", new { id = matricula.ComprobanteDocumentoId });
    }

    private IActionResult RedirigirA(string? retorno) =>
        !string.IsNullOrWhiteSpace(retorno) && Url.IsLocalUrl(retorno)
            ? Redirect(retorno)
            : RedirectToAction(nameof(MisCursos));
}
