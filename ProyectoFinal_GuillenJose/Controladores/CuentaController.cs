using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Controladores;

/// <summary>
/// Registro, ingreso y perfil de las personas usuarias. No se usa la interfaz predeterminada de
/// ASP.NET Identity: se escribieron pantallas propias para que el idioma, los mensajes y el
/// diseño sean los mismos del resto del sistema y para poder pedir la carrera en el registro,
/// tal como quedó definido en el prototipo.
/// </summary>
public class CuentaController(
    UserManager<Usuario> gestorUsuarios,
    SignInManager<Usuario> gestorSesiones,
    ContextoMatricula contexto,
    IAlmacenamientoArchivos almacen,
    ServicioNotificaciones notificaciones,
    ServicioBitacora bitacora) : Controller
{
    // =================================================================================
    //  Registro
    // =================================================================================

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Inicio");
        }

        ViewData["Titulo"] = "Crear una cuenta";
        return View(new ModeloRegistro { Carreras = await ListarCarrerasAsync() });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(ModeloRegistro modelo)
    {
        ViewData["Titulo"] = "Crear una cuenta";

        if (!ModelState.IsValid)
        {
            modelo.Carreras = await ListarCarrerasAsync();
            return View(modelo);
        }

        var identificacionNormalizada = new string([.. modelo.Identificacion.Where(char.IsDigit)]);

        if (await contexto.Users.AnyAsync(u => u.Identificacion == identificacionNormalizada))
        {
            ModelState.AddModelError(nameof(modelo.Identificacion),
                "Ya existe una cuenta registrada con esa identificación.");
            modelo.Carreras = await ListarCarrerasAsync();
            return View(modelo);
        }

        var cuenta = new Usuario
        {
            UserName = modelo.Correo,
            Email = modelo.Correo,
            EmailConfirmed = true,
            Identificacion = identificacionNormalizada,
            Nombre = modelo.Nombre.Trim(),
            Apellidos = modelo.Apellidos.Trim(),
            FechaNacimiento = modelo.FechaNacimiento,
            CarreraId = modelo.CarreraId,
            FechaRegistro = DateTime.Now
        };

        var resultado = await gestorUsuarios.CreateAsync(cuenta, modelo.Clave);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, TraducirError(error));
            }

            modelo.Carreras = await ListarCarrerasAsync();
            return View(modelo);
        }

        // Toda cuenta creada desde el portal entra como estudiante; los roles de docencia y
        // administración los otorga la oficina de registro.
        await gestorUsuarios.AddToRoleAsync(cuenta, RolesSistema.Estudiante);

        notificaciones.Emitir(cuenta.Id, "Bienvenida a MatrículaUCR",
            "Su cuenta quedó registrada. Revise la oferta de cursos de su carrera para armar la matrícula.",
            "/Cursos/Disponibles");

        bitacora.Registrar("Registro de cuenta", nameof(Usuario), cuenta.Id,
            $"Alta desde el portal con la carrera {modelo.CarreraId}.");

        await contexto.SaveChangesAsync();
        await gestorSesiones.SignInAsync(cuenta, isPersistent: false);

        TempData["Exito"] = $"Su cuenta quedó lista, {cuenta.Nombre}. Ya puede matricular cursos.";
        return RedirectToAction("Disponibles", "Cursos");
    }

    // =================================================================================
    //  Ingreso y salida
    // =================================================================================

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Ingreso(string? rutaRetorno = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Inicio");
        }

        ViewData["Titulo"] = "Iniciar sesión";
        return View(new ModeloIngreso { RutaRetorno = rutaRetorno });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingreso(ModeloIngreso modelo)
    {
        ViewData["Titulo"] = "Iniciar sesión";

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await gestorSesiones.PasswordSignInAsync(
            modelo.Correo, modelo.Clave, modelo.Recordarme, lockoutOnFailure: true);

        if (resultado.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "La cuenta quedó bloqueada por varios intentos fallidos. Vuelva a intentarlo en cinco minutos.");
            return View(modelo);
        }

        if (!resultado.Succeeded)
        {
            // El mensaje es deliberadamente genérico: decir cuál de los dos datos falló le
            // serviría a quien intenta averiguar qué correos están registrados.
            ModelState.AddModelError(string.Empty, "El correo o la contraseña no son correctos.");
            return View(modelo);
        }

        await bitacora.RegistrarYGuardarAsync("Inicio de sesión", nameof(Usuario), null, modelo.Correo);

        if (!string.IsNullOrWhiteSpace(modelo.RutaRetorno) && Url.IsLocalUrl(modelo.RutaRetorno))
        {
            return Redirect(modelo.RutaRetorno);
        }

        return RedirectToAction("Index", "Inicio");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await bitacora.RegistrarYGuardarAsync("Cierre de sesión", nameof(Usuario));
        await gestorSesiones.SignOutAsync();

        return RedirectToAction("Index", "Inicio");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccesoDenegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["Titulo"] = "Acceso denegado";
        return View();
    }

    // =================================================================================
    //  Perfil
    // =================================================================================

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Perfil()
    {
        var cuenta = await ObtenerCuentaAsync();

        if (cuenta is null)
        {
            return Challenge();
        }

        ViewData["Titulo"] = "Mi perfil";
        return View(await ArmarPerfilAsync(cuenta));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(ModeloPerfil modelo)
    {
        var cuenta = await ObtenerCuentaAsync();

        if (cuenta is null)
        {
            return Challenge();
        }

        ViewData["Titulo"] = "Mi perfil";

        if (!ModelState.IsValid)
        {
            return View(await ArmarPerfilAsync(cuenta, modelo));
        }

        cuenta.Nombre = modelo.Nombre.Trim();
        cuenta.Apellidos = modelo.Apellidos.Trim();
        cuenta.PhoneNumber = modelo.Telefono;
        cuenta.FechaNacimiento = modelo.FechaNacimiento;

        if (modelo.Fotografia is not null)
        {
            var error = almacen.Validar(modelo.Fotografia, CategoriaDocumento.Fotografia);

            if (error is not null)
            {
                ModelState.AddModelError(nameof(modelo.Fotografia), error);
                return View(await ArmarPerfilAsync(cuenta, modelo));
            }

            var documento = await almacen.GuardarAsync(
                modelo.Fotografia, CategoriaDocumento.Fotografia, cuenta.Id);

            cuenta.FotografiaDocumentoId = documento.Id;
        }

        await gestorUsuarios.UpdateAsync(cuenta);

        // La cookie guarda el nombre y la fotografía, así que hay que reemitirla para que la
        // barra superior refleje el cambio sin exigir un nuevo inicio de sesión.
        await gestorSesiones.RefreshSignInAsync(cuenta);

        await bitacora.RegistrarYGuardarAsync("Actualizar perfil", nameof(Usuario), cuenta.Id);

        TempData["Exito"] = "Su perfil quedó actualizado.";
        return RedirectToAction(nameof(Perfil));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarClave(ModeloCambioClave modelo)
    {
        var cuenta = await ObtenerCuentaAsync();

        if (cuenta is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise los datos del cambio de contraseña.";
            return RedirectToAction(nameof(Perfil));
        }

        var resultado = await gestorUsuarios.ChangePasswordAsync(
            cuenta, modelo.ClaveActual, modelo.ClaveNueva);

        if (!resultado.Succeeded)
        {
            TempData["Error"] = string.Join(" ", resultado.Errors.Select(TraducirError));
            return RedirectToAction(nameof(Perfil));
        }

        await gestorSesiones.RefreshSignInAsync(cuenta);
        await bitacora.RegistrarYGuardarAsync("Cambio de contraseña", nameof(Usuario), cuenta.Id);

        TempData["Exito"] = "Su contraseña fue actualizada.";
        return RedirectToAction(nameof(Perfil));
    }

    // =================================================================================
    //  Avisos
    // =================================================================================

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Avisos()
    {
        var usuarioId = gestorUsuarios.GetUserId(User)!;
        var pendientes = await notificaciones.ContarPendientesAsync(usuarioId);
        var avisos = await notificaciones.ObtenerRecientesAsync(usuarioId, 40);

        await notificaciones.MarcarTodasLeidasAsync(usuarioId);

        ViewData["Titulo"] = "Avisos";
        return View(new ModeloAvisos { Avisos = avisos, Pendientes = pendientes });
    }

    // =================================================================================
    //  Apoyos privados
    // =================================================================================

    private async Task<Usuario?> ObtenerCuentaAsync()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return usuarioId is null ? null : await gestorUsuarios.FindByIdAsync(usuarioId);
    }

    private async Task<ModeloPerfil> ArmarPerfilAsync(Usuario cuenta, ModeloPerfil? edicion = null)
    {
        var carrera = cuenta.CarreraId is null
            ? null
            : await contexto.Carreras.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cuenta.CarreraId);

        var roles = await gestorUsuarios.GetRolesAsync(cuenta);

        return new ModeloPerfil
        {
            UsuarioId = cuenta.Id,
            Correo = cuenta.Email ?? string.Empty,
            Identificacion = cuenta.Identificacion,
            Nombre = edicion?.Nombre ?? cuenta.Nombre,
            Apellidos = edicion?.Apellidos ?? cuenta.Apellidos,
            Telefono = edicion?.Telefono ?? cuenta.PhoneNumber,
            FechaNacimiento = edicion?.FechaNacimiento ?? cuenta.FechaNacimiento,
            NombreCarrera = carrera?.Nombre ?? "Sin carrera asignada",
            Rol = roles.FirstOrDefault() ?? "Sin rol",
            FotografiaDocumentoId = cuenta.FotografiaDocumentoId,
            FechaRegistro = cuenta.FechaRegistro
        };
    }

    private async Task<List<SelectListItem>> ListarCarrerasAsync() =>
        await contexto.Carreras
            .AsNoTracking()
            .Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .Select(c => new SelectListItem(c.Nombre, c.Id.ToString()))
            .ToListAsync();

    /// <summary>
    /// Identity devuelve sus mensajes en inglés. Se traducen los que la persona usuaria puede
    /// llegar a ver, y el resto se deja pasar para no ocultar información al depurar.
    /// </summary>
    private static string TraducirError(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Ya existe una cuenta con ese correo electrónico.",
        "PasswordTooShort" => "La contraseña debe tener al menos 8 caracteres.",
        "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número.",
        "PasswordRequiresUpper" => "La contraseña debe incluir al menos una letra mayúscula.",
        "PasswordRequiresLower" => "La contraseña debe incluir al menos una letra minúscula.",
        "PasswordMismatch" => "La contraseña actual no es correcta.",
        "InvalidEmail" => "El correo electrónico no tiene un formato válido.",
        _ => error.Description
    };
}
