using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Pruebas;

/// <summary>
/// Banco de pruebas: levanta una base de datos SQLite en memoria con un escenario académico
/// mínimo pero completo, de modo que cada prueba trabaje sobre datos propios y no dependa del
/// orden de ejecución ni de la base de SQL Server.
///
/// Se eligió SQLite sobre el proveedor en memoria porque respeta las llaves foráneas y los
/// índices únicos, que es justamente lo que varias de estas reglas están comprobando.
/// </summary>
public sealed class BancoDePruebas : IDisposable
{
    private readonly SqliteConnection _conexion;

    public ContextoMatricula Contexto { get; }
    public ServicioMatricula Matricula { get; }
    public ServicioCatalogo Catalogo { get; }
    public AlmacenamientoFalso Almacen { get; }

    public string EstudianteId { get; } = "estudiante-prueba";
    public int PeriodoAbiertoId { get; private set; }
    public int PeriodoCerradoId { get; private set; }

    /// <summary>Identificadores de los grupos del escenario, por su etiqueta legible.</summary>
    public Dictionary<string, int> Grupos { get; } = [];

    public BancoDePruebas(bool conHistorial = true)
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<ContextoMatricula>()
            .UseSqlite(_conexion)
            .Options;

        Contexto = new ContextoMatricula(opciones);
        Contexto.Database.EnsureCreated();

        // QuestPDF exige declarar la licencia antes de componer cualquier documento.
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        Sembrar(conHistorial);

        var opcionesMatricula = Options.Create(new OpcionesMatricula
        {
            CreditosMinimos = 3,
            CostoPorCredito = 48500m,
            CargoAdministrativo = 15000m,
            TamanoPagina = 8
        });

        var accesor = new HttpContextAccessor();
        var bitacora = new ServicioBitacora(Contexto, accesor);
        var notificaciones = new ServicioNotificaciones(Contexto);
        Almacen = new AlmacenamientoFalso(Contexto);
        var comprobantes = new ServicioComprobantes(Contexto, Almacen);

        Matricula = new ServicioMatricula(Contexto, opcionesMatricula, bitacora, notificaciones, comprobantes);
        Catalogo = new ServicioCatalogo(Contexto, Matricula, opcionesMatricula);
    }

    /// <summary>
    /// Escenario: una carrera con cuatro cursos, uno de ellos con requisito; dos periodos, uno
    /// cerrado con historial y otro con la matrícula abierta; y una persona estudiante que
    /// aprobó un curso y reprobó otro.
    /// </summary>
    private void Sembrar(bool conHistorial)
    {
        var carrera = new Carrera
        {
            Codigo = "IS",
            Nombre = "Ingeniería en Sistemas",
            Descripcion = "Carrera de prueba",
            TituloOtorgado = "Bachillerato en Ingeniería en Sistemas",
            CreditosPlan = 132
        };

        var otraCarrera = new Carrera
        {
            Codigo = "ADE",
            Nombre = "Administración",
            Descripcion = "Carrera ajena al estudiante de prueba",
            TituloOtorgado = "Bachillerato en Administración",
            CreditosPlan = 126
        };

        Contexto.Carreras.AddRange(carrera, otraCarrera);

        var programacionUno = NuevoCurso("SC-101", "Programación I", 4);
        var programacionDos = NuevoCurso("SC-102", "Programación II", 4);
        var matematica = NuevoCurso("MA-101", "Matemática General", 4);
        var redes = NuevoCurso("SC-320", "Redes", 3);
        var contabilidad = NuevoCurso("AD-210", "Contabilidad", 4);

        Contexto.Cursos.AddRange(programacionUno, programacionDos, matematica, redes, contabilidad);
        Contexto.SaveChanges();

        foreach (var curso in new[] { programacionUno, programacionDos, matematica, redes })
        {
            Contexto.CursosCarrera.Add(new CursoCarrera
            {
                CarreraId = carrera.Id,
                CursoId = curso.Id,
                Ciclo = 1,
                EsObligatorio = true
            });
        }

        Contexto.CursosCarrera.Add(new CursoCarrera
        {
            CarreraId = otraCarrera.Id,
            CursoId = contabilidad.Id,
            Ciclo = 1
        });

        // Programación II exige tener aprobada Programación I.
        Contexto.Requisitos.Add(new Requisito
        {
            CursoId = programacionDos.Id,
            CursoRequisitoId = programacionUno.Id,
            NotaMinima = DetalleMatricula.NotaAprobacion
        });

        var docente = new Docente
        {
            Identificacion = "111111111",
            Nombre = "Ana",
            Apellidos = "Rodríguez",
            Especialidad = "Software",
            CorreoInstitucional = "ana@prueba.cr"
        };

        Contexto.Docentes.Add(docente);

        var hoy = DateTime.Today;

        var cerrado = new PeriodoAcademico
        {
            Codigo = "I-2026",
            Nombre = "I Cuatrimestre 2026",
            FechaInicio = hoy.AddDays(-200),
            FechaFin = hoy.AddDays(-100),
            InicioMatricula = hoy.AddDays(-230),
            FinMatricula = hoy.AddDays(-210),
            Estado = EstadoPeriodo.Cerrado,
            MaximoCreditos = 18
        };

        var abierto = new PeriodoAcademico
        {
            Codigo = "II-2026",
            Nombre = "II Cuatrimestre 2026",
            FechaInicio = hoy.AddDays(14),
            FechaFin = hoy.AddDays(110),
            InicioMatricula = hoy.AddDays(-3),
            FinMatricula = hoy.AddDays(20),
            Estado = EstadoPeriodo.MatriculaAbierta,
            MaximoCreditos = 10
        };

        Contexto.PeriodosAcademicos.AddRange(cerrado, abierto);
        Contexto.SaveChanges();

        PeriodoCerradoId = cerrado.Id;
        PeriodoAbiertoId = abierto.Id;

        AgregarGrupo("SC-101 cerrado", programacionUno, docente, cerrado, 1, 30);
        AgregarGrupo("MA-101 cerrado", matematica, docente, cerrado, 1, 30);
        AgregarGrupo("SC-102 abierto", programacionDos, docente, abierto, 1, 30);
        AgregarGrupo("MA-101 abierto", matematica, docente, abierto, 1, 30);
        AgregarGrupo("SC-320 abierto", redes, docente, abierto, 1, 30);
        AgregarGrupo("SC-320 lleno", redes, docente, abierto, 2, 1);
        AgregarGrupo("SC-320 cerrado por cupo", redes, docente, abierto, 3, 30, EstadoGrupo.CerradoPorCupo);
        AgregarGrupo("AD-210 ajeno", contabilidad, docente, abierto, 1, 30);
        AgregarGrupo("SC-101 pasado", programacionUno, docente, cerrado, 2, 30);
        AgregarGrupo("SC-101 abierto", programacionUno, docente, abierto, 1, 30);

        Contexto.SaveChanges();

        var estudiante = new Usuario
        {
            Id = EstudianteId,
            UserName = "estudiante@prueba.cr",
            Email = "estudiante@prueba.cr",
            Identificacion = "118330875",
            Nombre = "José",
            Apellidos = "Guillén",
            CarreraId = carrera.Id
        };

        var otroEstudiante = new Usuario
        {
            Id = "otro-estudiante",
            UserName = "otro@prueba.cr",
            Email = "otro@prueba.cr",
            Identificacion = "222222222",
            Nombre = "María",
            Apellidos = "Solís",
            CarreraId = carrera.Id
        };

        Contexto.Users.AddRange(estudiante, otroEstudiante);
        Contexto.SaveChanges();

        if (!conHistorial)
        {
            return;
        }

        // Historial: aprobó Programación I con 90 y reprobó Matemática con 55.
        var historica = new Matricula
        {
            EstudianteId = EstudianteId,
            PeriodoAcademicoId = cerrado.Id,
            Estado = EstadoMatricula.Confirmada,
            FechaConfirmacion = hoy.AddDays(-220),
            TotalCreditos = 8,
            NumeroComprobante = "MAT-I2026-000001"
        };

        Contexto.Matriculas.Add(historica);
        Contexto.SaveChanges();

        Contexto.DetallesMatricula.AddRange(
            new DetalleMatricula
            {
                MatriculaId = historica.Id,
                GrupoId = Grupos["SC-101 cerrado"],
                NotaFinal = 90,
                FechaRegistroNota = hoy.AddDays(-105)
            },
            new DetalleMatricula
            {
                MatriculaId = historica.Id,
                GrupoId = Grupos["MA-101 cerrado"],
                NotaFinal = 55,
                FechaRegistroNota = hoy.AddDays(-105)
            });

        // La otra persona ya ocupó el único espacio del grupo lleno.
        var ajena = new Matricula
        {
            EstudianteId = "otro-estudiante",
            PeriodoAcademicoId = abierto.Id,
            Estado = EstadoMatricula.Confirmada,
            FechaConfirmacion = hoy.AddDays(-1),
            TotalCreditos = 3,
            NumeroComprobante = "MAT-II2026-000002"
        };

        Contexto.Matriculas.Add(ajena);
        Contexto.SaveChanges();

        Contexto.DetallesMatricula.Add(new DetalleMatricula
        {
            MatriculaId = ajena.Id,
            GrupoId = Grupos["SC-320 lleno"]
        });

        Contexto.SaveChanges();
    }

    private static Curso NuevoCurso(string codigo, string nombre, int creditos) => new()
    {
        Codigo = codigo,
        Nombre = nombre,
        Descripcion = $"Curso de prueba {codigo}",
        Creditos = creditos,
        HorasSemanales = 4,
        Modalidad = ModalidadCurso.Presencial
    };

    private void AgregarGrupo(string etiqueta, Curso curso, Docente docente, PeriodoAcademico periodo,
                              int numero, int cupo, EstadoGrupo estado = EstadoGrupo.Abierto)
    {
        var grupo = new Grupo
        {
            CursoId = curso.Id,
            DocenteId = docente.Id,
            PeriodoAcademicoId = periodo.Id,
            NumeroGrupo = numero,
            Horario = "Lunes 18:00 a 20:30",
            Aula = "AULA-101",
            CupoMaximo = cupo,
            Estado = estado
        };

        Contexto.Grupos.Add(grupo);
        Contexto.SaveChanges();

        Grupos[etiqueta] = grupo.Id;
    }

    public void Dispose()
    {
        Contexto.Dispose();
        _conexion.Dispose();
    }
}

/// <summary>
/// Sustituto del almacén de archivos para las pruebas: conserva el contenido en memoria y no
/// toca el disco, pero sí registra el documento en la base para que las llaves foráneas del
/// comprobante y del acta se comporten igual que en producción.
/// </summary>
public class AlmacenamientoFalso(ContextoMatricula contexto) : IAlmacenamientoArchivos
{
    private readonly Dictionary<int, byte[]> _contenidos = [];

    public int CantidadGuardada => _contenidos.Count;

    public Task<Documento> GuardarAsync(IFormFile archivo, CategoriaDocumento categoria, string? propietarioUsuarioId) =>
        GuardarContenidoAsync([1, 2, 3], archivo.FileName, archivo.ContentType, categoria, propietarioUsuarioId);

    public async Task<Documento> GuardarContenidoAsync(byte[] contenido, string nombreOriginal, string tipoContenido,
                                                       CategoriaDocumento categoria, string? propietarioUsuarioId)
    {
        var documento = new Documento
        {
            NombreOriginal = nombreOriginal,
            NombreAlmacenado = Guid.NewGuid().ToString("N"),
            TipoContenido = tipoContenido,
            TamanoBytes = contenido.LongLength,
            HashSha256 = new string('0', 64),
            Categoria = categoria,
            PropietarioUsuarioId = propietarioUsuarioId
        };

        contexto.Documentos.Add(documento);
        await contexto.SaveChangesAsync();

        _contenidos[documento.Id] = contenido;
        return documento;
    }

    public Task<byte[]> LeerAsync(Documento documento) =>
        Task.FromResult(_contenidos.TryGetValue(documento.Id, out var contenido) ? contenido : []);

    public void Eliminar(Documento documento) => _contenidos.Remove(documento.Id);

    public string? Validar(IFormFile archivo, CategoriaDocumento categoria) => null;
}
