using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Controladores;

/// <summary>
/// Única puerta de salida del almacén de archivos. Ningún documento se sirve como contenido
/// estático: toda descarga entra por aquí y solo continúa si quien la pide es la persona
/// propietaria, tiene un rol con potestad para verla, o el archivo es de acceso general dentro
/// de la comunidad, como el programa de un curso.
/// </summary>
[Authorize]
public class DocumentosController(
    ContextoMatricula contexto,
    IAlmacenamientoArchivos almacen,
    ServicioBitacora bitacora) : Controller
{
    /// <summary>Entrega el archivo para mostrarlo dentro de la página, como una fotografía.</summary>
    [HttpGet]
    public Task<IActionResult> Ver(int id) => Entregar(id, comoDescarga: false);

    /// <summary>Entrega el archivo forzando la descarga, como un comprobante en PDF.</summary>
    [HttpGet]
    public Task<IActionResult> Descargar(int id) => Entregar(id, comoDescarga: true);

    private async Task<IActionResult> Entregar(int id, bool comoDescarga)
    {
        var documento = await contexto.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

        if (documento is null)
        {
            return NotFound();
        }

        if (!await PuedeVerAsync(documento))
        {
            // Se registra el intento: un acceso denegado a un documento ajeno es justo el tipo
            // de evento que la oficina de registro querría poder revisar después.
            await bitacora.RegistrarYGuardarAsync("Descarga denegada", nameof(Documento),
                id.ToString(), documento.NombreOriginal);

            return Forbid();
        }

        byte[] contenido;

        try
        {
            contenido = await almacen.LeerAsync(documento);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }

        if (comoDescarga)
        {
            await bitacora.RegistrarYGuardarAsync("Descargar documento", nameof(Documento),
                id.ToString(), documento.NombreOriginal);

            return File(contenido, documento.TipoContenido, documento.NombreOriginal);
        }

        return File(contenido, documento.TipoContenido);
    }

    /// <summary>
    /// Reglas de acceso a un documento, de la más barata a la más costosa de evaluar.
    /// </summary>
    private async Task<bool> PuedeVerAsync(Documento documento)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // La administración audita todo el acervo documental.
        if (User.IsInRole(RolesSistema.Administrador))
        {
            return true;
        }

        // Cada quien ve lo suyo.
        if (documento.PropietarioUsuarioId == usuarioId)
        {
            return true;
        }

        // Las fotografías de perfil las ve el personal académico, porque el docente necesita
        // reconocer a su grupo en la lista de clase.
        if (documento.Categoria == CategoriaDocumento.Fotografia && User.IsInRole(RolesSistema.Docente))
        {
            return true;
        }

        // El programa de un curso lo ve quien esté matriculado en ese grupo.
        if (documento.Categoria == CategoriaDocumento.ProgramaCurso && usuarioId is not null)
        {
            return await contexto.Grupos
                .Where(g => g.ProgramaDocumentoId == documento.Id)
                .AnyAsync(g => g.Detalles.Any(d => d.Matricula!.EstudianteId == usuarioId
                                                && d.Estado == EstadoDetalleMatricula.Activo));
        }

        return false;
    }
}
