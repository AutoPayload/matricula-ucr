using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Reúne los indicadores del panel de administración. Se aísla del controlador porque las mismas
/// cifras las pide la vista al cargar y el cliente asíncrono cada vez que refresca el panel.
/// </summary>
public class ServicioEstadisticas(ContextoMatricula contexto)
{
    /// <summary>Arma el tablero completo para el periodo indicado.</summary>
    public async Task<TableroAdministrativo> ObtenerTableroAsync(int periodoId)
    {
        var periodo = await contexto.PeriodosAcademicos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == periodoId);

        var matriculasConfirmadas = await contexto.Matriculas
            .CountAsync(m => m.PeriodoAcademicoId == periodoId && m.Estado == EstadoMatricula.Confirmada);

        var matriculasEnProceso = await contexto.Matriculas
            .CountAsync(m => m.PeriodoAcademicoId == periodoId && m.Estado == EstadoMatricula.Borrador);

        var creditosTotales = await contexto.Matriculas
            .Where(m => m.PeriodoAcademicoId == periodoId && m.Estado == EstadoMatricula.Confirmada)
            .SumAsync(m => (int?)m.TotalCreditos) ?? 0;

        var ingresos = await contexto.Matriculas
            .Where(m => m.PeriodoAcademicoId == periodoId && m.Estado == EstadoMatricula.Confirmada)
            .SumAsync(m => (decimal?)m.MontoTotal) ?? 0m;

        var estudiantesActivos = await contexto.Users.CountAsync(u => u.Activo && u.CarreraId != null);

        return new TableroAdministrativo
        {
            PeriodoId = periodoId,
            PeriodoNombre = periodo?.Nombre ?? "Sin periodo",
            PeriodoEstado = periodo?.Estado.Describir() ?? "—",
            MatriculasConfirmadas = matriculasConfirmadas,
            MatriculasEnProceso = matriculasEnProceso,
            CreditosTotales = creditosTotales,
            IngresoProyectado = ingresos,
            EstudiantesActivos = estudiantesActivos,
            CarrerasActivas = await contexto.Carreras.CountAsync(c => c.Activa),
            CursosActivos = await contexto.Cursos.CountAsync(c => c.Activo),
            DocentesActivos = await contexto.Docentes.CountAsync(d => d.Activo),
            GruposAbiertos = await contexto.Grupos
                .CountAsync(g => g.PeriodoAcademicoId == periodoId && g.Estado == EstadoGrupo.Abierto),
            MatriculaPorCarrera = await ObtenerMatriculaPorCarreraAsync(periodoId),
            OcupacionPorGrupo = await ObtenerOcupacionAsync(periodoId)
        };
    }

    /// <summary>Cantidad de personas matriculadas en cada carrera durante el periodo.</summary>
    public async Task<List<SerieValor>> ObtenerMatriculaPorCarreraAsync(int periodoId)
    {
        var datos = await contexto.Matriculas
            .AsNoTracking()
            .Where(m => m.PeriodoAcademicoId == periodoId && m.Estado == EstadoMatricula.Confirmada)
            .GroupBy(m => m.Estudiante!.Carrera!.Nombre)
            .Select(grupo => new SerieValor
            {
                Etiqueta = grupo.Key ?? "Sin carrera",
                Valor = grupo.Count()
            })
            .ToListAsync();

        return [.. datos.OrderByDescending(d => d.Valor)];
    }

    /// <summary>Ocupación de cada grupo del periodo, ordenada de la más llena a la más vacía.</summary>
    public async Task<List<OcupacionGrupo>> ObtenerOcupacionAsync(int periodoId, int cantidad = 10)
    {
        var datos = await contexto.Grupos
            .AsNoTracking()
            .Include(g => g.Curso)
            .Where(g => g.PeriodoAcademicoId == periodoId && g.Estado != EstadoGrupo.Cancelado)
            .Select(g => new OcupacionGrupo
            {
                GrupoId = g.Id,
                Etiqueta = g.Curso!.Codigo + " grupo " + g.NumeroGrupo,
                NombreCurso = g.Curso.Nombre,
                CupoMaximo = g.CupoMaximo,
                Inscritos = g.Detalles.Count(d => d.Estado == EstadoDetalleMatricula.Activo
                                               && d.Matricula!.Estado != EstadoMatricula.Anulada)
            })
            .ToListAsync();

        return [.. datos.OrderByDescending(d => d.PorcentajeOcupacion).ThenBy(d => d.Etiqueta).Take(cantidad)];
    }
}
