using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Api;

/// <summary>
/// Servicios de solo lectura para sistemas externos, como la plataforma de aula virtual o la
/// pasarela de pagos de la universidad. Se autentican con clave de aplicación en el encabezado,
/// que es el tercer mecanismo de autenticación del sistema.
///
/// Nunca expone datos personales completos: devuelve la oferta académica y cifras agregadas.
/// </summary>
[ApiController]
[Route("api/integracion")]
[AllowAnonymous]
[FiltroClaveApi]
public class IntegracionApiController(ContextoMatricula contexto) : ControllerBase
{
    /// <summary>Oferta de grupos de un periodo, para publicarla en el aula virtual.</summary>
    [HttpGet("oferta/{codigoPeriodo}")]
    public async Task<IActionResult> Oferta(string codigoPeriodo)
    {
        var periodo = await contexto.PeriodosAcademicos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Codigo == codigoPeriodo);

        if (periodo is null)
        {
            return NotFound(new { titulo = "Periodo no encontrado", estado = 404, codigoPeriodo });
        }

        var grupos = await contexto.Grupos
            .AsNoTracking()
            .Include(g => g.Curso)
            .Include(g => g.Docente)
            .Where(g => g.PeriodoAcademicoId == periodo.Id && g.Estado != EstadoGrupo.Cancelado)
            .OrderBy(g => g.Curso!.Codigo)
            .ThenBy(g => g.NumeroGrupo)
            .Select(g => new
            {
                grupoId = g.Id,
                curso = g.Curso!.Codigo,
                nombre = g.Curso.Nombre,
                creditos = g.Curso.Creditos,
                modalidad = g.Curso.Modalidad.ToString(),
                numeroGrupo = g.NumeroGrupo,
                horario = g.Horario,
                aula = g.Aula,
                docente = g.Docente == null ? null : g.Docente.Nombre + " " + g.Docente.Apellidos,
                cupoMaximo = g.CupoMaximo,
                inscritos = g.Detalles.Count(d => d.Estado == EstadoDetalleMatricula.Activo
                                               && d.Matricula!.Estado == EstadoMatricula.Confirmada)
            })
            .ToListAsync();

        return Ok(new
        {
            periodo = new { periodo.Codigo, periodo.Nombre, estado = periodo.Estado.ToString() },
            total = grupos.Count,
            grupos
        });
    }

    /// <summary>Cifras agregadas del periodo, para el tablero institucional.</summary>
    [HttpGet("resumen/{codigoPeriodo}")]
    public async Task<IActionResult> Resumen(string codigoPeriodo)
    {
        var periodo = await contexto.PeriodosAcademicos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Codigo == codigoPeriodo);

        if (periodo is null)
        {
            return NotFound(new { titulo = "Periodo no encontrado", estado = 404, codigoPeriodo });
        }

        var confirmadas = await contexto.Matriculas
            .Where(m => m.PeriodoAcademicoId == periodo.Id && m.Estado == EstadoMatricula.Confirmada)
            .ToListAsync();

        return Ok(new
        {
            periodo = periodo.Codigo,
            matriculasConfirmadas = confirmadas.Count,
            creditosTotales = confirmadas.Sum(m => m.TotalCreditos),
            montoTotal = confirmadas.Sum(m => m.MontoTotal),
            gruposAbiertos = await contexto.Grupos
                .CountAsync(g => g.PeriodoAcademicoId == periodo.Id && g.Estado == EstadoGrupo.Abierto),
            generadoEn = DateTime.Now
        });
    }
}
