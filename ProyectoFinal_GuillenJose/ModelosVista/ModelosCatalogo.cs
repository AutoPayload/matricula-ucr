using System.ComponentModel.DataAnnotations;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.ModelosVista;

/// <summary>
/// Criterios con los que la persona estudiante recorta el catálogo. Los mismos valores viajan
/// por la cadena de consulta en la vista Razor y por la API que atiende el filtrado asíncrono.
/// </summary>
public class FiltroCatalogo
{
    [Display(Name = "Buscar")]
    public string? Texto { get; set; }

    [Display(Name = "Créditos")]
    public int? Creditos { get; set; }

    [Display(Name = "Modalidad")]
    public ModalidadCurso? Modalidad { get; set; }

    [Display(Name = "Ciclo del plan")]
    public int? Ciclo { get; set; }

    [Display(Name = "Solo con cupo")]
    public bool SoloConCupo { get; set; }

    [Display(Name = "Solo los que puedo matricular")]
    public bool SoloHabilitados { get; set; }

    public int Pagina { get; set; } = 1;

    /// <summary>Convierte el filtro en pares nombre/valor para conservarlo en los enlaces.</summary>
    public Dictionary<string, string?> ComoParametros() => new()
    {
        ["Texto"] = Texto,
        ["Creditos"] = Creditos?.ToString(),
        ["Modalidad"] = Modalidad?.ToString(),
        ["Ciclo"] = Ciclo?.ToString(),
        ["SoloConCupo"] = SoloConCupo ? "true" : null,
        ["SoloHabilitados"] = SoloHabilitados ? "true" : null
    };

    public bool HayFiltroActivo =>
        !string.IsNullOrWhiteSpace(Texto) || Creditos.HasValue || Modalidad.HasValue
        || Ciclo.HasValue || SoloConCupo || SoloHabilitados;
}

/// <summary>
/// Una fila del catálogo: el grupo con toda la información que la persona necesita para decidir,
/// incluido el motivo por el que no puede matricularlo cuando ese es el caso.
/// </summary>
public class CursoDisponible
{
    public int GrupoId { get; init; }
    public int CursoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public int Creditos { get; init; }
    public ModalidadCurso Modalidad { get; init; }
    public int Ciclo { get; set; }
    public bool EsObligatorio { get; set; }
    public int NumeroGrupo { get; init; }
    public string Horario { get; init; } = string.Empty;
    public string Aula { get; init; } = string.Empty;
    public string Docente { get; init; } = "Sin asignar";
    public int Inscritos { get; init; }
    public int CupoMaximo { get; init; }
    public EstadoGrupo Estado { get; init; }

    /// <summary>Verdadero cuando el grupo ya forma parte de la matrícula en proceso.</summary>
    public bool EnMiMatricula { get; set; }

    /// <summary>Verdadero cuando el curso ya está aprobado en el historial.</summary>
    public bool YaAprobado { get; set; }

    /// <summary>Motivo por el que el botón de matrícula queda deshabilitado; nulo si está libre.</summary>
    public string? MotivoBloqueo { get; set; }

    public int Disponibles => Math.Max(0, CupoMaximo - Inscritos);
    public bool HayCupo => Disponibles > 0;
    public bool SePuedeMatricular => MotivoBloqueo is null && !EnMiMatricula;
    public string Etiqueta => $"{Codigo} grupo {NumeroGrupo:00}";
}

/// <summary>Todo lo que necesita la pantalla de cursos disponibles.</summary>
public class ModeloCatalogo
{
    public PeriodoAcademico? Periodo { get; init; }
    public string NombreCarrera { get; init; } = string.Empty;
    public FiltroCatalogo Filtro { get; init; } = new();
    public ResultadoPaginado<CursoDisponible> Resultado { get; init; } = new();
    public int CreditosEnMatricula { get; init; }
    public int CursosEnMatricula { get; init; }
    public bool MatriculaConfirmada { get; init; }
    public List<int> CiclosDisponibles { get; init; } = [];
}
