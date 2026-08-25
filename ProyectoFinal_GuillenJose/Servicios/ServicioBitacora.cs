using System.Security.Claims;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Escribe la bitácora de auditoría. Toma la identidad y la dirección de origen del contexto de
/// la petición en curso, de modo que quien registra un movimiento no tenga que acordarse de
/// pasar esos datos en cada llamada.
/// </summary>
public class ServicioBitacora(ContextoMatricula contexto, IHttpContextAccessor accesor)
{
    /// <summary>
    /// Anota una acción. No guarda los cambios: se une a la transacción que esté abierta para
    /// que la auditoría y el movimiento auditado se confirmen o se deshagan juntos.
    /// </summary>
    public void Registrar(string accion, string entidad, string? entidadId = null, string? detalle = null)
    {
        var contextoHttp = accesor.HttpContext;
        var identidad = contextoHttp?.User;

        var anotacion = new Bitacora
        {
            FechaHora = DateTime.Now,
            UsuarioId = identidad?.FindFirstValue(ClaimTypes.NameIdentifier),
            NombreUsuario = identidad?.Identity?.IsAuthenticated == true
                ? identidad.Identity.Name ?? "Sin nombre"
                : "Anónimo",
            Rol = identidad?.FindFirstValue(ClaimTypes.Role) ?? "Sin rol",
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle,
            DireccionIp = contextoHttp?.Connection.RemoteIpAddress?.ToString()
        };

        contexto.Bitacoras.Add(anotacion);
    }

    /// <summary>Anota una acción y la confirma de inmediato, para operaciones sueltas.</summary>
    public async Task RegistrarYGuardarAsync(
        string accion, string entidad, string? entidadId = null, string? detalle = null)
    {
        Registrar(accion, entidad, entidadId, detalle);
        await contexto.SaveChangesAsync();
    }
}
