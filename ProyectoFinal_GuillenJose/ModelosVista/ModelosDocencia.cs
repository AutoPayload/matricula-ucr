using System.ComponentModel.DataAnnotations;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.ModelosVista;

/// <summary>Listado de los grupos que imparte una persona docente en un periodo.</summary>
public class ModeloMisGrupos
{
    public string NombreDocente { get; init; } = string.Empty;
    public string Especialidad { get; init; } = string.Empty;
    public PeriodoAcademico? Periodo { get; init; }
    public List<PeriodoAcademico> Periodos { get; init; } = [];
    public List<ResumenGrupo> Grupos { get; init; } = [];

    public int TotalEstudiantes => Grupos.Sum(g => g.Inscritos);
    public int ActasPendientes => Grupos.Count(g => !g.ActaCerrada);
}

/// <summary>Fila del listado de grupos con el avance de la calificación.</summary>
public class ResumenGrupo
{
    public int GrupoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string NombreCurso { get; init; } = string.Empty;
    public int NumeroGrupo { get; init; }
    public string Horario { get; init; } = string.Empty;
    public string Aula { get; init; } = string.Empty;
    public int Creditos { get; init; }
    public int Inscritos { get; init; }
    public int CupoMaximo { get; init; }
    public int NotasRegistradas { get; init; }
    public bool ActaCerrada { get; init; }
    public bool TienePrograma { get; init; }
    public EstadoGrupo Estado { get; init; }

    public string Etiqueta => $"{Codigo} grupo {NumeroGrupo:00}";
    public bool CalificacionCompleta => Inscritos > 0 && NotasRegistradas == Inscritos;
}

/// <summary>Lista de clase con el formulario de registro de notas.</summary>
public class ModeloListaClase
{
    public Grupo Grupo { get; init; } = default!;
    public List<FilaEstudiante> Estudiantes { get; init; } = [];
    public bool ActaCerrada { get; init; }
    public bool PuedeCalificar { get; init; }

    public int NotasRegistradas => Estudiantes.Count(e => e.NotaFinal.HasValue);
    public int Aprobados => Estudiantes.Count(e => e.NotaFinal >= DetalleMatricula.NotaAprobacion);
    public int Reprobados => Estudiantes.Count(e => e.NotaFinal.HasValue && e.NotaFinal < DetalleMatricula.NotaAprobacion);

    public double Promedio
    {
        get
        {
            var conNota = Estudiantes.Where(e => e.NotaFinal.HasValue).ToList();
            return conNota.Count == 0 ? 0 : Math.Round(conNota.Average(e => e.NotaFinal!.Value), 1);
        }
    }
}

/// <summary>Una persona matriculada en el grupo, con su nota editable.</summary>
public class FilaEstudiante
{
    public int DetalleId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public string Iniciales { get; set; } = "??";
    public string Correo { get; set; } = string.Empty;
    public int? FotografiaDocumentoId { get; set; }
    public DateTime FechaInclusion { get; set; }

    [Range(0, 100, ErrorMessage = "La nota debe estar entre 0 y 100.")]
    [Display(Name = "Nota final")]
    public int? NotaFinal { get; set; }

    public DateTime? FechaRegistroNota { get; set; }
}

/// <summary>Datos que llegan del formulario de calificación.</summary>
public class ModeloRegistroNotas
{
    public int GrupoId { get; set; }
    public List<FilaEstudiante> Estudiantes { get; set; } = [];
}
