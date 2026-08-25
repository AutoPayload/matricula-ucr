using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Contrato del almacén de archivos del sistema. Se define como interfaz para que la
/// implementación en disco pueda sustituirse por un almacenamiento en la nube sin tocar los
/// controladores: la firma es la misma que expondría un adaptador de un servicio remoto.
/// </summary>
public interface IAlmacenamientoArchivos
{
    /// <summary>
    /// Guarda el contenido recibido y devuelve el registro de metadatos ya persistido.
    /// </summary>
    /// <param name="archivo">Archivo cargado desde el formulario.</param>
    /// <param name="categoria">Clasificación con la que se guarda.</param>
    /// <param name="propietarioUsuarioId">Cuenta dueña del archivo, base del control de acceso.</param>
    Task<Documento> GuardarAsync(IFormFile archivo, CategoriaDocumento categoria, string? propietarioUsuarioId);

    /// <summary>
    /// Guarda contenido generado por el propio sistema, como un comprobante en PDF.
    /// </summary>
    Task<Documento> GuardarContenidoAsync(
        byte[] contenido,
        string nombreOriginal,
        string tipoContenido,
        CategoriaDocumento categoria,
        string? propietarioUsuarioId);

    /// <summary>Lee el contenido binario de un documento previamente guardado.</summary>
    Task<byte[]> LeerAsync(Documento documento);

    /// <summary>Elimina el archivo físico. Los metadatos los administra quien llama.</summary>
    void Eliminar(Documento documento);

    /// <summary>
    /// Verifica que la extensión y el tamaño del archivo sean aceptables antes de guardarlo.
    /// Devuelve el mensaje de error o nulo cuando el archivo es válido.
    /// </summary>
    string? Validar(IFormFile archivo, CategoriaDocumento categoria);
}
