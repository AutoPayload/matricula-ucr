using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.AyudantesEtiqueta;

/// <summary>
/// Componente propio número cinco: la etiqueta de estado. Se escribe como
/// <c>&lt;indicador-estado matricula="@modelo.Estado" /&gt;</c> y se encarga de traducir la
/// enumeración a texto legible y de elegir el color que corresponde.
///
/// Antes de existir este componente, cada vista repetía una cadena de condicionales para
/// pintar lo mismo; ahora la correspondencia entre estado y apariencia está en un solo lugar.
/// </summary>
[HtmlTargetElement("indicador-estado", TagStructure = TagStructure.WithoutEndTag)]
public class IndicadorEstadoTagHelper : TagHelper
{
    [HtmlAttributeName("matricula")]
    public EstadoMatricula? Matricula { get; set; }

    [HtmlAttributeName("detalle")]
    public EstadoDetalleMatricula? Detalle { get; set; }

    [HtmlAttributeName("periodo")]
    public EstadoPeriodo? Periodo { get; set; }

    [HtmlAttributeName("grupo")]
    public EstadoGrupo? Grupo { get; set; }

    /// <summary>Nota final; se muestra como aprobado, reprobado o pendiente.</summary>
    [HtmlAttributeName("nota")]
    public int? Nota { get; set; }

    [HtmlAttributeName("activo")]
    public bool? Activo { get; set; }

    public override void Process(TagHelperContext contexto, TagHelperOutput salida)
    {
        var (texto, tono) = Resolver();

        salida.TagName = "span";
        salida.TagMode = TagMode.StartTagAndEndTag;
        salida.Attributes.SetAttribute("class", $"indicador indicador--{tono}");
        salida.Content.SetHtmlContent(HtmlEncoder.Default.Encode(texto));
    }

    private (string Texto, string Tono) Resolver()
    {
        if (Matricula is { } estadoMatricula)
        {
            return estadoMatricula switch
            {
                EstadoMatricula.Confirmada => ("Confirmada", "exito"),
                EstadoMatricula.Borrador => ("En proceso", "atencion"),
                EstadoMatricula.Anulada => ("Anulada", "error"),
                _ => (estadoMatricula.ToString(), "neutro")
            };
        }

        if (Detalle is { } estadoDetalle)
        {
            return estadoDetalle switch
            {
                EstadoDetalleMatricula.Activo => ("Matriculado", "exito"),
                EstadoDetalleMatricula.Retirado => ("Retirado", "error"),
                _ => (estadoDetalle.ToString(), "neutro")
            };
        }

        if (Periodo is { } estadoPeriodo)
        {
            return estadoPeriodo switch
            {
                EstadoPeriodo.MatriculaAbierta => ("Matrícula abierta", "acento"),
                EstadoPeriodo.EnCurso => ("Lecciones en curso", "exito"),
                EstadoPeriodo.Planificado => ("Planificado", "neutro"),
                EstadoPeriodo.Cerrado => ("Cerrado", "neutro"),
                _ => (estadoPeriodo.ToString(), "neutro")
            };
        }

        if (Grupo is { } estadoGrupo)
        {
            return estadoGrupo switch
            {
                EstadoGrupo.Abierto => ("Abierto", "exito"),
                EstadoGrupo.CerradoPorCupo => ("Sin cupo", "atencion"),
                EstadoGrupo.Cancelado => ("Cancelado", "error"),
                _ => (estadoGrupo.ToString(), "neutro")
            };
        }

        if (Nota is { } notaFinal)
        {
            return notaFinal >= DetalleMatricula.NotaAprobacion
                ? ($"Aprobado · {notaFinal}", "exito")
                : ($"Reprobado · {notaFinal}", "error");
        }

        if (Activo is { } estaActivo)
        {
            return estaActivo ? ("Activo", "exito") : ("Inactivo", "neutro");
        }

        return ("Pendiente", "neutro");
    }
}
