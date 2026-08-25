using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Componentes;

/// <summary>
/// Componente propio número uno: la campana de avisos de la barra superior. Muestra el conteo
/// de mensajes sin leer y los cinco más recientes. Se resuelve en el servidor en cada carga
/// para que el número esté siempre al día sin depender de JavaScript.
/// </summary>
public class NotificacionesPendientesViewComponent(ServicioNotificaciones notificaciones) : ViewComponent
{
    private const string Vista = "~/Vistas/Compartidas/Componentes/NotificacionesPendientes.cshtml";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var usuarioId = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(usuarioId))
        {
            return View(Vista, new ResumenAvisos());
        }

        return View(Vista, new ResumenAvisos
        {
            Pendientes = await notificaciones.ContarPendientesAsync(usuarioId),
            Recientes = await notificaciones.ObtenerRecientesAsync(usuarioId)
        });
    }

    /// <summary>Datos que consume la vista del componente.</summary>
    public class ResumenAvisos
    {
        public int Pendientes { get; init; }
        public List<Notificacion> Recientes { get; init; } = [];
    }
}
