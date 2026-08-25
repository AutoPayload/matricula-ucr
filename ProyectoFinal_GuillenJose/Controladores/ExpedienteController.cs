using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;

namespace ProyectoFinal_GuillenJose.Controladores;

/// <summary>
/// Expediente académico de la persona estudiante: el recorrido completo de lo cursado, con
/// notas, promedios por periodo y acceso a los comprobantes emitidos.
/// </summary>
[Authorize(Policy = Politicas.SoloEstudiantado)]
public class ExpedienteController(ContextoMatricula contexto, UserManager<Usuario> gestorUsuarios) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Titulo"] = "Expediente académico";

        var estudianteId = gestorUsuarios.GetUserId(User)!;

        var estudiante = await contexto.Users
            .AsNoTracking()
            .Include(u => u.Carrera)
            .FirstAsync(u => u.Id == estudianteId);

        var matriculas = await contexto.Matriculas
            .AsNoTracking()
            .Include(m => m.PeriodoAcademico)
            .Include(m => m.Detalles).ThenInclude(d => d.Grupo!).ThenInclude(g => g.Curso)
            .Include(m => m.Detalles).ThenInclude(d => d.Grupo!).ThenInclude(g => g.Docente)
            .Where(m => m.EstudianteId == estudianteId && m.Estado != EstadoMatricula.Anulada)
            .OrderByDescending(m => m.PeriodoAcademico!.FechaInicio)
            .ToListAsync();

        return View(new ModeloExpediente
        {
            EstudianteId = estudianteId,
            NombreEstudiante = estudiante.NombreCompleto,
            Identificacion = estudiante.Identificacion,
            NombreCarrera = estudiante.Carrera?.Nombre ?? "Sin carrera asignada",
            Periodos = [.. matriculas.Select(m => new BloquePeriodo
            {
                NombrePeriodo = m.PeriodoAcademico?.Nombre ?? "Periodo",
                CodigoPeriodo = m.PeriodoAcademico?.Codigo ?? string.Empty,
                Estado = m.Estado,
                NumeroComprobante = m.NumeroComprobante,
                ComprobanteDocumentoId = m.ComprobanteDocumentoId,
                Lineas = [.. m.Detalles
                    .Where(d => d.Estado == EstadoDetalleMatricula.Activo)
                    .OrderBy(d => d.Grupo!.Curso!.Codigo)]
            })]
        });
    }
}
