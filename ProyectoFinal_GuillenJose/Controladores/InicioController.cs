using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;

namespace ProyectoFinal_GuillenJose.Controladores;

/// <summary>
/// Portada del sitio y páginas de servicio. La portada cambia según quién entre: una persona
/// anónima ve la presentación del sistema, y quien ya inició sesión ve un acceso directo a lo
/// que le corresponde por su rol.
/// </summary>
public class InicioController(ContextoMatricula contexto) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["Titulo"] = "Inicio";

        var periodo = await contexto.PeriodosAcademicos
            .AsNoTracking()
            .Where(p => p.Estado == EstadoPeriodo.MatriculaAbierta)
            .OrderByDescending(p => p.FechaInicio)
            .FirstOrDefaultAsync();

        return View(new ModeloPortada
        {
            PeriodoVigente = periodo,
            TotalCarreras = await contexto.Carreras.CountAsync(c => c.Activa),
            TotalCursos = await contexto.Cursos.CountAsync(c => c.Activo),
            TotalDocentes = await contexto.Docentes.CountAsync(d => d.Activo),
            TotalGrupos = periodo is null
                ? 0
                : await contexto.Grupos.CountAsync(g => g.PeriodoAcademicoId == periodo.Id
                                                     && g.Estado == EstadoGrupo.Abierto)
        });
    }

    /// <summary>
    /// Página que atiende los códigos 403 y 404 con la identidad visual del sistema, en lugar
    /// de la pantalla en blanco del servidor.
    /// </summary>
    [Route("Inicio/CodigoEstado/{codigo:int}")]
    public IActionResult CodigoEstado(int codigo)
    {
        ViewData["Titulo"] = codigo switch
        {
            403 => "Acceso denegado",
            404 => "Página no encontrada",
            _ => "Solicitud no procesada"
        };

        return View(new ModeloCodigoEstado
        {
            Codigo = codigo,
            Titulo = ViewData["Titulo"]!.ToString()!,
            Explicacion = codigo switch
            {
                403 => "Su cuenta no tiene permiso para ver esta sección. Si cree que se trata de un error, " +
                       "comuníquese con la oficina de registro.",
                404 => "La dirección solicitada no existe o el registro fue eliminado.",
                _ => "La solicitud no pudo atenderse. Intente de nuevo en unos minutos."
            }
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["Titulo"] = "Error inesperado";

        return View(new ModeloError
        {
            IdentificadorSolicitud = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    /// <summary>Página informativa con las credenciales de demostración del proyecto.</summary>
    public IActionResult Acerca()
    {
        ViewData["Titulo"] = "Acerca del sistema";
        return View();
    }
}
