using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ProyectoFinal_GuillenJose.AyudantesEtiqueta;

/// <summary>
/// Componente propio número seis: la barra de ocupación de cupos. Se escribe como
/// <c>&lt;barra-ocupacion inscritos="18" cupo="25" /&gt;</c> y dibuja una barra proporcional
/// con el texto de espacios libres.
///
/// El color cambia según qué tan lleno esté el grupo, porque en la pantalla de matrícula lo
/// que la persona necesita saber de un vistazo es si le conviene apurarse.
/// </summary>
[HtmlTargetElement("barra-ocupacion", TagStructure = TagStructure.WithoutEndTag)]
public class BarraOcupacionTagHelper : TagHelper
{
    [HtmlAttributeName("inscritos")]
    public int Inscritos { get; set; }

    [HtmlAttributeName("cupo")]
    public int Cupo { get; set; }

    /// <summary>Cuando es falso solo se dibuja la barra, sin el texto al costado.</summary>
    [HtmlAttributeName("con-texto")]
    public bool ConTexto { get; set; } = true;

    public override void Process(TagHelperContext contexto, TagHelperOutput salida)
    {
        var porcentaje = Cupo <= 0 ? 100 : Math.Min(100, (int)Math.Round(Inscritos * 100d / Cupo));
        var disponibles = Math.Max(0, Cupo - Inscritos);

        var variante = porcentaje >= 100 ? " ocupacion__relleno--lleno"
            : porcentaje <= 50 ? " ocupacion__relleno--holgado"
            : string.Empty;

        var texto = disponibles switch
        {
            0 => "Sin cupo",
            1 => "1 espacio",
            _ => $"{disponibles} espacios"
        };

        var codificador = HtmlEncoder.Default;
        var descripcion = $"{Inscritos} de {Cupo} espacios ocupados";

        salida.TagName = "span";
        salida.TagMode = TagMode.StartTagAndEndTag;
        salida.Attributes.SetAttribute("class", "ocupacion");
        salida.Attributes.SetAttribute("title", descripcion);

        var contenido =
            $"<span class=\"ocupacion__pista\" role=\"img\" aria-label=\"{codificador.Encode(descripcion)}\">" +
            $"<span class=\"ocupacion__relleno{variante}\" style=\"width:{porcentaje}%\"></span></span>";

        if (ConTexto)
        {
            contenido += $"<span class=\"ocupacion__texto\">{codificador.Encode(texto)}</span>";
        }

        salida.Content.SetHtmlContent(contenido);
    }
}
