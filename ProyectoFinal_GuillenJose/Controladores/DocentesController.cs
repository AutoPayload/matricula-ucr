using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Controladores;

/// <summary>
/// Mantenimiento del personal docente. Además del alta y la edición, desde aquí se le crea la
/// cuenta de acceso al portal, que es un paso aparte porque no todo docente necesita entrar al
/// sistema y porque la cuenta implica una contraseña que hay que comunicarle a la persona.
/// </summary>
[Authorize(Policy = Politicas.SoloAdministracion)]
public class DocentesController(
    ContextoMatricula contexto,
    UserManager<Usuario> gestorUsuarios,
    ServicioBitacora bitacora,
    IOptions<OpcionesMatricula> opciones) : Controller
{
    private readonly int _tamanoPagina = opciones.Value.TamanoPagina;

    [HttpGet]
    public async Task<IActionResult> Index(string? texto, bool? activo, bool? conCuenta, int pagina = 1)
    {
        ViewData["Titulo"] = "Personal docente";

        var consulta = contexto.Docentes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var termino = texto.Trim();
            consulta = consulta.Where(d => d.Nombre.Contains(termino)
                                        || d.Apellidos.Contains(termino)
                                        || d.Especialidad.Contains(termino)
                                        || d.Identificacion.Contains(termino)
                                        || d.CorreoInstitucional.Contains(termino));
        }

        if (activo is { } valorActivo)
        {
            consulta = consulta.Where(d => d.Activo == valorActivo);
        }

        if (conCuenta is { } valorCuenta)
        {
            consulta = valorCuenta
                ? consulta.Where(d => d.UsuarioId != null)
                : consulta.Where(d => d.UsuarioId == null);
        }

        ViewBag.Texto = texto;
        ViewBag.Activo = activo;
        ViewBag.ConCuenta = conCuenta;

        return View(await consulta
            .OrderBy(d => d.Apellidos)
            .ThenBy(d => d.Nombre)
            .Select(d => new FilaDocente
            {
                Id = d.Id,
                Identificacion = d.Identificacion,
                NombreCompleto = d.Nombre + " " + d.Apellidos,
                Especialidad = d.Especialidad,
                CorreoInstitucional = d.CorreoInstitucional,
                Telefono = d.Telefono,
                Activo = d.Activo,
                TieneCuenta = d.UsuarioId != null,
                GruposAsignados = d.Grupos.Count
            })
            .PaginarAsync(pagina, _tamanoPagina));
    }

    [HttpGet]
    public async Task<IActionResult> Detalles(int id)
    {
        var docente = await contexto.Docentes
            .AsNoTracking()
            .Include(d => d.Grupos).ThenInclude(g => g.Curso)
            .Include(d => d.Grupos).ThenInclude(g => g.PeriodoAcademico)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (docente is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = docente.NombreCompleto;
        return View(docente);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        ViewData["Titulo"] = "Nuevo docente";
        return View(new Docente { Activo = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Docente docente)
    {
        ViewData["Titulo"] = "Nuevo docente";

        await ValidarUnicidadAsync(docente, null);

        if (!ModelState.IsValid)
        {
            return View(docente);
        }

        docente.UsuarioId = null;
        contexto.Docentes.Add(docente);

        bitacora.Registrar("Crear docente", nameof(Docente), null, docente.NombreCompleto);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = $"{docente.NombreCompleto} quedó registrado en el cuerpo docente.";
        return RedirectToAction(nameof(Detalles), new { id = docente.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var docente = await contexto.Docentes.FindAsync(id);

        if (docente is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Editar {docente.NombreCompleto}";
        return View(docente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Docente docente)
    {
        if (id != docente.Id)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Editar {docente.NombreCompleto}";

        await ValidarUnicidadAsync(docente, id);

        if (!ModelState.IsValid)
        {
            return View(docente);
        }

        var original = await contexto.Docentes.FirstOrDefaultAsync(d => d.Id == id);

        if (original is null)
        {
            return NotFound();
        }

        original.Identificacion = docente.Identificacion;
        original.Nombre = docente.Nombre;
        original.Apellidos = docente.Apellidos;
        original.Especialidad = docente.Especialidad;
        original.CorreoInstitucional = docente.CorreoInstitucional;
        original.Telefono = docente.Telefono;
        original.Activo = docente.Activo;

        bitacora.Registrar("Editar docente", nameof(Docente), id.ToString(), docente.NombreCompleto);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = "Los datos del docente fueron actualizados.";
        return RedirectToAction(nameof(Detalles), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var docente = await contexto.Docentes
            .AsNoTracking()
            .Include(d => d.Grupos)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (docente is null)
        {
            return NotFound();
        }

        ViewData["Titulo"] = $"Eliminar {docente.NombreCompleto}";
        return View(docente);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(int id)
    {
        var docente = await contexto.Docentes.FindAsync(id);

        if (docente is null)
        {
            return NotFound();
        }

        if (await contexto.Grupos.AnyAsync(g => g.DocenteId == id))
        {
            docente.Activo = false;
            bitacora.Registrar("Desactivar docente", nameof(Docente), id.ToString(),
                "Tiene grupos asignados, se desactivó en lugar de eliminarse.");
            await contexto.SaveChangesAsync();

            TempData["Aviso"] = "El docente tiene grupos asignados, así que se desactivó " +
                                "en lugar de eliminarse.";
            return RedirectToAction(nameof(Index));
        }

        contexto.Docentes.Remove(docente);
        bitacora.Registrar("Eliminar docente", nameof(Docente), id.ToString(), docente.NombreCompleto);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = $"{docente.NombreCompleto} fue eliminado del cuerpo docente.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Crea la cuenta de acceso al portal para la persona docente y la enlaza a su expediente.
    /// La contraseña inicial se muestra una sola vez a quien administra, para que la comunique.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCuenta(int id)
    {
        var docente = await contexto.Docentes.FirstOrDefaultAsync(d => d.Id == id);

        if (docente is null)
        {
            return NotFound();
        }

        if (docente.UsuarioId is not null)
        {
            TempData["Aviso"] = "Esta persona ya tiene una cuenta de acceso.";
            return RedirectToAction(nameof(Detalles), new { id });
        }

        if (await gestorUsuarios.FindByEmailAsync(docente.CorreoInstitucional) is not null)
        {
            TempData["Error"] = "Ya existe una cuenta con ese correo institucional.";
            return RedirectToAction(nameof(Detalles), new { id });
        }

        var claveInicial = GenerarClaveInicial();

        var cuenta = new Usuario
        {
            UserName = docente.CorreoInstitucional,
            Email = docente.CorreoInstitucional,
            EmailConfirmed = true,
            Identificacion = docente.Identificacion,
            Nombre = docente.Nombre,
            Apellidos = docente.Apellidos,
            PhoneNumber = docente.Telefono,
            FechaRegistro = DateTime.Now
        };

        var resultado = await gestorUsuarios.CreateAsync(cuenta, claveInicial);

        if (!resultado.Succeeded)
        {
            TempData["Error"] = string.Join(" ", resultado.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Detalles), new { id });
        }

        await gestorUsuarios.AddToRoleAsync(cuenta, RolesSistema.Docente);

        docente.UsuarioId = cuenta.Id;
        bitacora.Registrar("Crear cuenta docente", nameof(Docente), id.ToString(), docente.CorreoInstitucional);
        await contexto.SaveChangesAsync();

        TempData["Exito"] = $"Cuenta creada para {docente.CorreoInstitucional}. " +
                            $"Contraseña inicial: {claveInicial} (comuníquela y solicite el cambio).";

        return RedirectToAction(nameof(Detalles), new { id });
    }

    private async Task ValidarUnicidadAsync(Docente docente, int? idExcluido)
    {
        var repetidaIdentificacion = await contexto.Docentes
            .AnyAsync(d => d.Identificacion == docente.Identificacion && d.Id != idExcluido);

        if (repetidaIdentificacion)
        {
            ModelState.AddModelError(nameof(docente.Identificacion),
                "Ya existe un docente con esa identificación.");
        }

        var repetidoCorreo = await contexto.Docentes
            .AnyAsync(d => d.CorreoInstitucional == docente.CorreoInstitucional && d.Id != idExcluido);

        if (repetidoCorreo)
        {
            ModelState.AddModelError(nameof(docente.CorreoInstitucional),
                "Ya existe un docente con ese correo institucional.");
        }
    }

    /// <summary>
    /// Contraseña temporal legible pero no adivinable. Cumple la política del sistema:
    /// mayúscula, minúsculas y dígitos, con al menos ocho caracteres.
    /// </summary>
    private static string GenerarClaveInicial()
    {
        var sufijo = Random.Shared.Next(1000, 9999);
        return $"Docencia{sufijo}";
    }
}
