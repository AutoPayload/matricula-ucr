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
/// Portal de la persona docente: sus grupos del periodo, la lista de clase, el registro de la
/// nota final, el cierre del acta y la publicación del programa del curso.
///
/// Todas las acciones verifican que el grupo pertenezca a quien las solicita. No basta con el
/// rol: un docente no puede calificar el grupo de otro aunque conozca el identificador.
/// </summary>
[Authorize(Policy = Politicas.SoloDocencia)]
public class DocenciaController(
    ContextoMatricula contexto,
    UserManager<Usuario> gestorUsuarios,
    IAlmacenamientoArchivos almacen,
    ServicioComprobantes comprobantes,
    ServicioNotificaciones notificaciones,
    ServicioBitacora bitacora) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? periodoId)
    {
        ViewData["Titulo"] = "Mis grupos";

        var docente = await ObtenerDocenteAsync();

        if (docente is null)
        {
            return View(new ModeloMisGrupos
            {
                NombreDocente = "Sin expediente docente",
                Especialidad = "Su cuenta tiene el rol de docencia pero no está enlazada a un expediente."
            });
        }

        var periodos = await contexto.PeriodosAcademicos
            .AsNoTracking()
            .Where(p => p.Grupos.Any(g => g.DocenteId == docente.Id))
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync();

        var periodo = periodoId is null
            ? periodos.FirstOrDefault(p => p.Estado != EstadoPeriodo.Cerrado) ?? periodos.FirstOrDefault()
            : periodos.FirstOrDefault(p => p.Id == periodoId);

        var grupos = periodo is null
            ? []
            : await contexto.Grupos
                .AsNoTracking()
                .Include(g => g.Curso)
                .Where(g => g.DocenteId == docente.Id && g.PeriodoAcademicoId == periodo.Id)
                .Select(g => new ResumenGrupo
                {
                    GrupoId = g.Id,
                    Codigo = g.Curso!.Codigo,
                    NombreCurso = g.Curso.Nombre,
                    NumeroGrupo = g.NumeroGrupo,
                    Horario = g.Horario,
                    Aula = g.Aula,
                    Creditos = g.Curso.Creditos,
                    CupoMaximo = g.CupoMaximo,
                    Estado = g.Estado,
                    ActaCerrada = g.ActaCerrada,
                    TienePrograma = g.ProgramaDocumentoId != null,
                    Inscritos = g.Detalles.Count(d => d.Estado == EstadoDetalleMatricula.Activo
                                                   && d.Matricula!.Estado == EstadoMatricula.Confirmada),
                    NotasRegistradas = g.Detalles.Count(d => d.Estado == EstadoDetalleMatricula.Activo
                                                          && d.Matricula!.Estado == EstadoMatricula.Confirmada
                                                          && d.NotaFinal != null)
                })
                .OrderBy(g => g.Codigo)
                .ThenBy(g => g.NumeroGrupo)
                .ToListAsync();

        return View(new ModeloMisGrupos
        {
            NombreDocente = docente.NombreCompleto,
            Especialidad = docente.Especialidad,
            Periodo = periodo,
            Periodos = periodos,
            Grupos = grupos
        });
    }

    [HttpGet]
    public async Task<IActionResult> ListaClase(int id)
    {
        var grupo = await ObtenerGrupoPropioAsync(id);

        if (grupo is null)
        {
            return Forbid();
        }

        ViewData["Titulo"] = $"{grupo.Curso!.Codigo} grupo {grupo.NumeroGrupo:00}";

        return View(await ArmarListaAsync(grupo));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarNotas(ModeloRegistroNotas modelo)
    {
        var grupo = await ObtenerGrupoPropioAsync(modelo.GrupoId);

        if (grupo is null)
        {
            return Forbid();
        }

        if (grupo.ActaCerrada)
        {
            TempData["Error"] = "El acta de este grupo ya fue cerrada y las notas no pueden modificarse.";
            return RedirectToAction(nameof(ListaClase), new { id = modelo.GrupoId });
        }

        var identificadores = modelo.Estudiantes.Select(e => e.DetalleId).ToList();

        var detalles = await contexto.DetallesMatricula
            .Include(d => d.Matricula)
            .Include(d => d.Grupo!).ThenInclude(g => g.Curso)
            .Where(d => identificadores.Contains(d.Id) && d.GrupoId == grupo.Id)
            .ToListAsync();

        var registradas = 0;

        foreach (var fila in modelo.Estudiantes)
        {
            var detalle = detalles.FirstOrDefault(d => d.Id == fila.DetalleId);

            if (detalle is null || fila.NotaFinal == detalle.NotaFinal)
            {
                continue;
            }

            if (fila.NotaFinal is < 0 or > 100)
            {
                TempData["Error"] = "Las notas deben estar entre 0 y 100.";
                return RedirectToAction(nameof(ListaClase), new { id = modelo.GrupoId });
            }

            detalle.NotaFinal = fila.NotaFinal;
            detalle.FechaRegistroNota = fila.NotaFinal is null ? null : DateTime.Now;
            registradas++;

            if (fila.NotaFinal is not null)
            {
                notificaciones.Emitir(
                    detalle.Matricula!.EstudianteId,
                    "Nota publicada",
                    $"Su nota final de {detalle.Grupo!.Curso!.Codigo} — {detalle.Grupo.Curso.Nombre} " +
                    $"es {fila.NotaFinal}.",
                    "/Expediente");
            }
        }

        if (registradas == 0)
        {
            TempData["Aviso"] = "No hubo cambios que guardar.";
            return RedirectToAction(nameof(ListaClase), new { id = modelo.GrupoId });
        }

        bitacora.Registrar("Registrar notas", nameof(Grupo), grupo.Id.ToString(),
            $"{registradas} nota(s) actualizada(s) en {grupo.Curso!.Codigo} grupo {grupo.NumeroGrupo:00}.");

        await contexto.SaveChangesAsync();

        TempData["Exito"] = registradas == 1
            ? "Se guardó una nota."
            : $"Se guardaron {registradas} notas.";

        return RedirectToAction(nameof(ListaClase), new { id = modelo.GrupoId });
    }

    /// <summary>
    /// Cierra el acta y genera el documento oficial en PDF. A partir de ese momento las notas
    /// quedan congeladas: cualquier corrección tiene que pasar por la oficina de registro.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarActa(int id)
    {
        var grupo = await ObtenerGrupoPropioAsync(id);

        if (grupo is null)
        {
            return Forbid();
        }

        if (grupo.ActaCerrada)
        {
            TempData["Aviso"] = "El acta de este grupo ya estaba cerrada.";
            return RedirectToAction(nameof(ListaClase), new { id });
        }

        var pendientes = await contexto.DetallesMatricula
            .CountAsync(d => d.GrupoId == grupo.Id
                          && d.Estado == EstadoDetalleMatricula.Activo
                          && d.Matricula!.Estado == EstadoMatricula.Confirmada
                          && d.NotaFinal == null);

        if (pendientes > 0)
        {
            TempData["Error"] = pendientes == 1
                ? "Falta registrar una nota antes de cerrar el acta."
                : $"Faltan {pendientes} notas por registrar antes de cerrar el acta.";

            return RedirectToAction(nameof(ListaClase), new { id });
        }

        var usuarioId = gestorUsuarios.GetUserId(User);
        var documento = await comprobantes.GenerarActaNotasAsync(grupo, usuarioId);

        grupo.ActaCerrada = true;
        grupo.FechaCierreActa = DateTime.Now;

        bitacora.Registrar("Cerrar acta", nameof(Grupo), grupo.Id.ToString(),
            $"Acta {documento.NombreOriginal} generada.");

        await contexto.SaveChangesAsync();

        TempData["Exito"] = "El acta quedó cerrada y el documento está disponible para descarga.";
        return RedirectToAction("Descargar", "Documentos", new { id = documento.Id });
    }

    /// <summary>Publica el programa del curso para que lo descargue el estudiantado del grupo.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CargarPrograma(int id, IFormFile? archivo)
    {
        var grupo = await ObtenerGrupoPropioAsync(id);

        if (grupo is null)
        {
            return Forbid();
        }

        if (archivo is null)
        {
            TempData["Error"] = "Seleccione el archivo del programa antes de enviarlo.";
            return RedirectToAction(nameof(ListaClase), new { id });
        }

        var error = almacen.Validar(archivo, CategoriaDocumento.ProgramaCurso);

        if (error is not null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(ListaClase), new { id });
        }

        var usuarioId = gestorUsuarios.GetUserId(User);
        var documento = await almacen.GuardarAsync(archivo, CategoriaDocumento.ProgramaCurso, usuarioId);

        grupo.ProgramaDocumentoId = documento.Id;

        var matriculados = await contexto.DetallesMatricula
            .Where(d => d.GrupoId == grupo.Id && d.Estado == EstadoDetalleMatricula.Activo)
            .Select(d => d.Matricula!.EstudianteId)
            .Distinct()
            .ToListAsync();

        notificaciones.EmitirEnLote(matriculados, "Programa del curso publicado",
            $"Ya está disponible el programa de {grupo.Curso!.Codigo} — {grupo.Curso.Nombre}.",
            $"/Cursos/Detalle/{grupo.Id}");

        bitacora.Registrar("Publicar programa", nameof(Grupo), grupo.Id.ToString(), documento.NombreOriginal);

        await contexto.SaveChangesAsync();

        TempData["Exito"] = "El programa del curso quedó publicado.";
        return RedirectToAction(nameof(ListaClase), new { id });
    }

    // =================================================================================
    //  Apoyos privados
    // =================================================================================

    private async Task<Docente?> ObtenerDocenteAsync()
    {
        var usuarioId = gestorUsuarios.GetUserId(User);

        return usuarioId is null
            ? null
            : await contexto.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.UsuarioId == usuarioId);
    }

    /// <summary>Devuelve el grupo solo si pertenece a la persona docente que hizo la petición.</summary>
    private async Task<Grupo?> ObtenerGrupoPropioAsync(int grupoId)
    {
        var docente = await ObtenerDocenteAsync();

        if (docente is null)
        {
            return null;
        }

        return await contexto.Grupos
            .Include(g => g.Curso)
            .Include(g => g.PeriodoAcademico)
            .Include(g => g.Docente)
            .Include(g => g.ProgramaDocumento)
            .FirstOrDefaultAsync(g => g.Id == grupoId && g.DocenteId == docente.Id);
    }

    private async Task<ModeloListaClase> ArmarListaAsync(Grupo grupo)
    {
        var estudiantes = await contexto.DetallesMatricula
            .AsNoTracking()
            .Include(d => d.Matricula!).ThenInclude(m => m.Estudiante)
            .Where(d => d.GrupoId == grupo.Id
                     && d.Estado == EstadoDetalleMatricula.Activo
                     && d.Matricula!.Estado == EstadoMatricula.Confirmada)
            .OrderBy(d => d.Matricula!.Estudiante!.Apellidos)
            .ThenBy(d => d.Matricula!.Estudiante!.Nombre)
            .Select(d => new FilaEstudiante
            {
                DetalleId = d.Id,
                NombreCompleto = d.Matricula!.Estudiante!.Nombre + " " + d.Matricula.Estudiante.Apellidos,
                // Las iniciales salen del nombre y del primer apellido, igual que en la barra
                // superior, para que la misma persona no aparezca con dos abreviaturas distintas.
                Iniciales = (d.Matricula.Estudiante.Nombre.Substring(0, 1)
                             + d.Matricula.Estudiante.Apellidos.Substring(0, 1)).ToUpper(),
                Identificacion = d.Matricula.Estudiante.Identificacion,
                Correo = d.Matricula.Estudiante.Email ?? string.Empty,
                FotografiaDocumentoId = d.Matricula.Estudiante.FotografiaDocumentoId,
                FechaInclusion = d.FechaInclusion,
                NotaFinal = d.NotaFinal,
                FechaRegistroNota = d.FechaRegistroNota
            })
            .ToListAsync();

        return new ModeloListaClase
        {
            Grupo = grupo,
            Estudiantes = estudiantes,
            ActaCerrada = grupo.ActaCerrada,
            PuedeCalificar = !grupo.ActaCerrada && grupo.Estado != EstadoGrupo.Cancelado
        };
    }
}
