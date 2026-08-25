using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Arma el catálogo de cursos disponibles para una persona estudiante: filtra, pagina y marca
/// cada fila con el motivo por el que puede o no matricularse.
///
/// Se separó del servicio de matrícula porque son dos responsabilidades distintas: uno consulta
/// y el otro modifica. Además, la vista Razor y la API de filtrado asíncrono consumen exactamente
/// este método, de modo que el resultado de un filtro por AJAX nunca difiere del de una recarga.
/// </summary>
public class ServicioCatalogo(
    ContextoMatricula contexto,
    ServicioMatricula matricula,
    IOptions<OpcionesMatricula> opciones)
{
    private readonly OpcionesMatricula _opciones = opciones.Value;

    public async Task<ModeloCatalogo> ObtenerCatalogoAsync(string estudianteId, FiltroCatalogo filtro)
    {
        var estudiante = await contexto.Users
            .AsNoTracking()
            .Include(u => u.Carrera)
            .FirstOrDefaultAsync(u => u.Id == estudianteId);

        var periodo = await matricula.ObtenerPeriodoVigenteAsync();

        if (estudiante?.CarreraId is null || periodo is null)
        {
            return new ModeloCatalogo
            {
                Periodo = periodo,
                NombreCarrera = estudiante?.Carrera?.Nombre ?? "Sin carrera asignada",
                Filtro = filtro
            };
        }

        var carreraId = estudiante.CarreraId.Value;

        // Los cursos del plan de la carrera, con el ciclo en el que se ubican.
        var plan = await contexto.CursosCarrera
            .AsNoTracking()
            .Where(cc => cc.CarreraId == carreraId)
            .Select(cc => new { cc.CursoId, cc.Ciclo, cc.EsObligatorio })
            .ToListAsync();

        var cursosDelPlan = plan.Select(p => p.CursoId).ToHashSet();
        var ciclosPorCurso = plan.ToDictionary(p => p.CursoId, p => (p.Ciclo, p.EsObligatorio));

        var consulta = contexto.Grupos
            .AsNoTracking()
            .Include(g => g.Curso)
            .Include(g => g.Docente)
            .Where(g => g.PeriodoAcademicoId == periodo.Id
                     && g.Estado != EstadoGrupo.Cancelado
                     && g.Curso!.Activo
                     && cursosDelPlan.Contains(g.CursoId));

        // -----------------------------------------------------------------------------
        // Filtros. Se aplican sobre la consulta para que el recorte lo haga la base de datos
        // y no la memoria del servidor.
        // -----------------------------------------------------------------------------
        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(g => g.Curso!.Nombre.Contains(texto)
                                        || g.Curso.Codigo.Contains(texto)
                                        || g.Docente!.Apellidos.Contains(texto)
                                        || g.Docente.Nombre.Contains(texto));
        }

        if (filtro.Creditos is { } creditos)
        {
            consulta = consulta.Where(g => g.Curso!.Creditos == creditos);
        }

        if (filtro.Modalidad is { } modalidad)
        {
            consulta = consulta.Where(g => g.Curso!.Modalidad == modalidad);
        }

        if (filtro.Ciclo is { } ciclo)
        {
            var cursosDelCiclo = plan.Where(p => p.Ciclo == ciclo).Select(p => p.CursoId).ToList();
            consulta = consulta.Where(g => cursosDelCiclo.Contains(g.CursoId));
        }

        var filas = await consulta
            .Select(g => new CursoDisponible
            {
                GrupoId = g.Id,
                CursoId = g.CursoId,
                Codigo = g.Curso!.Codigo,
                Nombre = g.Curso.Nombre,
                Descripcion = g.Curso.Descripcion,
                Creditos = g.Curso.Creditos,
                Modalidad = g.Curso.Modalidad,
                NumeroGrupo = g.NumeroGrupo,
                Horario = g.Horario,
                Aula = g.Aula,
                Estado = g.Estado,
                CupoMaximo = g.CupoMaximo,
                Docente = g.Docente == null ? "Sin asignar" : g.Docente.Nombre + " " + g.Docente.Apellidos,
                Inscritos = g.Detalles.Count(d => d.Estado == EstadoDetalleMatricula.Activo
                                               && d.Matricula!.Estado != EstadoMatricula.Anulada)
            })
            .ToListAsync();

        // -----------------------------------------------------------------------------
        // Marcado de cada fila con el estado de la persona: qué tiene aprobado, qué ya
        // agregó a su matrícula y qué requisito le falta.
        // -----------------------------------------------------------------------------
        var aprobados = await matricula.ObtenerCursosAprobadosAsync(estudianteId);

        var borrador = await contexto.Matriculas
            .AsNoTracking()
            .Include(m => m.Detalles).ThenInclude(d => d.Grupo!).ThenInclude(g => g.Curso)
            .FirstOrDefaultAsync(m => m.EstudianteId == estudianteId && m.PeriodoAcademicoId == periodo.Id);

        var detallesActivos = borrador?.Detalles
            .Where(d => d.Estado == EstadoDetalleMatricula.Activo)
            .ToList() ?? [];

        var gruposEnMatricula = detallesActivos.Select(d => d.GrupoId).ToHashSet();
        var cursosEnMatricula = detallesActivos.Select(d => d.Grupo!.CursoId).ToHashSet();
        var creditosEnMatricula = detallesActivos.Sum(d => d.Grupo?.Curso?.Creditos ?? 0);

        var requisitos = await contexto.Requisitos
            .AsNoTracking()
            .Include(r => r.CursoRequisito)
            .Where(r => cursosDelPlan.Contains(r.CursoId))
            .ToListAsync();

        var ventanaAbierta = periodo.AceptaMatricula(DateTime.Now);
        var confirmada = borrador?.Estado == EstadoMatricula.Confirmada;

        foreach (var fila in filas)
        {
            if (ciclosPorCurso.TryGetValue(fila.CursoId, out var ubicacion))
            {
                fila.Ciclo = ubicacion.Ciclo;
                fila.EsObligatorio = ubicacion.EsObligatorio;
            }

            fila.EnMiMatricula = gruposEnMatricula.Contains(fila.GrupoId);
            fila.YaAprobado = aprobados.Contains(fila.CursoId);
            fila.MotivoBloqueo = DeterminarBloqueo(
                fila, aprobados, cursosEnMatricula, requisitos,
                creditosEnMatricula, periodo.MaximoCreditos, ventanaAbierta, confirmada);
        }

        if (filtro.SoloConCupo)
        {
            filas = [.. filas.Where(f => f.HayCupo)];
        }

        if (filtro.SoloHabilitados)
        {
            filas = [.. filas.Where(f => f.SePuedeMatricular)];
        }

        var ordenadas = filas
            .OrderBy(f => f.Ciclo)
            .ThenBy(f => f.Codigo)
            .ThenBy(f => f.NumeroGrupo)
            .ToList();

        var pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;
        var tamano = _opciones.TamanoPagina;
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(ordenadas.Count / (double)tamano));

        if (pagina > totalPaginas)
        {
            pagina = totalPaginas;
        }

        return new ModeloCatalogo
        {
            Periodo = periodo,
            NombreCarrera = estudiante.Carrera?.Nombre ?? "Sin carrera asignada",
            Filtro = filtro,
            CreditosEnMatricula = creditosEnMatricula,
            CursosEnMatricula = detallesActivos.Count,
            MatriculaConfirmada = confirmada,
            CiclosDisponibles = [.. plan.Select(p => p.Ciclo).Distinct().OrderBy(c => c)],
            Resultado = ResultadoPaginado<CursoDisponible>.Crear(
                ordenadas.Skip((pagina - 1) * tamano).Take(tamano),
                pagina, tamano, ordenadas.Count)
        };
    }

    /// <summary>
    /// Traduce el estado de la persona frente a un grupo en el motivo concreto por el que no
    /// puede matricularlo. Devolver nulo significa que la matrícula está habilitada.
    /// El orden importa: se informa primero la razón más importante para quien lee.
    /// </summary>
    private static string? DeterminarBloqueo(
        CursoDisponible fila,
        HashSet<int> aprobados,
        HashSet<int> cursosEnMatricula,
        List<Requisito> requisitos,
        int creditosActuales,
        int topeCreditos,
        bool ventanaAbierta,
        bool matriculaConfirmada)
    {
        if (matriculaConfirmada)
        {
            return "Su matrícula del periodo ya está confirmada.";
        }

        if (!ventanaAbierta)
        {
            return "La ventana de matrícula está cerrada.";
        }

        if (aprobados.Contains(fila.CursoId))
        {
            return "Ya tiene este curso aprobado.";
        }

        if (fila.EnMiMatricula)
        {
            return null;
        }

        if (cursosEnMatricula.Contains(fila.CursoId))
        {
            return "Ya tiene este curso en otro grupo de su matrícula.";
        }

        var faltantes = requisitos
            .Where(r => r.CursoId == fila.CursoId && !aprobados.Contains(r.CursoRequisitoId))
            .Select(r => r.CursoRequisito!.Codigo)
            .ToList();

        if (faltantes.Count > 0)
        {
            return $"Le falta aprobar {string.Join(" y ", faltantes)}.";
        }

        if (fila.Estado != EstadoGrupo.Abierto)
        {
            return "El grupo no está abierto para matrícula.";
        }

        if (!fila.HayCupo)
        {
            return "El grupo no tiene cupo disponible.";
        }

        if (creditosActuales + fila.Creditos > topeCreditos)
        {
            return $"Superaría el tope de {topeCreditos} créditos del periodo.";
        }

        return null;
    }
}
