using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Reglas de negocio de la matrícula. Toda la lógica que decide si una persona puede o no
/// inscribir un grupo vive aquí y no en los controladores, por dos razones: la misma regla la
/// consultan la vista Razor y la API que atiende las peticiones asíncronas, y así las reglas
/// pueden probarse de forma automatizada sin levantar el servidor web.
///
/// Las reglas que se aplican al agregar un grupo son, en este orden: ventana de matrícula
/// abierta, grupo disponible, carrera asignada, curso perteneciente al plan de estudios, curso
/// no aprobado antes, curso no repetido en el periodo, requisitos aprobados, tope de créditos
/// del periodo y cupo libre en el grupo.
/// </summary>
public class ServicioMatricula(
    ContextoMatricula contexto,
    IOptions<OpcionesMatricula> opciones,
    ServicioBitacora bitacora,
    ServicioNotificaciones notificaciones,
    ServicioComprobantes comprobantes)
{
    private readonly OpcionesMatricula _opciones = opciones.Value;

    // =================================================================================
    //  Consultas de apoyo
    // =================================================================================

    /// <summary>
    /// Periodo sobre el que trabaja el sistema: el que tiene la matrícula abierta y, si no hay
    /// ninguno, el más reciente para que las pantallas de consulta sigan mostrando información.
    /// </summary>
    public async Task<PeriodoAcademico?> ObtenerPeriodoVigenteAsync()
    {
        var abierto = await contexto.PeriodosAcademicos
            .Where(p => p.Estado == EstadoPeriodo.MatriculaAbierta)
            .OrderByDescending(p => p.FechaInicio)
            .FirstOrDefaultAsync();

        return abierto ?? await contexto.PeriodosAcademicos
            .OrderByDescending(p => p.FechaInicio)
            .FirstOrDefaultAsync();
    }

    /// <summary>Cantidad de personas inscritas en un grupo, sin contar matrículas anuladas.</summary>
    public async Task<int> ContarInscritosAsync(int grupoId) =>
        await contexto.DetallesMatricula
            .CountAsync(d => d.GrupoId == grupoId
                          && d.Estado == EstadoDetalleMatricula.Activo
                          && d.Matricula!.Estado != EstadoMatricula.Anulada);

    /// <summary>
    /// Devuelve la matrícula en borrador de la persona para el periodo, y la crea si aún no existe.
    /// </summary>
    public async Task<Matricula> ObtenerOCrearBorradorAsync(string estudianteId, int periodoId)
    {
        var matricula = await contexto.Matriculas
            .Include(m => m.Detalles).ThenInclude(d => d.Grupo!).ThenInclude(g => g.Curso)
            .Include(m => m.Detalles).ThenInclude(d => d.Grupo!).ThenInclude(g => g.Docente)
            .FirstOrDefaultAsync(m => m.EstudianteId == estudianteId && m.PeriodoAcademicoId == periodoId);

        if (matricula is not null)
        {
            return matricula;
        }

        matricula = new Matricula
        {
            EstudianteId = estudianteId,
            PeriodoAcademicoId = periodoId,
            Estado = EstadoMatricula.Borrador,
            FechaCreacion = DateTime.Now
        };

        contexto.Matriculas.Add(matricula);
        await contexto.SaveChangesAsync();

        return matricula;
    }

    /// <summary>
    /// Códigos de los cursos que la persona tiene aprobados, es decir con nota registrada
    /// mayor o igual a la nota de aprobación en una matrícula confirmada.
    /// </summary>
    public async Task<HashSet<int>> ObtenerCursosAprobadosAsync(string estudianteId) =>
        [.. await contexto.DetallesMatricula
            .Where(d => d.Matricula!.EstudianteId == estudianteId
                     && d.Matricula.Estado == EstadoMatricula.Confirmada
                     && d.Estado == EstadoDetalleMatricula.Activo
                     && d.NotaFinal != null
                     && d.NotaFinal >= DetalleMatricula.NotaAprobacion)
            .Select(d => d.Grupo!.CursoId)
            .Distinct()
            .ToListAsync()];

    // =================================================================================
    //  Movimientos sobre el borrador
    // =================================================================================

    /// <summary>
    /// Agrega un grupo a la matrícula en borrador aplicando las reglas del reglamento.
    /// Cuando alguna no se cumple devuelve el motivo en español, listo para mostrarse.
    /// </summary>
    public async Task<ResultadoOperacion> AgregarGrupoAsync(string estudianteId, int grupoId)
    {
        var grupo = await contexto.Grupos
            .Include(g => g.Curso)
            .Include(g => g.PeriodoAcademico)
            .FirstOrDefaultAsync(g => g.Id == grupoId);

        if (grupo?.Curso is null || grupo.PeriodoAcademico is null)
        {
            return ResultadoOperacion.Fallido("El grupo indicado no existe.");
        }

        // Regla 1: la ventana de matrícula debe estar abierta.
        if (!grupo.PeriodoAcademico.AceptaMatricula(DateTime.Now))
        {
            return ResultadoOperacion.Fallido(
                $"El periodo {grupo.PeriodoAcademico.Nombre} no tiene la matrícula abierta en este momento.");
        }

        // Regla 2: el grupo debe estar disponible.
        if (grupo.Estado != EstadoGrupo.Abierto)
        {
            return ResultadoOperacion.Fallido("El grupo no está abierto para matrícula.");
        }

        var estudiante = await contexto.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == estudianteId);

        if (estudiante is null)
        {
            return ResultadoOperacion.Fallido("No se encontró la cuenta de la persona estudiante.");
        }

        // Regla 3: el curso debe pertenecer al plan de estudios de su carrera.
        if (estudiante.CarreraId is null)
        {
            return ResultadoOperacion.Fallido(
                "Su cuenta no tiene una carrera asignada. Comuníquese con la oficina de registro.");
        }

        var perteneceAlPlan = await contexto.CursosCarrera
            .AnyAsync(cc => cc.CarreraId == estudiante.CarreraId && cc.CursoId == grupo.CursoId);

        if (!perteneceAlPlan)
        {
            return ResultadoOperacion.Fallido(
                $"El curso {grupo.Curso.Codigo} no forma parte del plan de estudios de su carrera.");
        }

        var matricula = await ObtenerOCrearBorradorAsync(estudianteId, grupo.PeriodoAcademicoId);

        if (matricula.Estado == EstadoMatricula.Confirmada)
        {
            return ResultadoOperacion.Fallido(
                "Su matrícula del periodo ya fue confirmada. Solicite un movimiento en la oficina de registro.");
        }

        // Regla 4: un curso ya aprobado no se vuelve a llevar. Se consulta antes que el resto
        // porque es el rechazo que más desconcierta si se informa tarde.
        var aprobadosPrevios = await ObtenerCursosAprobadosAsync(estudianteId);

        if (aprobadosPrevios.Contains(grupo.CursoId))
        {
            return ResultadoOperacion.Fallido(
                $"Ya tiene aprobado el curso {grupo.Curso.Codigo} y no necesita volver a llevarlo.");
        }

        // Regla 5: no se repite el mismo curso, ni siquiera en otro grupo.
        var yaTieneElCurso = await contexto.DetallesMatricula
            .AnyAsync(d => d.MatriculaId == matricula.Id
                        && d.Estado == EstadoDetalleMatricula.Activo
                        && d.Grupo!.CursoId == grupo.CursoId);

        if (yaTieneElCurso)
        {
            return ResultadoOperacion.Fallido(
                $"Ya tiene el curso {grupo.Curso.Codigo} en su matrícula de este periodo.");
        }

        // Regla 6: los requisitos deben estar aprobados.
        var requisitos = await contexto.Requisitos
            .Include(r => r.CursoRequisito)
            .Where(r => r.CursoId == grupo.CursoId)
            .ToListAsync();

        if (requisitos.Count > 0)
        {
            var faltantes = requisitos
                .Where(r => !aprobadosPrevios.Contains(r.CursoRequisitoId))
                .Select(r => r.CursoRequisito!.Codigo)
                .ToList();

            if (faltantes.Count > 0)
            {
                return ResultadoOperacion.Fallido(
                    $"Le falta aprobar el requisito {string.Join(", ", faltantes)} para llevar {grupo.Curso.Codigo}.");
            }
        }

        // Regla 7: el tope de créditos del periodo.
        var creditosActuales = await ContarCreditosAsync(matricula.Id);
        var topeDelPeriodo = grupo.PeriodoAcademico.MaximoCreditos;

        if (creditosActuales + grupo.Curso.Creditos > topeDelPeriodo)
        {
            return ResultadoOperacion.Fallido(
                $"Con este curso llegaría a {creditosActuales + grupo.Curso.Creditos} créditos y el " +
                $"tope del periodo es de {topeDelPeriodo}.");
        }

        // Regla 8: debe quedar cupo. Se verifica de último porque es la más costosa.
        var inscritos = await ContarInscritosAsync(grupo.Id);

        if (inscritos >= grupo.CupoMaximo)
        {
            return ResultadoOperacion.Fallido(
                $"El grupo {grupo.NumeroGrupo:00} de {grupo.Curso.Codigo} ya no tiene cupo disponible.");
        }

        contexto.DetallesMatricula.Add(new DetalleMatricula
        {
            MatriculaId = matricula.Id,
            GrupoId = grupo.Id,
            FechaInclusion = DateTime.Now,
            Estado = EstadoDetalleMatricula.Activo
        });

        bitacora.Registrar("Agregar curso", nameof(Matricula), matricula.Id.ToString(),
            $"Grupo {grupo.Etiqueta} agregado al borrador.");

        await contexto.SaveChangesAsync();

        return ResultadoOperacion.Correcto(
            $"Se agregó {grupo.Curso.Codigo} — {grupo.Curso.Nombre} a su matrícula.", matricula.Id);
    }

    /// <summary>Quita un grupo del borrador. Una matrícula confirmada no admite este movimiento.</summary>
    public async Task<ResultadoOperacion> QuitarGrupoAsync(string estudianteId, int detalleId)
    {
        var detalle = await contexto.DetallesMatricula
            .Include(d => d.Matricula)
            .Include(d => d.Grupo!).ThenInclude(g => g.Curso)
            .FirstOrDefaultAsync(d => d.Id == detalleId);

        if (detalle?.Matricula is null || detalle.Matricula.EstudianteId != estudianteId)
        {
            return ResultadoOperacion.Fallido("El curso indicado no pertenece a su matrícula.");
        }

        if (detalle.Matricula.Estado != EstadoMatricula.Borrador)
        {
            return ResultadoOperacion.Fallido(
                "La matrícula ya fue confirmada y no se puede modificar desde el portal.");
        }

        var codigo = detalle.Grupo?.Curso?.Codigo ?? "el curso";
        contexto.DetallesMatricula.Remove(detalle);

        bitacora.Registrar("Quitar curso", nameof(Matricula), detalle.MatriculaId.ToString(),
            $"Se retiró {codigo} del borrador.");

        await contexto.SaveChangesAsync();

        return ResultadoOperacion.Correcto($"Se quitó {codigo} de su matrícula.", detalle.MatriculaId);
    }

    /// <summary>Suma de créditos activos de una matrícula.</summary>
    public async Task<int> ContarCreditosAsync(int matriculaId) =>
        await contexto.DetallesMatricula
            .Where(d => d.MatriculaId == matriculaId && d.Estado == EstadoDetalleMatricula.Activo)
            .SumAsync(d => (int?)d.Grupo!.Curso!.Creditos) ?? 0;

    // =================================================================================
    //  Confirmación
    // =================================================================================

    /// <summary>
    /// Sella la matrícula: valida de nuevo cupo y ventana, calcula el monto, emite el número de
    /// comprobante y genera el PDF. Todo ocurre dentro de una transacción explícita porque entre
    /// la última verificación de cupo y el guardado puede colarse otra persona.
    ///
    /// La transacción se ejecuta a través de la estrategia de reintentos del proveedor. El
    /// contexto está configurado con reintentos ante fallas transitorias, y Entity Framework Core
    /// exige que en ese caso las transacciones abiertas a mano se envuelvan de esta forma para
    /// que el bloque completo pueda repetirse como una unidad.
    /// </summary>
    public async Task<ResultadoOperacion> ConfirmarAsync(string estudianteId, int periodoId)
    {
        var matricula = await contexto.Matriculas
            .Include(m => m.PeriodoAcademico)
            .Include(m => m.Estudiante)
            .Include(m => m.Detalles).ThenInclude(d => d.Grupo!).ThenInclude(g => g.Curso)
            .Include(m => m.Detalles).ThenInclude(d => d.Grupo!).ThenInclude(g => g.Docente)
            .FirstOrDefaultAsync(m => m.EstudianteId == estudianteId && m.PeriodoAcademicoId == periodoId);

        if (matricula is null)
        {
            return ResultadoOperacion.Fallido("No hay una matrícula en proceso para este periodo.");
        }

        if (matricula.Estado == EstadoMatricula.Confirmada)
        {
            return ResultadoOperacion.Fallido("Esta matrícula ya estaba confirmada.");
        }

        if (matricula.PeriodoAcademico is null || !matricula.PeriodoAcademico.AceptaMatricula(DateTime.Now))
        {
            return ResultadoOperacion.Fallido("La ventana de matrícula del periodo está cerrada.");
        }

        var activos = matricula.Detalles.Where(d => d.Estado == EstadoDetalleMatricula.Activo).ToList();
        var totalCreditos = activos.Sum(d => d.Grupo?.Curso?.Creditos ?? 0);

        if (totalCreditos < _opciones.CreditosMinimos)
        {
            return ResultadoOperacion.Fallido(
                $"Debe matricular al menos {_opciones.CreditosMinimos} créditos y lleva {totalCreditos}.");
        }

        var estrategia = contexto.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
            await SellarAsync(matricula, activos, totalCreditos, estudianteId));
    }

    /// <summary>
    /// Cuerpo transaccional de la confirmación. Se separó del método público para que la
    /// estrategia de reintentos pueda repetirlo entero si la conexión falla a mitad de camino.
    /// </summary>
    private async Task<ResultadoOperacion> SellarAsync(
        Matricula matricula, List<DetalleMatricula> activos, int totalCreditos, string estudianteId)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync();

        // Última verificación de cupo, ahora que nadie más puede colarse dentro de la transacción.
        foreach (var detalle in activos)
        {
            var inscritos = await contexto.DetallesMatricula
                .CountAsync(d => d.GrupoId == detalle.GrupoId
                              && d.Id != detalle.Id
                              && d.Estado == EstadoDetalleMatricula.Activo
                              && d.Matricula!.Estado == EstadoMatricula.Confirmada);

            if (inscritos >= (detalle.Grupo?.CupoMaximo ?? 0))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Fallido(
                    $"El grupo de {detalle.Grupo?.Curso?.Codigo} se llenó mientras armaba su matrícula. " +
                    "Quítelo y elija otro grupo.");
            }
        }

        matricula.Estado = EstadoMatricula.Confirmada;
        matricula.FechaConfirmacion = DateTime.Now;
        matricula.TotalCreditos = totalCreditos;
        matricula.MontoTotal = (totalCreditos * _opciones.CostoPorCredito) + _opciones.CargoAdministrativo;
        matricula.NumeroComprobante = ComponerComprobante(matricula);

        await contexto.SaveChangesAsync();

        // El comprobante se genera con la matrícula ya sellada para que refleje el número final.
        var documento = await comprobantes.GenerarComprobanteMatriculaAsync(matricula);
        matricula.ComprobanteDocumentoId = documento.Id;

        notificaciones.Emitir(
            estudianteId,
            "Matrícula confirmada",
            $"Su matrícula del periodo {matricula.PeriodoAcademico.Nombre} quedó confirmada con " +
            $"{totalCreditos} créditos. El comprobante {matricula.NumeroComprobante} está disponible.",
            "/Matricula/MisCursos");

        bitacora.Registrar("Confirmar matrícula", nameof(Matricula), matricula.Id.ToString(),
            $"Comprobante {matricula.NumeroComprobante} con {totalCreditos} créditos.");

        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();

        return ResultadoOperacion.Correcto(
            $"Matrícula confirmada. Su comprobante es {matricula.NumeroComprobante}.", matricula.Id);
    }

    /// <summary>
    /// Anula una matrícula confirmada. Es una operación de la oficina de registro, no del portal
    /// del estudiantado, y libera de inmediato el cupo de los grupos involucrados.
    /// </summary>
    public async Task<ResultadoOperacion> AnularAsync(int matriculaId, string motivo)
    {
        var matricula = await contexto.Matriculas
            .Include(m => m.Detalles)
            .FirstOrDefaultAsync(m => m.Id == matriculaId);

        if (matricula is null)
        {
            return ResultadoOperacion.Fallido("La matrícula indicada no existe.");
        }

        if (matricula.Estado == EstadoMatricula.Anulada)
        {
            return ResultadoOperacion.Fallido("La matrícula ya estaba anulada.");
        }

        matricula.Estado = EstadoMatricula.Anulada;

        foreach (var detalle in matricula.Detalles)
        {
            detalle.Estado = EstadoDetalleMatricula.Retirado;
        }

        notificaciones.Emitir(
            matricula.EstudianteId,
            "Matrícula anulada",
            $"Su matrícula {matricula.NumeroComprobante ?? "en proceso"} fue anulada. Motivo: {motivo}");

        bitacora.Registrar("Anular matrícula", nameof(Matricula), matricula.Id.ToString(), motivo);

        await contexto.SaveChangesAsync();

        return ResultadoOperacion.Correcto("La matrícula fue anulada y los cupos quedaron liberados.");
    }

    /// <summary>
    /// Compone el número de comprobante con el formato MAT-II2026-000014. El identificador de la
    /// matrícula ya está asignado en este punto, así que el consecutivo nunca se repite.
    /// </summary>
    private static string ComponerComprobante(Matricula matricula)
    {
        var periodo = (matricula.PeriodoAcademico?.Codigo ?? "PER").Replace("-", string.Empty);
        return $"MAT-{periodo}-{matricula.Id:000000}";
    }
}
