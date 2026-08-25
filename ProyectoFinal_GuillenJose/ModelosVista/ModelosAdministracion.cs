using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.ModelosVista;

/// <summary>Fila del listado de carreras.</summary>
public class FilaCarrera
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string TituloOtorgado { get; init; } = string.Empty;
    public int CreditosPlan { get; init; }
    public bool Activa { get; init; }
    public int CantidadCursos { get; init; }
    public int CantidadEstudiantes { get; init; }
}

/// <summary>Plan de estudios de una carrera con el formulario para asociar cursos.</summary>
public class ModeloPlanEstudios
{
    public Carrera Carrera { get; init; } = default!;
    public List<CursoCarrera> Plan { get; init; } = [];
    public List<SelectListItem> CursosDisponibles { get; init; } = [];
    public int CantidadEstudiantes { get; init; }

    public int CreditosDelPlan => Plan.Sum(p => p.Curso?.Creditos ?? 0);
    public IEnumerable<IGrouping<int, CursoCarrera>> PorCiclo => Plan.GroupBy(p => p.Ciclo).OrderBy(g => g.Key);
}

/// <summary>Fila del listado de cursos.</summary>
public class FilaCurso
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public int Creditos { get; init; }
    public int HorasSemanales { get; init; }
    public ModalidadCurso Modalidad { get; init; }
    public bool Activo { get; init; }
    public int CantidadCarreras { get; init; }
    public int CantidadGrupos { get; init; }
    public int CantidadRequisitos { get; init; }
}

/// <summary>Ficha de un curso con sus requisitos y las carreras que lo incluyen.</summary>
public class ModeloFichaCurso
{
    public Curso Curso { get; init; } = default!;
    public List<Requisito> Requisitos { get; init; } = [];
    public List<Requisito> EsRequisitoDe { get; init; } = [];
    public List<CursoCarrera> Carreras { get; init; } = [];
    public List<Grupo> Grupos { get; init; } = [];
    public List<SelectListItem> CursosCandidatos { get; init; } = [];
}

/// <summary>Fila del listado de personal docente.</summary>
public class FilaDocente
{
    public int Id { get; init; }
    public string Identificacion { get; init; } = string.Empty;
    public string NombreCompleto { get; init; } = string.Empty;
    public string Especialidad { get; init; } = string.Empty;
    public string CorreoInstitucional { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    public bool Activo { get; init; }
    public bool TieneCuenta { get; init; }
    public int GruposAsignados { get; init; }
}

/// <summary>Fila del listado de periodos académicos.</summary>
public class FilaPeriodo
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    public DateTime InicioMatricula { get; init; }
    public DateTime FinMatricula { get; init; }
    public EstadoPeriodo Estado { get; init; }
    public int MaximoCreditos { get; init; }
    public int CantidadGrupos { get; init; }
    public int MatriculasConfirmadas { get; init; }
}

/// <summary>Fila del listado de grupos.</summary>
public class FilaGrupo
{
    public int Id { get; init; }
    public string CodigoCurso { get; init; } = string.Empty;
    public string NombreCurso { get; init; } = string.Empty;
    public int NumeroGrupo { get; init; }
    public string Periodo { get; init; } = string.Empty;
    public string Docente { get; init; } = "Sin asignar";
    public string Horario { get; init; } = string.Empty;
    public string Aula { get; init; } = string.Empty;
    public int CupoMaximo { get; init; }
    public int Inscritos { get; init; }
    public EstadoGrupo Estado { get; init; }
    public bool ActaCerrada { get; init; }

    public string Etiqueta => $"{CodigoCurso} grupo {NumeroGrupo:00}";
}

/// <summary>Listado de matrículas del periodo para la oficina de registro.</summary>
public class FilaMatricula
{
    public int Id { get; init; }
    public string? NumeroComprobante { get; init; }
    public string Estudiante { get; init; } = string.Empty;
    public string Identificacion { get; init; } = string.Empty;
    public string Carrera { get; init; } = string.Empty;
    public string Periodo { get; init; } = string.Empty;
    public EstadoMatricula Estado { get; init; }
    public int TotalCreditos { get; init; }
    public decimal MontoTotal { get; init; }
    public DateTime? FechaConfirmacion { get; init; }
    public int CantidadCursos { get; init; }
}
