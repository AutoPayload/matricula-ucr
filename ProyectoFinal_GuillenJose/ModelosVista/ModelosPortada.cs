using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.ModelosVista;

/// <summary>Cifras y periodo que se muestran en la portada del sitio.</summary>
public class ModeloPortada
{
    public PeriodoAcademico? PeriodoVigente { get; init; }
    public int TotalCarreras { get; init; }
    public int TotalCursos { get; init; }
    public int TotalDocentes { get; init; }
    public int TotalGrupos { get; init; }
}

/// <summary>Datos de la página que atiende los códigos de estado del servidor.</summary>
public class ModeloCodigoEstado
{
    public int Codigo { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string Explicacion { get; init; } = string.Empty;
}

/// <summary>Identificador de la solicitud fallida, para rastrearla en la bitácora del servidor.</summary>
public class ModeloError
{
    public string? IdentificadorSolicitud { get; init; }
    public bool MostrarIdentificador => !string.IsNullOrEmpty(IdentificadorSolicitud);
}
