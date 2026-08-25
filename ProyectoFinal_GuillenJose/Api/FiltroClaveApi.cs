using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProyectoFinal_GuillenJose.Api;

/// <summary>
/// Tercer mecanismo de autenticación del sistema: clave de aplicación en el encabezado, pensada
/// para integraciones entre servidores, donde no hay una persona que inicie sesión ni un
/// navegador que guarde una cookie.
///
/// La comparación se hace en tiempo constante para no filtrar información sobre la clave a
/// partir de cuánto tarda en fallar la verificación.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FiltroClaveApiAttribute : Attribute, IAsyncActionFilter
{
    public const string NombreEncabezado = "X-Clave-Api";
    private const string RutaConfiguracion = "Integracion:ClaveApi";

    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate siguiente)
    {
        var configuracion = contexto.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var claveEsperada = configuracion[RutaConfiguracion];

        if (string.IsNullOrWhiteSpace(claveEsperada))
        {
            contexto.Result = new ObjectResult(new
            {
                titulo = "Integración no configurada",
                estado = 503,
                detalle = "El servidor no tiene definida la clave de integración."
            })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };

            return;
        }

        if (!contexto.HttpContext.Request.Headers.TryGetValue(NombreEncabezado, out var recibida)
            || !EsIgual(recibida.ToString(), claveEsperada))
        {
            contexto.Result = new UnauthorizedObjectResult(new
            {
                titulo = "Clave de integración inválida",
                estado = 401,
                detalle = $"Envíe la clave en el encabezado {NombreEncabezado}."
            });

            return;
        }

        await siguiente();
    }

    /// <summary>Comparación de duración constante entre dos cadenas.</summary>
    private static bool EsIgual(string recibida, string esperada)
    {
        if (recibida.Length != esperada.Length)
        {
            return false;
        }

        var diferencia = 0;

        for (var indice = 0; indice < esperada.Length; indice++)
        {
            diferencia |= recibida[indice] ^ esperada[indice];
        }

        return diferencia == 0;
    }
}
