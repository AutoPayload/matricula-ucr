using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ProyectoFinal_GuillenJose.AyudantesEtiqueta;

/// <summary>
/// Componente propio número cuatro: el control de paginación. Se escribe en la vista como
/// <c>&lt;paginador pagina-actual="3" total-paginas="9" total-elementos="72" ... /&gt;</c> y
/// resuelve por su cuenta los enlaces conservando los filtros activos, de modo que ninguna
/// vista tenga que armar la cadena de consulta a mano.
///
/// Cuando la lista es larga se recorta con puntos suspensivos: siempre se ven la primera y la
/// última página y una ventana de páginas alrededor de la actual.
/// </summary>
[HtmlTargetElement("paginador", TagStructure = TagStructure.WithoutEndTag)]
public class PaginadorTagHelper(IUrlHelperFactory fabricaUrl) : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ContextoVista { get; set; } = default!;

    [HtmlAttributeName("pagina-actual")]
    public int PaginaActual { get; set; } = 1;

    [HtmlAttributeName("total-paginas")]
    public int TotalPaginas { get; set; } = 1;

    [HtmlAttributeName("total-elementos")]
    public int TotalElementos { get; set; }

    [HtmlAttributeName("primera-fila")]
    public int PrimeraFila { get; set; }

    [HtmlAttributeName("ultima-fila")]
    public int UltimaFila { get; set; }

    [HtmlAttributeName("accion")]
    public string Accion { get; set; } = "Index";

    [HtmlAttributeName("controlador")]
    public string? Controlador { get; set; }

    /// <summary>Filtros vigentes que deben conservarse al cambiar de página.</summary>
    [HtmlAttributeName("filtros", DictionaryAttributePrefix = "filtro-")]
    public IDictionary<string, string?> Filtros { get; set; } = new Dictionary<string, string?>();

    [HtmlAttributeName("nombre-singular")]
    public string NombreSingular { get; set; } = "registro";

    [HtmlAttributeName("nombre-plural")]
    public string NombrePlural { get; set; } = "registros";

    public override void Process(TagHelperContext contexto, TagHelperOutput salida)
    {
        salida.TagName = "nav";
        salida.TagMode = TagMode.StartTagAndEndTag;
        salida.Attributes.SetAttribute("class", "paginador");
        salida.Attributes.SetAttribute("aria-label", "Paginación de resultados");

        var ayudante = fabricaUrl.GetUrlHelper(ContextoVista);
        var codificador = HtmlEncoder.Default;
        var constructor = new StringBuilder();

        var resumen = TotalElementos == 0
            ? $"No hay {NombrePlural} que coincidan con el filtro"
            : $"{PrimeraFila} a {UltimaFila} de {TotalElementos} {(TotalElementos == 1 ? NombreSingular : NombrePlural)}";

        constructor.Append("<span class=\"diminuto\">").Append(codificador.Encode(resumen)).Append("</span>");

        if (TotalPaginas > 1)
        {
            constructor.Append("<span class=\"paginador__paginas\">");
            constructor.Append(Enlace(ayudante, codificador, PaginaActual - 1, "Anterior", PaginaActual <= 1));

            foreach (var pagina in CalcularVentana())
            {
                if (pagina == 0)
                {
                    constructor.Append("<span class=\"inerte\">…</span>");
                    continue;
                }

                if (pagina == PaginaActual)
                {
                    constructor.Append("<span class=\"actual\" aria-current=\"page\">")
                               .Append(pagina).Append("</span>");
                }
                else
                {
                    constructor.Append(Enlace(ayudante, codificador, pagina, pagina.ToString(), false));
                }
            }

            constructor.Append(Enlace(ayudante, codificador, PaginaActual + 1, "Siguiente", PaginaActual >= TotalPaginas));
            constructor.Append("</span>");
        }

        salida.Content.SetHtmlContent(constructor.ToString());
    }

    /// <summary>
    /// Decide qué números de página se dibujan. El cero representa el corte con puntos suspensivos.
    /// </summary>
    private IEnumerable<int> CalcularVentana()
    {
        const int Margen = 2;

        if (TotalPaginas <= 7)
        {
            for (var pagina = 1; pagina <= TotalPaginas; pagina++)
            {
                yield return pagina;
            }

            yield break;
        }

        var desde = Math.Max(2, PaginaActual - Margen);
        var hasta = Math.Min(TotalPaginas - 1, PaginaActual + Margen);

        yield return 1;

        if (desde > 2)
        {
            yield return 0;
        }

        for (var pagina = desde; pagina <= hasta; pagina++)
        {
            yield return pagina;
        }

        if (hasta < TotalPaginas - 1)
        {
            yield return 0;
        }

        yield return TotalPaginas;
    }

    private string Enlace(IUrlHelper ayudante, HtmlEncoder codificador, int pagina, string texto, bool inerte)
    {
        if (inerte)
        {
            return $"<span class=\"inerte\">{codificador.Encode(texto)}</span>";
        }

        var valores = new RouteValueDictionary(Filtros.Where(f => !string.IsNullOrWhiteSpace(f.Value))
            .ToDictionary(f => f.Key, f => (object?)f.Value));

        valores["pagina"] = pagina;

        var direccion = Controlador is null
            ? ayudante.Action(Accion, valores)
            : ayudante.Action(Accion, Controlador, valores);

        return $"<a href=\"{codificador.Encode(direccion ?? "#")}\">{codificador.Encode(texto)}</a>";
    }
}
