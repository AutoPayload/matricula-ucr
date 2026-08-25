using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Emite los tokens JWT firmados con HMAC-SHA256 que protegen la API interna. Es el segundo
/// mecanismo de autenticación del sistema: el navegador entra con cookie, pero las peticiones
/// asíncronas que el propio sitio dispara viajan con un token de vida corta, de manera que la
/// API pueda consumirse también desde una aplicación móvil sin depender de la sesión web.
/// </summary>
public class ServicioTokens(IOptions<OpcionesJwt> opciones)
{
    private readonly OpcionesJwt _opciones = opciones.Value;

    /// <summary>
    /// Construye el token de la persona usuaria indicada e informa el momento en que expira.
    /// </summary>
    /// <param name="usuario">Cuenta autenticada por cookie que solicita el token.</param>
    /// <param name="roles">Roles vigentes de la cuenta, ya resueltos por Identity.</param>
    public (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario, IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var expiraEnUtc = DateTime.UtcNow.AddMinutes(_opciones.MinutosDeVigencia);

        var afirmaciones = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id),
            new(JwtRegisteredClaimNames.UniqueName, usuario.UserName ?? usuario.Email ?? usuario.Id),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(ClaimTypes.Name, usuario.UserName ?? usuario.Id),
            new("nombreCompleto", usuario.NombreCompleto),
            new("identificacion", usuario.Identificacion)
        };

        // Un rol por afirmación: así el atributo de autorización por roles funciona igual
        // sobre el token que sobre la cookie.
        afirmaciones.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol)));

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.ClaveSecreta));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: afirmaciones,
            notBefore: DateTime.UtcNow,
            expires: expiraEnUtc,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEnUtc);
    }
}
