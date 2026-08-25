using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Controladores;

/// <summary>
/// Catálogo de cursos de la persona estudiante. Corresponde a las pantallas 3 y 4 del prototipo:
/// la lista de cursos disponibles de su carrera y el detalle de cada uno.
/// </summary>
[Authorize(Policy = Politicas.SoloEstudiantado)]
public class CursosController(
    ServicioCatalogo catalogo,
    ServicioMatricula matricula,
    ContextoMatricula contexto,
    UserManager<Usuario> gestorUsuarios) : Controller
{
    /// <summary>
    /// Pantalla 3 del prototipo. El filtrado y la paginación funcionan tanto con recarga
    /// completa como sin ella: cuando la petición llega desde el cliente asíncrono se devuelve
    /// solo la tabla, y cuando llega desde el navegador se devuelve la página entera.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Disponibles(FiltroCatalogo filtro)
    {
        var estudianteId = gestorUsuarios.GetUserId(User)!;
        var modelo = await catalogo.ObtenerCatalogoAsync(estudianteId, filtro);

        ViewData["Titulo"] = "Cursos disponibles";

        if (EsPeticionAsincrona())
        {
            return PartialView("_TablaCatalogo", modelo);
        }

        return View(modelo);
    }

    /// <summary>Pantalla 4 del prototipo: la ficha completa del grupo.</summary>
    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var grupo = await contexto.Grupos
            .AsNoTracking()
            .Include(g => g.Curso)
            .Include(g => g.Docente)
            .Include(g => g.PeriodoAcademico)
            .Include(g => g.ProgramaDocumento)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grupo?.Curso is null)
        {
            return NotFound();
        }

        var estudianteId = gestorUsuarios.GetUserId(User)!;

        // Se reutiliza el catálogo para no duplicar la evaluación de las reglas: se pide el
        // catálogo sin filtros y se busca la fila del grupo. Así el detalle nunca contradice
        // lo que la lista dice sobre el mismo grupo.
        var completo = await catalogo.ObtenerCatalogoAsync(estudianteId, new FiltroCatalogo { Pagina = 1 });

        var filaEnCatalogo = await BuscarFilaAsync(estudianteId, id);

        var requisitos = await contexto.Requisitos
            .AsNoTracking()
            .Include(r => r.CursoRequisito)
            .Where(r => r.CursoId == grupo.CursoId)
            .Select(r => r.CursoRequisito!)
            .ToListAsync();

        var aprobados = await matricula.ObtenerCursosAprobadosAsync(estudianteId);

        ViewData["Titulo"] = $"{grupo.Curso.Codigo} · {grupo.Curso.Nombre}";

        return View(new ModeloDetalleGrupo
        {
            Grupo = grupo,
            Fila = filaEnCatalogo,
            Inscritos = filaEnCatalogo?.Inscritos ?? await matricula.ContarInscritosAsync(grupo.Id),
            Requisitos = requisitos,
            CursosAprobados = aprobados,
            NombreCarrera = completo.NombreCarrera,
            OtrosGrupos = await contexto.Grupos
                .AsNoTracking()
                .Include(g => g.Docente)
                .Where(g => g.CursoId == grupo.CursoId
                         && g.PeriodoAcademicoId == grupo.PeriodoAcademicoId
                         && g.Id != grupo.Id
                         && g.Estado != EstadoGrupo.Cancelado)
                .OrderBy(g => g.NumeroGrupo)
                .ToListAsync()
        });
    }

    /// <summary>
    /// Localiza la fila del catálogo que corresponde a un grupo, recorriendo las páginas hasta
    /// dar con ella. El catálogo de una carrera no supera unas pocas decenas de grupos, así que
    /// el recorrido es barato y evita duplicar la lógica de reglas en este controlador.
    /// </summary>
    private async Task<CursoDisponible?> BuscarFilaAsync(string estudianteId, int grupoId)
    {
        var pagina = 1;

        while (true)
        {
            var resultado = await catalogo.ObtenerCatalogoAsync(
                estudianteId, new FiltroCatalogo { Pagina = pagina });

            var fila = resultado.Resultado.Elementos.FirstOrDefault(f => f.GrupoId == grupoId);

            if (fila is not null || pagina >= resultado.Resultado.TotalPaginas)
            {
                return fila;
            }

            pagina++;
        }
    }

    private bool EsPeticionAsincrona() =>
        string.Equals(Request.Headers["X-Solicitud-Asincrona"], "1", StringComparison.Ordinal);
}
