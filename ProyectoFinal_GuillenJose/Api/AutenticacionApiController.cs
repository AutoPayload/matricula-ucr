using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Api;

/// <summary>
/// Punto de entrada del segundo mecanismo de autenticación. La persona ya entró al sitio con su
/// cookie de sesión; aquí canjea esa sesión por un token JWT de vida corta que el navegador
/// adjunta a cada petición asíncrona.
///
/// El canje se pide desde el cliente al cargar la página, de modo que el token nunca queda
/// escrito en el HTML ni sobrevive a la pestaña.
/// </summary>
[ApiController]
[Route("api/autenticacion")]
public class AutenticacionApiController(
    UserManager<Usuario> gestorUsuarios,
    ServicioTokens servicioTokens) : ControllerBase
{
    /// <summary>Emite un token para la sesión vigente.</summary>
    [HttpPost("token")]
    [Authorize(AuthenticationSchemes = Esquemas.Cookie)]
    public async Task<IActionResult> Emitir()
    {
        var cuenta = await gestorUsuarios.GetUserAsync(User);

        if (cuenta is null)
        {
            return Unauthorized(new { titulo = "Sesión no válida", estado = 401 });
        }

        var roles = await gestorUsuarios.GetRolesAsync(cuenta);
        var (token, expiraEn) = servicioTokens.GenerarToken(cuenta, roles);

        return Ok(new
        {
            token,
            expiraEn,
            tipo = "Bearer",
            nombre = cuenta.NombreCompleto,
            roles
        });
    }

    /// <summary>Comprobación de estado, útil para verificar que la API responde.</summary>
    [HttpGet("~/api/salud")]
    [AllowAnonymous]
    public IActionResult Salud() => Ok(new
    {
        estado = "En funcionamiento",
        aplicacion = "MatrículaUCR",
        version = "1.0.0",
        fechaServidor = DateTime.Now
    });
}
