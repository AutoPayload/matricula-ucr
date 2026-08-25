using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Datos;

/// <summary>
/// Carga los datos con los que arranca el sistema: roles, cuentas de demostración, oferta
/// académica y un historial de matrícula que da sentido a los promedios y a los requisitos.
///
/// La ventana de matrícula del periodo vigente se calcula a partir de la fecha de siembra en
/// lugar de fijarse en el calendario, de modo que la demostración funcione el día que se
/// ejecute el proyecto y no solo la semana en que se escribió.
/// </summary>
public static class SembradorDatos
{
    public const string CorreoAdministracion = "registro@matriculaucr.cr";
    public const string ClaveAdministracion = "Admin2026*";
    public const string ClaveDocencia = "Docente2026*";
    public const string ClaveEstudiantado = "Estudiante2026*";

    public static async Task SembrarAsync(IServiceProvider proveedor)
    {
        var contexto = proveedor.GetRequiredService<ContextoMatricula>();
        var gestorRoles = proveedor.GetRequiredService<RoleManager<IdentityRole>>();
        var gestorUsuarios = proveedor.GetRequiredService<UserManager<Usuario>>();

        await SembrarRolesAsync(gestorRoles);

        // Si la oferta académica ya existe se respeta lo que haya en la base y solo se garantiza
        // que la cuenta de administración siga disponible.
        if (await contexto.Carreras.AnyAsync())
        {
            await AsegurarAdministracionAsync(gestorUsuarios, contexto);
            return;
        }

        var carreras = await SembrarCarrerasAsync(contexto);
        var cursos = await SembrarCursosAsync(contexto);
        await SembrarPlanesEstudioAsync(contexto, carreras, cursos);
        await SembrarRequisitosAsync(contexto, cursos);

        var docentes = await SembrarDocentesAsync(contexto, gestorUsuarios);
        var periodos = await SembrarPeriodosAsync(contexto);
        var grupos = await SembrarGruposAsync(contexto, cursos, docentes, periodos);

        await AsegurarAdministracionAsync(gestorUsuarios, contexto);
        var estudiantes = await SembrarEstudiantesAsync(gestorUsuarios, carreras);
        await SembrarHistorialAsync(contexto, estudiantes, periodos, grupos);
    }

    // =================================================================================
    //  Seguridad
    // =================================================================================

    private static async Task SembrarRolesAsync(RoleManager<IdentityRole> gestorRoles)
    {
        foreach (var rol in RolesSistema.Todos)
        {
            if (!await gestorRoles.RoleExistsAsync(rol))
            {
                await gestorRoles.CreateAsync(new IdentityRole(rol));
            }
        }
    }

    private static async Task AsegurarAdministracionAsync(
        UserManager<Usuario> gestorUsuarios, ContextoMatricula contexto)
    {
        var administrador = await gestorUsuarios.FindByEmailAsync(CorreoAdministracion);

        if (administrador is null)
        {
            administrador = new Usuario
            {
                UserName = CorreoAdministracion,
                Email = CorreoAdministracion,
                EmailConfirmed = true,
                Identificacion = "101110111",
                Nombre = "Oficina",
                Apellidos = "de Registro",
                FechaRegistro = DateTime.Now
            };

            var resultado = await gestorUsuarios.CreateAsync(administrador, ClaveAdministracion);

            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo crear la cuenta de administración: " +
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
            }
        }

        if (!await gestorUsuarios.IsInRoleAsync(administrador, RolesSistema.Administrador))
        {
            await gestorUsuarios.AddToRoleAsync(administrador, RolesSistema.Administrador);
        }

        await contexto.SaveChangesAsync();
    }

    // =================================================================================
    //  Oferta académica
    // =================================================================================

    private static async Task<Dictionary<string, Carrera>> SembrarCarrerasAsync(ContextoMatricula contexto)
    {
        var carreras = new List<Carrera>
        {
            new()
            {
                Codigo = "IS",
                Nombre = "Ingeniería en Sistemas de Información",
                Descripcion = "Formación en desarrollo de software, bases de datos, redes y " +
                              "arquitectura de soluciones para la industria tecnológica.",
                TituloOtorgado = "Bachillerato en Ingeniería en Sistemas de Información",
                CreditosPlan = 132
            },
            new()
            {
                Codigo = "IIN",
                Nombre = "Ingeniería Industrial",
                Descripcion = "Optimización de procesos productivos, gestión de la calidad y " +
                              "seguridad ocupacional en organizaciones de manufactura y servicios.",
                TituloOtorgado = "Bachillerato en Ingeniería Industrial",
                CreditosPlan = 138
            },
            new()
            {
                Codigo = "ADE",
                Nombre = "Administración de Empresas",
                Descripcion = "Dirección de organizaciones, contabilidad, mercadeo y " +
                              "emprendimiento con enfoque en la pequeña y mediana empresa.",
                TituloOtorgado = "Bachillerato en Administración de Empresas",
                CreditosPlan = 126
            }
        };

        contexto.Carreras.AddRange(carreras);
        await contexto.SaveChangesAsync();

        return carreras.ToDictionary(c => c.Codigo);
    }

    private static async Task<Dictionary<string, Curso>> SembrarCursosAsync(ContextoMatricula contexto)
    {
        // Código, nombre, créditos, horas, modalidad, descripción.
        var definiciones = new (string Codigo, string Nombre, int Creditos, int Horas, ModalidadCurso Modalidad, string Descripcion)[]
        {
            ("MA-101", "Matemática General", 4, 5, ModalidadCurso.Presencial,
                "Álgebra, funciones, trigonometría y fundamentos de razonamiento cuantitativo."),
            ("MA-201", "Cálculo Diferencial e Integral", 4, 5, ModalidadCurso.Presencial,
                "Límites, derivadas, integrales y sus aplicaciones a problemas de ingeniería."),
            ("CO-101", "Comunicación Escrita", 3, 3, ModalidadCurso.Bimodal,
                "Redacción de documentos técnicos, normativa ortográfica y citación bibliográfica."),
            ("IN-101", "Inglés Técnico I", 2, 3, ModalidadCurso.Virtual,
                "Comprensión de lectura y vocabulario técnico aplicado a la especialidad."),
            ("ES-201", "Estadística Aplicada", 4, 4, ModalidadCurso.Presencial,
                "Estadística descriptiva, probabilidad, muestreo y prueba de hipótesis."),
            ("SC-101", "Programación I", 4, 5, ModalidadCurso.Presencial,
                "Lógica de programación, estructuras de control y funciones con lenguaje C#."),
            ("SC-102", "Programación II", 4, 5, ModalidadCurso.Presencial,
                "Programación orientada a objetos, colecciones y manejo de excepciones."),
            ("SC-205", "Bases de Datos", 4, 4, ModalidadCurso.Presencial,
                "Modelo relacional, normalización, lenguaje SQL y transacciones."),
            ("SC-301", "Programación Web Avanzada", 4, 4, ModalidadCurso.Bimodal,
                "Aplicaciones web con ASP.NET Core, patrón modelo vista controlador y servicios REST."),
            ("SC-310", "Arquitectura de Software", 3, 3, ModalidadCurso.Bimodal,
                "Patrones de diseño, capas, calidad del software y decisiones arquitectónicas."),
            ("SC-320", "Redes y Comunicaciones", 3, 3, ModalidadCurso.Presencial,
                "Modelo de capas, protocolos, direccionamiento y seguridad perimetral."),
            ("SC-410", "Inteligencia Artificial", 4, 4, ModalidadCurso.Virtual,
                "Aprendizaje automático, evaluación de modelos y ética en la automatización."),
            ("II-210", "Investigación de Operaciones", 4, 4, ModalidadCurso.Presencial,
                "Programación lineal, redes, teoría de colas y modelos de decisión."),
            ("II-305", "Gestión de la Calidad", 3, 3, ModalidadCurso.Bimodal,
                "Normas internacionales, control estadístico de procesos y mejora continua."),
            ("II-320", "Ergonomía y Seguridad Laboral", 3, 3, ModalidadCurso.Presencial,
                "Análisis de puestos, prevención de riesgos y legislación laboral vigente."),
            ("AD-101", "Fundamentos de Administración", 3, 3, ModalidadCurso.Presencial,
                "Planificación, organización, dirección y control de las organizaciones."),
            ("AD-210", "Contabilidad Financiera", 4, 4, ModalidadCurso.Presencial,
                "Registro contable, estados financieros y análisis de razones."),
            ("AD-330", "Mercadeo Estratégico", 3, 3, ModalidadCurso.Bimodal,
                "Segmentación, posicionamiento, mezcla de mercadeo y analítica comercial.")
        };

        var cursos = definiciones.Select(d => new Curso
        {
            Codigo = d.Codigo,
            Nombre = d.Nombre,
            Creditos = d.Creditos,
            HorasSemanales = d.Horas,
            Modalidad = d.Modalidad,
            Descripcion = d.Descripcion
        }).ToList();

        contexto.Cursos.AddRange(cursos);
        await contexto.SaveChangesAsync();

        return cursos.ToDictionary(c => c.Codigo);
    }

    private static async Task SembrarPlanesEstudioAsync(
        ContextoMatricula contexto, Dictionary<string, Carrera> carreras, Dictionary<string, Curso> cursos)
    {
        var planes = new Dictionary<string, (string Codigo, int Ciclo, bool Obligatorio)[]>
        {
            ["IS"] =
            [
                ("MA-101", 1, true), ("CO-101", 1, true), ("SC-101", 1, true),
                ("IN-101", 2, true), ("SC-102", 2, true),
                ("MA-201", 3, true), ("SC-205", 3, true),
                ("ES-201", 4, true), ("SC-320", 4, true),
                ("SC-301", 5, true), ("SC-310", 5, false),
                ("SC-410", 6, false)
            ],
            ["IIN"] =
            [
                ("MA-101", 1, true), ("CO-101", 1, true),
                ("IN-101", 2, true), ("MA-201", 2, true),
                ("ES-201", 3, true),
                ("II-210", 4, true),
                ("II-305", 5, true), ("II-320", 5, false)
            ],
            ["ADE"] =
            [
                ("CO-101", 1, true), ("AD-101", 1, true),
                ("MA-101", 2, true), ("IN-101", 2, true),
                ("AD-210", 3, true),
                ("ES-201", 4, true),
                ("AD-330", 5, true)
            ]
        };

        foreach (var (codigoCarrera, detalle) in planes)
        {
            foreach (var (codigoCurso, ciclo, obligatorio) in detalle)
            {
                contexto.CursosCarrera.Add(new CursoCarrera
                {
                    CarreraId = carreras[codigoCarrera].Id,
                    CursoId = cursos[codigoCurso].Id,
                    Ciclo = ciclo,
                    EsObligatorio = obligatorio
                });
            }
        }

        await contexto.SaveChangesAsync();
    }

    private static async Task SembrarRequisitosAsync(ContextoMatricula contexto, Dictionary<string, Curso> cursos)
    {
        var cadena = new (string Curso, string Requisito)[]
        {
            ("MA-201", "MA-101"),
            ("ES-201", "MA-101"),
            ("SC-102", "SC-101"),
            ("SC-205", "SC-102"),
            ("SC-301", "SC-205"),
            ("SC-310", "SC-205"),
            ("SC-410", "ES-201"),
            ("II-210", "MA-201"),
            ("AD-330", "AD-101")
        };

        foreach (var (curso, requisito) in cadena)
        {
            contexto.Requisitos.Add(new Requisito
            {
                CursoId = cursos[curso].Id,
                CursoRequisitoId = cursos[requisito].Id,
                NotaMinima = DetalleMatricula.NotaAprobacion
            });
        }

        await contexto.SaveChangesAsync();
    }

    // =================================================================================
    //  Personal docente
    // =================================================================================

    private static async Task<Dictionary<string, Docente>> SembrarDocentesAsync(
        ContextoMatricula contexto, UserManager<Usuario> gestorUsuarios)
    {
        var definiciones = new (string Identificacion, string Nombre, string Apellidos, string Especialidad, string Correo, bool ConCuenta)[]
        {
            ("108450321", "Ana", "Rodríguez Solano", "Desarrollo de Software", "ana.rodriguez@matriculaucr.cr", true),
            ("204670198", "Luis", "Vargas Mena", "Bases de Datos y Analítica", "luis.vargas@matriculaucr.cr", true),
            ("301220887", "Carlos", "Mora Jiménez", "Matemática Aplicada", "carlos.mora@matriculaucr.cr", false),
            ("112780654", "Silvia", "Castro Ramírez", "Estadística e Inteligencia Artificial", "silvia.castro@matriculaucr.cr", false),
            ("206340775", "Marta", "Fernández Ruiz", "Gestión Empresarial", "marta.fernandez@matriculaucr.cr", false),
            ("115980342", "Andrés", "Jiménez Soto", "Redes y Telecomunicaciones", "andres.jimenez@matriculaucr.cr", false)
        };

        var docentes = new Dictionary<string, Docente>();

        foreach (var definicion in definiciones)
        {
            var docente = new Docente
            {
                Identificacion = definicion.Identificacion,
                Nombre = definicion.Nombre,
                Apellidos = definicion.Apellidos,
                Especialidad = definicion.Especialidad,
                CorreoInstitucional = definicion.Correo
            };

            if (definicion.ConCuenta)
            {
                var cuenta = new Usuario
                {
                    UserName = definicion.Correo,
                    Email = definicion.Correo,
                    EmailConfirmed = true,
                    Identificacion = definicion.Identificacion,
                    Nombre = definicion.Nombre,
                    Apellidos = definicion.Apellidos,
                    FechaRegistro = DateTime.Now
                };

                var resultado = await gestorUsuarios.CreateAsync(cuenta, ClaveDocencia);

                if (resultado.Succeeded)
                {
                    await gestorUsuarios.AddToRoleAsync(cuenta, RolesSistema.Docente);
                    docente.UsuarioId = cuenta.Id;
                }
            }

            contexto.Docentes.Add(docente);
            docentes[definicion.Correo] = docente;
        }

        await contexto.SaveChangesAsync();

        return docentes;
    }

    // =================================================================================
    //  Calendario académico y oferta de grupos
    // =================================================================================

    private static async Task<Dictionary<string, PeriodoAcademico>> SembrarPeriodosAsync(ContextoMatricula contexto)
    {
        var hoy = DateTime.Today;

        var periodos = new List<PeriodoAcademico>
        {
            new()
            {
                Codigo = "I-2026",
                Nombre = "I Cuatrimestre 2026",
                FechaInicio = new DateTime(2026, 1, 12),
                FechaFin = new DateTime(2026, 4, 24),
                InicioMatricula = new DateTime(2025, 12, 1),
                FinMatricula = new DateTime(2026, 1, 9),
                Estado = EstadoPeriodo.Cerrado,
                MaximoCreditos = 18
            },
            new()
            {
                Codigo = "II-2026",
                Nombre = "II Cuatrimestre 2026",
                FechaInicio = new DateTime(2026, 5, 11),
                FechaFin = new DateTime(2026, 8, 21),
                InicioMatricula = new DateTime(2026, 4, 20),
                FinMatricula = new DateTime(2026, 5, 8),
                Estado = EstadoPeriodo.Cerrado,
                MaximoCreditos = 18
            },
            new()
            {
                Codigo = "III-2026",
                Nombre = "III Cuatrimestre 2026",
                FechaInicio = hoy.AddDays(14),
                FechaFin = hoy.AddDays(112),
                // La ventana se ancla a la fecha de siembra para que la demostración
                // encuentre siempre la matrícula abierta.
                InicioMatricula = hoy.AddDays(-7),
                FinMatricula = hoy.AddDays(21),
                Estado = EstadoPeriodo.MatriculaAbierta,
                MaximoCreditos = 18
            }
        };

        contexto.PeriodosAcademicos.AddRange(periodos);
        await contexto.SaveChangesAsync();

        return periodos.ToDictionary(p => p.Codigo);
    }

    private static async Task<Dictionary<string, Grupo>> SembrarGruposAsync(
        ContextoMatricula contexto,
        Dictionary<string, Curso> cursos,
        Dictionary<string, Docente> docentes,
        Dictionary<string, PeriodoAcademico> periodos)
    {
        var ana = docentes["ana.rodriguez@matriculaucr.cr"];
        var luis = docentes["luis.vargas@matriculaucr.cr"];
        var carlos = docentes["carlos.mora@matriculaucr.cr"];
        var silvia = docentes["silvia.castro@matriculaucr.cr"];
        var marta = docentes["marta.fernandez@matriculaucr.cr"];
        var andres = docentes["andres.jimenez@matriculaucr.cr"];

        // Periodo, curso, grupo, docente, horario, aula, cupo.
        var definiciones = new (string Periodo, string Curso, int Numero, Docente Docente, string Horario, string Aula, int Cupo)[]
        {
            // Historial: I Cuatrimestre 2026.
            ("I-2026", "SC-101", 1, ana, "Lunes y miércoles 18:00 a 20:30", "LAB-201", 30),
            ("I-2026", "MA-101", 1, carlos, "Martes y jueves 18:00 a 20:30", "AULA-104", 40),
            ("I-2026", "CO-101", 1, marta, "Sábado 08:00 a 11:00", "AULA-210", 35),
            ("I-2026", "AD-101", 1, marta, "Lunes y miércoles 18:00 a 20:00", "AULA-301", 35),

            // Historial: II Cuatrimestre 2026.
            ("II-2026", "SC-102", 1, ana, "Lunes y miércoles 18:00 a 20:30", "LAB-201", 30),
            ("II-2026", "IN-101", 1, marta, "Virtual, jueves 19:00 a 21:00", "VIRTUAL", 45),
            ("II-2026", "MA-201", 1, carlos, "Martes y jueves 18:00 a 20:30", "AULA-104", 35),
            ("II-2026", "MA-101", 1, carlos, "Sábado 08:00 a 11:00", "AULA-104", 40),
            ("II-2026", "AD-210", 1, marta, "Martes y jueves 18:00 a 20:00", "AULA-301", 30),

            // Oferta vigente: III Cuatrimestre 2026.
            ("III-2026", "SC-205", 1, luis, "Lunes y miércoles 18:00 a 20:30", "LAB-202", 25),
            ("III-2026", "SC-205", 2, luis, "Sábado 08:00 a 12:00", "LAB-202", 25),
            ("III-2026", "SC-301", 1, ana, "Martes y jueves 18:00 a 20:30", "LAB-201", 22),
            ("III-2026", "SC-310", 1, ana, "Viernes 18:00 a 21:00", "AULA-205", 20),
            ("III-2026", "SC-320", 1, andres, "Martes y jueves 18:00 a 20:00", "LAB-105", 24),
            // Cupo deliberadamente pequeño: sirve para mostrar el rechazo por falta de espacio.
            ("III-2026", "SC-320", 2, andres, "Sábado 13:00 a 17:00", "LAB-105", 1),
            ("III-2026", "SC-410", 1, silvia, "Virtual, miércoles 19:00 a 21:30", "VIRTUAL", 30),
            ("III-2026", "ES-201", 1, silvia, "Lunes y miércoles 18:00 a 20:00", "AULA-108", 30),
            ("III-2026", "MA-201", 1, carlos, "Martes y jueves 18:00 a 20:30", "AULA-104", 35),
            ("III-2026", "MA-101", 1, carlos, "Sábado 08:00 a 11:00", "AULA-104", 40),
            ("III-2026", "CO-101", 1, marta, "Virtual, martes 19:00 a 21:00", "VIRTUAL", 40),
            ("III-2026", "IN-101", 1, marta, "Virtual, jueves 19:00 a 21:00", "VIRTUAL", 45),
            ("III-2026", "II-210", 1, carlos, "Lunes y miércoles 18:00 a 20:30", "AULA-112", 28),
            ("III-2026", "II-305", 1, silvia, "Sábado 08:00 a 12:00", "AULA-112", 25),
            ("III-2026", "II-320", 1, andres, "Viernes 18:00 a 21:00", "AULA-112", 25),
            ("III-2026", "AD-210", 1, marta, "Martes y jueves 18:00 a 20:00", "AULA-301", 30),
            ("III-2026", "AD-330", 1, marta, "Lunes y miércoles 18:00 a 20:00", "AULA-301", 30),
            ("III-2026", "AD-101", 1, marta, "Sábado 13:00 a 16:00", "AULA-301", 35)
        };

        var grupos = new Dictionary<string, Grupo>();

        foreach (var definicion in definiciones)
        {
            var grupo = new Grupo
            {
                CursoId = cursos[definicion.Curso].Id,
                DocenteId = definicion.Docente.Id,
                PeriodoAcademicoId = periodos[definicion.Periodo].Id,
                NumeroGrupo = definicion.Numero,
                Horario = definicion.Horario,
                Aula = definicion.Aula,
                CupoMaximo = definicion.Cupo,
                Estado = EstadoGrupo.Abierto
            };

            contexto.Grupos.Add(grupo);
            grupos[$"{definicion.Periodo}|{definicion.Curso}|{definicion.Numero}"] = grupo;
        }

        await contexto.SaveChangesAsync();

        return grupos;
    }

    // =================================================================================
    //  Estudiantado y su historial
    // =================================================================================

    private static async Task<Dictionary<string, Usuario>> SembrarEstudiantesAsync(
        UserManager<Usuario> gestorUsuarios, Dictionary<string, Carrera> carreras)
    {
        var definiciones = new (string Correo, string Identificacion, string Nombre, string Apellidos, string Carrera, DateTime Nacimiento)[]
        {
            ("jose.guillen@matriculaucr.cr", "118330875", "José Andrés", "Guillén Agüero", "IS", new DateTime(2001, 3, 18)),
            ("maria.solis@matriculaucr.cr", "402310588", "María Fernanda", "Solís Bonilla", "IS", new DateTime(2002, 7, 4)),
            ("kevin.rojas@matriculaucr.cr", "304450912", "Kevin", "Rojas Castillo", "IS", new DateTime(2000, 11, 27)),
            ("diego.arias@matriculaucr.cr", "115670233", "Diego", "Arias Naranjo", "IIN", new DateTime(2001, 1, 9)),
            ("laura.mendez@matriculaucr.cr", "207890456", "Laura", "Méndez Quirós", "ADE", new DateTime(2003, 5, 22))
        };

        var estudiantes = new Dictionary<string, Usuario>();

        foreach (var definicion in definiciones)
        {
            var cuenta = new Usuario
            {
                UserName = definicion.Correo,
                Email = definicion.Correo,
                EmailConfirmed = true,
                Identificacion = definicion.Identificacion,
                Nombre = definicion.Nombre,
                Apellidos = definicion.Apellidos,
                FechaNacimiento = definicion.Nacimiento,
                CarreraId = carreras[definicion.Carrera].Id,
                FechaRegistro = DateTime.Now.AddMonths(-8)
            };

            var resultado = await gestorUsuarios.CreateAsync(cuenta, ClaveEstudiantado);

            if (resultado.Succeeded)
            {
                await gestorUsuarios.AddToRoleAsync(cuenta, RolesSistema.Estudiante);
                estudiantes[definicion.Correo] = cuenta;
            }
        }

        return estudiantes;
    }

    /// <summary>
    /// Crea matrículas confirmadas de los dos periodos cerrados, con notas ya registradas.
    /// Sin este historial no habría promedio que mostrar ni requisitos que verificar, y la
    /// regla de prerrequisitos no podría demostrarse.
    /// </summary>
    private static async Task SembrarHistorialAsync(
        ContextoMatricula contexto,
        Dictionary<string, Usuario> estudiantes,
        Dictionary<string, PeriodoAcademico> periodos,
        Dictionary<string, Grupo> grupos)
    {
        // Correo, periodo, y las notas de cada grupo cursado.
        var historial = new (string Correo, string Periodo, (string Curso, int Numero, int Nota)[] Lineas)[]
        {
            ("jose.guillen@matriculaucr.cr", "I-2026",
                [("SC-101", 1, 92), ("MA-101", 1, 85), ("CO-101", 1, 88)]),
            ("jose.guillen@matriculaucr.cr", "II-2026",
                [("SC-102", 1, 90), ("IN-101", 1, 95), ("MA-201", 1, 78)]),

            // María reprobó Matemática General: su caso permite ver el bloqueo por requisitos.
            ("maria.solis@matriculaucr.cr", "I-2026",
                [("SC-101", 1, 76), ("MA-101", 1, 58), ("CO-101", 1, 81)]),
            ("maria.solis@matriculaucr.cr", "II-2026",
                [("SC-102", 1, 84), ("IN-101", 1, 89)]),

            ("kevin.rojas@matriculaucr.cr", "I-2026",
                [("SC-101", 1, 71), ("MA-101", 1, 74)]),
            ("kevin.rojas@matriculaucr.cr", "II-2026",
                [("SC-102", 1, 80), ("MA-201", 1, 70)]),

            ("diego.arias@matriculaucr.cr", "I-2026",
                [("MA-101", 1, 83), ("CO-101", 1, 79)]),
            ("diego.arias@matriculaucr.cr", "II-2026",
                [("MA-201", 1, 87), ("IN-101", 1, 91)]),

            ("laura.mendez@matriculaucr.cr", "I-2026",
                [("AD-101", 1, 94), ("CO-101", 1, 90)]),
            ("laura.mendez@matriculaucr.cr", "II-2026",
                [("AD-210", 1, 86), ("MA-101", 1, 77)])
        };

        foreach (var (correo, codigoPeriodo, lineas) in historial)
        {
            if (!estudiantes.TryGetValue(correo, out var estudiante))
            {
                continue;
            }

            var periodo = periodos[codigoPeriodo];

            var matricula = new Matricula
            {
                EstudianteId = estudiante.Id,
                PeriodoAcademicoId = periodo.Id,
                Estado = EstadoMatricula.Confirmada,
                FechaCreacion = periodo.InicioMatricula,
                FechaConfirmacion = periodo.InicioMatricula.AddDays(2),
                MontoTotal = 0
            };

            contexto.Matriculas.Add(matricula);
            await contexto.SaveChangesAsync();

            var creditos = 0;

            foreach (var (curso, numero, nota) in lineas)
            {
                var grupo = grupos[$"{codigoPeriodo}|{curso}|{numero}"];
                var creditosCurso = await contexto.Cursos
                    .Where(c => c.Id == grupo.CursoId)
                    .Select(c => c.Creditos)
                    .FirstAsync();

                creditos += creditosCurso;

                contexto.DetallesMatricula.Add(new DetalleMatricula
                {
                    MatriculaId = matricula.Id,
                    GrupoId = grupo.Id,
                    FechaInclusion = matricula.FechaCreacion,
                    Estado = EstadoDetalleMatricula.Activo,
                    NotaFinal = nota,
                    FechaRegistroNota = periodo.FechaFin
                });
            }

            matricula.TotalCreditos = creditos;
            matricula.MontoTotal = (creditos * 48500m) + 15000m;
            matricula.NumeroComprobante = $"MAT-{codigoPeriodo.Replace("-", string.Empty)}-{matricula.Id:000000}";

            await contexto.SaveChangesAsync();
        }

        // Los grupos de los periodos cerrados quedan con el acta sellada.
        foreach (var grupo in await contexto.Grupos
                     .Where(g => g.PeriodoAcademico!.Estado == EstadoPeriodo.Cerrado)
                     .ToListAsync())
        {
            grupo.ActaCerrada = true;
            grupo.FechaCierreActa = grupo.PeriodoAcademico!.FechaFin.AddDays(5);
        }

        // Kevin ya ocupó el único espacio del grupo pequeño de Redes: al intentar matricularlo
        // otra persona, el sistema responde que no hay cupo.
        if (estudiantes.TryGetValue("kevin.rojas@matriculaucr.cr", out var kevin))
        {
            var vigente = periodos["III-2026"];
            var grupoLleno = grupos["III-2026|SC-320|2"];

            var matriculaVigente = new Matricula
            {
                EstudianteId = kevin.Id,
                PeriodoAcademicoId = vigente.Id,
                Estado = EstadoMatricula.Confirmada,
                FechaCreacion = DateTime.Now.AddDays(-3),
                FechaConfirmacion = DateTime.Now.AddDays(-3),
                TotalCreditos = 3,
                MontoTotal = (3 * 48500m) + 15000m
            };

            contexto.Matriculas.Add(matriculaVigente);
            await contexto.SaveChangesAsync();

            contexto.DetallesMatricula.Add(new DetalleMatricula
            {
                MatriculaId = matriculaVigente.Id,
                GrupoId = grupoLleno.Id,
                FechaInclusion = matriculaVigente.FechaCreacion,
                Estado = EstadoDetalleMatricula.Activo
            });

            matriculaVigente.NumeroComprobante = $"MAT-III2026-{matriculaVigente.Id:000000}";
            await contexto.SaveChangesAsync();
        }

        // Aviso de apertura para todo el estudiantado.
        foreach (var estudiante in estudiantes.Values)
        {
            contexto.Notificaciones.Add(new Notificacion
            {
                UsuarioId = estudiante.Id,
                Titulo = "Matrícula abierta",
                Mensaje = "La matrícula del III Cuatrimestre 2026 ya está disponible. " +
                          "Revise la oferta de cursos de su carrera y confirme antes del cierre.",
                Enlace = "/Cursos/Disponibles",
                FechaCreacion = DateTime.Now.AddDays(-7)
            });
        }

        await contexto.SaveChangesAsync();
    }
}
