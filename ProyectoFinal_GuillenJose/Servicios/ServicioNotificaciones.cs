using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Emite y consulta los avisos que el sistema dirige a las personas usuarias. Los avisos se
/// encolan dentro de la misma transacción del hecho que los origina, así que una matrícula que
/// se deshace tampoco deja el aviso suelto.
/// </summary>
public class ServicioNotificaciones(ContextoMatricula contexto)
{
    /// <summary>Encola un aviso sin confirmar los cambios.</summary>
    public void Emitir(string usuarioId, string titulo, string mensaje, string? enlace = null)
    {
        contexto.Notificaciones.Add(new Notificacion
        {
            UsuarioId = usuarioId,
            Titulo = titulo,
            Mensaje = mensaje,
            Enlace = enlace,
            FechaCreacion = DateTime.Now
        });
    }

    /// <summary>Encola el mismo aviso para varias personas, por ejemplo al abrir la matrícula.</summary>
    public void EmitirEnLote(IEnumerable<string> usuarioIds, string titulo, string mensaje, string? enlace = null)
    {
        foreach (var usuarioId in usuarioIds)
        {
            Emitir(usuarioId, titulo, mensaje, enlace);
        }
    }

    public async Task<List<Notificacion>> ObtenerRecientesAsync(string usuarioId, int cantidad = 5) =>
        await contexto.Notificaciones
            .AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaCreacion)
            .Take(cantidad)
            .ToListAsync();

    public async Task<int> ContarPendientesAsync(string usuarioId) =>
        await contexto.Notificaciones.CountAsync(n => n.UsuarioId == usuarioId && !n.Leida);

    /// <summary>Marca como leídos todos los avisos de la persona indicada.</summary>
    public async Task<int> MarcarTodasLeidasAsync(string usuarioId) =>
        await contexto.Notificaciones
            .Where(n => n.UsuarioId == usuarioId && !n.Leida)
            .ExecuteUpdateAsync(fila => fila.SetProperty(n => n.Leida, true));
}
