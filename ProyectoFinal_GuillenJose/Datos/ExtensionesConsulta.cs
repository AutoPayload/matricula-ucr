using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.ModelosVista;

namespace ProyectoFinal_GuillenJose.Datos;

/// <summary>
/// Extensiones de consulta compartidas por los listados de administración. Se agrupan aquí
/// porque los seis mantenimientos pagina igual y no tiene sentido repetir el mismo bloque
/// de Skip y Take en cada controlador.
/// </summary>
public static class ExtensionesConsulta
{
    /// <summary>
    /// Ejecuta la consulta en dos pasos: primero cuenta el total y luego trae solo la página
    /// pedida. Si el número de página se pasa del final, se devuelve la última página con
    /// contenido en lugar de una lista vacía, que es lo que la persona espera al filtrar.
    /// </summary>
    public static async Task<ResultadoPaginado<T>> PaginarAsync<T>(
        this IQueryable<T> consulta, int pagina, int tamano)
    {
        if (tamano < 1)
        {
            tamano = 10;
        }

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamano));

        if (pagina < 1)
        {
            pagina = 1;
        }

        if (pagina > totalPaginas)
        {
            pagina = totalPaginas;
        }

        var elementos = await consulta
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return ResultadoPaginado<T>.Crear(elementos, pagina, tamano, total);
    }
}
