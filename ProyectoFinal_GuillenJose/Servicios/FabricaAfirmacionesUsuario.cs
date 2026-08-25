using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Añade a la identidad autenticada las afirmaciones propias del dominio. Sin esto, cada vista
/// tendría que consultar la base de datos para saber el nombre de la persona o si tiene
/// fotografía cargada; con esto la información viaja en la cookie y las vistas se limitan a leerla.
/// </summary>
public class FabricaAfirmacionesUsuario(
    UserManager<Usuario> gestorUsuarios,
    RoleManager<IdentityRole> gestorRoles,
    IOptions<IdentityOptions> opciones)
    : UserClaimsPrincipalFactory<Usuario, IdentityRole>(gestorUsuarios, gestorRoles, opciones)
{
    public const string AfirmacionNombreCompleto = "nombreCompleto";
    public const string AfirmacionIniciales = "iniciales";
    public const string AfirmacionIdentificacion = "identificacion";
    public const string AfirmacionCarrera = "carreraId";
    public const string AfirmacionFotografia = "fotografiaId";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario usuario)
    {
        var identidad = await base.GenerateClaimsAsync(usuario);

        identidad.AddClaim(new Claim(AfirmacionNombreCompleto, usuario.NombreCompleto));
        identidad.AddClaim(new Claim(AfirmacionIniciales, usuario.Iniciales));
        identidad.AddClaim(new Claim(AfirmacionIdentificacion, usuario.Identificacion));

        if (usuario.CarreraId is not null)
        {
            identidad.AddClaim(new Claim(AfirmacionCarrera, usuario.CarreraId.Value.ToString()));
        }

        if (usuario.FotografiaDocumentoId is not null)
        {
            identidad.AddClaim(new Claim(AfirmacionFotografia, usuario.FotografiaDocumentoId.Value.ToString()));
        }

        return identidad;
    }
}
