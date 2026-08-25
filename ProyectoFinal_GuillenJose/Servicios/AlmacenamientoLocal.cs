using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Implementación del almacén sobre el sistema de archivos del servidor.
///
/// Tres decisiones de diseño que conviene destacar. Primera: la carpeta vive fuera de wwwroot,
/// así que ningún archivo queda accesible por dirección directa y toda descarga pasa por el
/// controlador que verifica permisos. Segunda: el nombre en disco es un identificador generado,
/// nunca el nombre que trajo la persona usuaria, para evitar el recorrido de rutas. Tercera: se
/// calcula el resumen SHA-256 del contenido, que sirve para detectar cargas repetidas y para
/// comprobar más adelante que el archivo no fue alterado.
/// </summary>
public class AlmacenamientoLocal : IAlmacenamientoArchivos
{
    private readonly ContextoMatricula _contexto;
    private readonly OpcionesAlmacenamiento _opciones;
    private readonly string _rutaRaiz;

    public AlmacenamientoLocal(
        ContextoMatricula contexto,
        IOptions<OpcionesAlmacenamiento> opciones,
        IWebHostEnvironment entorno)
    {
        _contexto = contexto;
        _opciones = opciones.Value;
        _rutaRaiz = Path.Combine(entorno.ContentRootPath, _opciones.CarpetaRaiz);
        Directory.CreateDirectory(_rutaRaiz);
    }

    public string? Validar(IFormFile archivo, CategoriaDocumento categoria)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return "Seleccione un archivo antes de continuar.";
        }

        if (archivo.Length > _opciones.TamanoMaximoBytes)
        {
            var maximoEnMegas = _opciones.TamanoMaximoBytes / 1048576d;
            return $"El archivo supera el tamaño permitido de {maximoEnMegas:N1} MB.";
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var permitidas = ExtensionesPermitidas(categoria);

        if (!permitidas.Contains(extension))
        {
            return $"Solo se aceptan archivos {string.Join(", ", permitidas)}.";
        }

        return null;
    }

    public async Task<Documento> GuardarAsync(
        IFormFile archivo, CategoriaDocumento categoria, string? propietarioUsuarioId)
    {
        ArgumentNullException.ThrowIfNull(archivo);

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);

        return await GuardarContenidoAsync(
            memoria.ToArray(),
            Path.GetFileName(archivo.FileName),
            string.IsNullOrWhiteSpace(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType,
            categoria,
            propietarioUsuarioId);
    }

    public async Task<Documento> GuardarContenidoAsync(
        byte[] contenido,
        string nombreOriginal,
        string tipoContenido,
        CategoriaDocumento categoria,
        string? propietarioUsuarioId)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        var resumen = Convert.ToHexString(SHA256.HashData(contenido)).ToLowerInvariant();
        var extension = Path.GetExtension(nombreOriginal);
        var nombreAlmacenado = $"{Guid.NewGuid():N}{extension}";

        // Si el mismo contenido ya está en disco para la misma categoría y propietario, se
        // reutiliza el archivo físico y solo se registra la referencia nueva.
        var existente = await _contexto.Documentos
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.HashSha256 == resumen && d.Categoria == categoria);

        if (existente is not null && File.Exists(RutaFisica(existente.NombreAlmacenado)))
        {
            nombreAlmacenado = existente.NombreAlmacenado;
        }
        else
        {
            await File.WriteAllBytesAsync(RutaFisica(nombreAlmacenado), contenido);
        }

        var documento = new Documento
        {
            NombreOriginal = nombreOriginal,
            NombreAlmacenado = nombreAlmacenado,
            TipoContenido = tipoContenido,
            TamanoBytes = contenido.LongLength,
            HashSha256 = resumen,
            Categoria = categoria,
            PropietarioUsuarioId = propietarioUsuarioId,
            FechaCarga = DateTime.Now
        };

        _contexto.Documentos.Add(documento);
        await _contexto.SaveChangesAsync();

        return documento;
    }

    public async Task<byte[]> LeerAsync(Documento documento)
    {
        ArgumentNullException.ThrowIfNull(documento);

        var ruta = RutaFisica(documento.NombreAlmacenado);

        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException(
                "El archivo solicitado ya no está disponible en el almacén.", documento.NombreOriginal);
        }

        return await File.ReadAllBytesAsync(ruta);
    }

    public void Eliminar(Documento documento)
    {
        ArgumentNullException.ThrowIfNull(documento);

        var ruta = RutaFisica(documento.NombreAlmacenado);

        if (File.Exists(ruta))
        {
            File.Delete(ruta);
        }
    }

    private string[] ExtensionesPermitidas(CategoriaDocumento categoria) =>
        categoria == CategoriaDocumento.Fotografia
            ? _opciones.ExtensionesImagen
            : _opciones.ExtensionesDocumento;

    /// <summary>
    /// Compone la ruta física y se asegura de que quede dentro de la carpeta del almacén, aunque
    /// el nombre recibido intentara subir de directorio.
    /// </summary>
    private string RutaFisica(string nombreAlmacenado)
    {
        var candidata = Path.GetFullPath(Path.Combine(_rutaRaiz, Path.GetFileName(nombreAlmacenado)));

        if (!candidata.StartsWith(Path.GetFullPath(_rutaRaiz), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La ruta solicitada queda fuera del almacén de archivos.");
        }

        return candidata;
    }
}
