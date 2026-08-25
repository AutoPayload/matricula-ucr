using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.ModelosVista;

/// <summary>Ficha completa de un grupo, tal como la ve la persona estudiante.</summary>
public class ModeloDetalleGrupo
{
    public Grupo Grupo { get; init; } = default!;
    public CursoDisponible? Fila { get; init; }
    public int Inscritos { get; init; }
    public List<Curso> Requisitos { get; init; } = [];
    public HashSet<int> CursosAprobados { get; init; } = [];
    public string NombreCarrera { get; init; } = string.Empty;
    public List<Grupo> OtrosGrupos { get; init; } = [];

    public int Disponibles => Math.Max(0, Grupo.CupoMaximo - Inscritos);
}

/// <summary>Pantalla 5 del prototipo: los cursos matriculados del periodo.</summary>
public class ModeloMisCursos
{
    public Matricula? Matricula { get; init; }
    public PeriodoAcademico? Periodo { get; init; }
    public List<DetalleMatricula> Lineas { get; init; } = [];
    public int TotalCreditos { get; init; }
    public decimal MontoEstimado { get; init; }
    public int CreditosMinimos { get; init; }
    public bool VentanaAbierta { get; init; }
    public List<Matricula> Historial { get; init; } = [];

    public bool EsBorrador => Matricula?.Estado == EstadoMatricula.Borrador;
    public bool EstaConfirmada => Matricula?.Estado == EstadoMatricula.Confirmada;
    public bool PuedeConfirmar => EsBorrador && VentanaAbierta && TotalCreditos >= CreditosMinimos;
}

/// <summary>Expediente académico completo, agrupado por periodo.</summary>
public class ModeloExpediente
{
    public string NombreEstudiante { get; init; } = string.Empty;
    public string Identificacion { get; init; } = string.Empty;
    public string NombreCarrera { get; init; } = string.Empty;
    public string EstudianteId { get; init; } = string.Empty;
    public List<BloquePeriodo> Periodos { get; init; } = [];

    public int TotalCreditosAprobados => Periodos.Sum(p => p.CreditosAprobados);
    public int TotalCursos => Periodos.Sum(p => p.Lineas.Count);
}

/// <summary>Bloque del expediente correspondiente a un periodo lectivo.</summary>
public class BloquePeriodo
{
    public string NombrePeriodo { get; init; } = string.Empty;
    public string CodigoPeriodo { get; init; } = string.Empty;
    public EstadoMatricula Estado { get; init; }
    public string? NumeroComprobante { get; init; }
    public int? ComprobanteDocumentoId { get; init; }
    public List<DetalleMatricula> Lineas { get; init; } = [];

    public int CreditosMatriculados => Lineas.Sum(l => l.Grupo?.Curso?.Creditos ?? 0);
    public int CreditosAprobados => Lineas.Where(l => l.Aprobado).Sum(l => l.Grupo?.Curso?.Creditos ?? 0);

    public double Promedio
    {
        get
        {
            var calificadas = Lineas.Where(l => l.NotaFinal.HasValue).ToList();
            var creditos = calificadas.Sum(l => l.Grupo?.Curso?.Creditos ?? 0);

            return creditos == 0
                ? 0
                : Math.Round(calificadas.Sum(l => (l.NotaFinal ?? 0) * (l.Grupo?.Curso?.Creditos ?? 0)) / (double)creditos, 1);
        }
    }
}
