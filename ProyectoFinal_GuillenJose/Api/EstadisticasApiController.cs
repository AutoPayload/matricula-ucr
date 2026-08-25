using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Api;

/// <summary>
/// Indicadores que alimentan el panel de la oficina de registro. El panel los pide cada vez que
/// se cambia de periodo o se pulsa actualizar, sin recargar la página.
/// </summary>
[ApiController]
[Route("api/estadisticas")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
           Roles = RolesSistema.Administrador)]
public class EstadisticasApiController(
    ServicioEstadisticas estadisticas,
    ServicioMatricula servicioMatricula) : ControllerBase
{
    [HttpGet("panel")]
    public async Task<IActionResult> Panel(int? periodoId)
    {
        var periodo = periodoId ?? (await servicioMatricula.ObtenerPeriodoVigenteAsync())?.Id;

        if (periodo is null)
        {
            return NotFound(new { titulo = "Sin periodo configurado", estado = 404 });
        }

        var tablero = await estadisticas.ObtenerTableroAsync(periodo.Value);

        return Ok(new
        {
            periodo = tablero.PeriodoNombre,
            estado = tablero.PeriodoEstado,
            indicadores = new
            {
                tablero.MatriculasConfirmadas,
                tablero.MatriculasEnProceso,
                tablero.CreditosTotales,
                tablero.IngresoProyectado,
                tablero.PromedioCreditos,
                tablero.EstudiantesActivos,
                tablero.GruposAbiertos
            },
            matriculaPorCarrera = tablero.MatriculaPorCarrera
                .Select(s => new { s.Etiqueta, s.Valor }),
            ocupacion = tablero.OcupacionPorGrupo
                .Select(o => new
                {
                    o.Etiqueta,
                    o.NombreCurso,
                    o.Inscritos,
                    o.CupoMaximo,
                    o.Disponibles,
                    o.PorcentajeOcupacion
                })
        });
    }
}
