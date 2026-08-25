namespace ProyectoFinal_GuillenJose.ModelosVista;

/// <summary>
/// Página de resultados junto con los datos que necesita el control de paginación para dibujarse.
/// Se usa tanto en las vistas Razor como en las respuestas JSON que consume el cliente AJAX.
/// </summary>
/// <typeparam name="T">Tipo de los elementos de la página.</typeparam>
public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Elementos { get; init; } = [];
    public int PaginaActual { get; init; } = 1;
    public int TamanoPagina { get; init; } = 10;
    public int TotalElementos { get; init; }

    public int TotalPaginas => TamanoPagina <= 0
        ? 1
        : Math.Max(1, (int)Math.Ceiling(TotalElementos / (double)TamanoPagina));

    public bool HayAnterior => PaginaActual > 1;
    public bool HaySiguiente => PaginaActual < TotalPaginas;

    /// <summary>Número de la primera fila mostrada, para el texto "1 a 8 de 24".</summary>
    public int PrimeraFila => TotalElementos == 0 ? 0 : ((PaginaActual - 1) * TamanoPagina) + 1;

    /// <summary>Número de la última fila mostrada.</summary>
    public int UltimaFila => Math.Min(PaginaActual * TamanoPagina, TotalElementos);

    public static ResultadoPaginado<T> Crear(IEnumerable<T> elementos, int pagina, int tamano, int total) =>
        new()
        {
            Elementos = elementos.ToList(),
            PaginaActual = pagina < 1 ? 1 : pagina,
            TamanoPagina = tamano < 1 ? 10 : tamano,
            TotalElementos = total
        };
}
