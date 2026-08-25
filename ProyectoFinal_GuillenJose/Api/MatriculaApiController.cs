using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Api;

/// <summary>
/// Operaciones de matrícula que el navegador ejecuta sin recargar la página. Está protegida por
/// token JWT y no por la cookie de sesión, de manera que la misma API sirve para una aplicación
/// móvil futura sin cambiar una línea.
/// </summary>
[ApiController]
[Route("api/matricula")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
           Roles = RolesSistema.Estudiante)]
public class MatriculaApiController(
    ServicioMatricula servicioMatricula,
    ContextoMatricula contexto,
    IOptions<OpcionesMatricula> opciones) : ControllerBase
{
    private readonly OpcionesMatricula _opciones = opciones.Value;

    /// <summary>Agrega un grupo a la matrícula en proceso.</summary>
    [HttpPost("detalle")]
    public async Task<IActionResult> Agregar([FromBody] SolicitudGrupo solicitud)
    {
        var estudianteId = ObtenerUsuarioId();
        var resultado = await servicioMatricula.AgregarGrupoAsync(estudianteId, solicitud.GrupoId);

        return resultado.Exitoso
            ? Ok(await ArmarRespuestaAsync(estudianteId, resultado.Mensaje, true))
            : Conflict(await ArmarRespuestaAsync(estudianteId, resultado.Mensaje, false));
    }

    /// <summary>Quita una línea de la matrícula en proceso.</summary>
    [HttpDelete("detalle/{detalleId:int}")]
    public async Task<IActionResult> Quitar(int detalleId)
    {
        var estudianteId = ObtenerUsuarioId();
        var resultado = await servicioMatricula.QuitarGrupoAsync(estudianteId, detalleId);

        return resultado.Exitoso
            ? Ok(await ArmarRespuestaAsync(estudianteId, resultado.Mensaje, true))
            : Conflict(await ArmarRespuestaAsync(estudianteId, resultado.Mensaje, false));
    }

    /// <summary>Resumen de la matrícula en proceso: créditos, cursos y monto estimado.</summary>
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        var estudianteId = ObtenerUsuarioId();
        return Ok(await ArmarRespuestaAsync(estudianteId, "Resumen actualizado.", true));
    }

    /// <summary>Cupo disponible de un grupo, para revalidar antes de confirmar.</summary>
    [HttpGet("grupo/{grupoId:int}/cupo")]
    public async Task<IActionResult> Cupo(int grupoId)
    {
        var grupo = await contexto.Grupos
            .AsNoTracking()
            .Include(g => g.Curso)
            .FirstOrDefaultAsync(g => g.Id == grupoId);

        if (grupo is null)
        {
            return NotFound(new { titulo = "Grupo no encontrado", estado = 404 });
        }

        var inscritos = await servicioMatricula.ContarInscritosAsync(grupoId);

        return Ok(new
        {
            grupoId,
            curso = grupo.Curso?.Codigo,
            cupoMaximo = grupo.CupoMaximo,
            inscritos,
            disponibles = Math.Max(0, grupo.CupoMaximo - inscritos),
            hayCupo = inscritos < grupo.CupoMaximo
        });
    }

    private string ObtenerUsuarioId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("El token no trae el identificador de la persona usuaria.");

    /// <summary>
    /// Respuesta uniforme: además del mensaje, siempre viaja el estado actualizado de la
    /// matrícula, así el cliente refresca el panel lateral con una sola petición.
    /// </summary>
    private async Task<object> ArmarRespuestaAsync(string estudianteId, string mensaje, bool exitoso)
    {
        var periodo = await servicioMatricula.ObtenerPeriodoVigenteAsync();

        if (periodo is null)
        {
            return new { exitoso = false, mensaje = "No hay un periodo configurado.", creditos = 0 };
        }

        var lineas = await contexto.DetallesMatricula
            .AsNoTracking()
            .Include(d => d.Grupo!).ThenInclude(g => g.Curso)
            .Where(d => d.Matricula!.EstudianteId == estudianteId
                     && d.Matricula.PeriodoAcademicoId == periodo.Id
                     && d.Estado == EstadoDetalleMatricula.Activo)
            .OrderBy(d => d.Grupo!.Curso!.Codigo)
            .ToListAsync();

        var creditos = lineas.Sum(l => l.Grupo?.Curso?.Creditos ?? 0);

        return new
        {
            exitoso,
            mensaje,
            periodo = periodo.Nombre,
            creditos,
            topeCreditos = periodo.MaximoCreditos,
            creditosMinimos = _opciones.CreditosMinimos,
            cursos = lineas.Count,
            montoEstimado = (creditos * _opciones.CostoPorCredito) + _opciones.CargoAdministrativo,
            puedeConfirmar = creditos >= _opciones.CreditosMinimos && periodo.AceptaMatricula(DateTime.Now),
            detalle = lineas.Select(l => new
            {
                detalleId = l.Id,
                grupoId = l.GrupoId,
                codigo = l.Grupo?.Curso?.Codigo,
                nombre = l.Grupo?.Curso?.Nombre,
                creditos = l.Grupo?.Curso?.Creditos ?? 0,
                horario = l.Grupo?.Horario
            })
        };
    }
}

/// <summary>Cuerpo de la petición para agregar un grupo.</summary>
public class SolicitudGrupo
{
    public int GrupoId { get; set; }
}
