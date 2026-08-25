namespace ProyectoFinal_GuillenJose.ModelosVista;

/// <summary>Indicadores que alimentan el panel de la oficina de registro.</summary>
public class TableroAdministrativo
{
    public int PeriodoId { get; init; }
    public string PeriodoNombre { get; init; } = string.Empty;
    public string PeriodoEstado { get; init; } = string.Empty;

    public int MatriculasConfirmadas { get; init; }
    public int MatriculasEnProceso { get; init; }
    public int CreditosTotales { get; init; }
    public decimal IngresoProyectado { get; init; }

    public int EstudiantesActivos { get; init; }
    public int CarrerasActivas { get; init; }
    public int CursosActivos { get; init; }
    public int DocentesActivos { get; init; }
    public int GruposAbiertos { get; init; }

    public List<SerieValor> MatriculaPorCarrera { get; init; } = [];
    public List<OcupacionGrupo> OcupacionPorGrupo { get; init; } = [];

    /// <summary>Promedio de créditos por matrícula confirmada.</summary>
    public double PromedioCreditos => MatriculasConfirmadas == 0
        ? 0
        : Math.Round(CreditosTotales / (double)MatriculasConfirmadas, 1);
}

/// <summary>Par etiqueta y valor para las barras del panel.</summary>
public class SerieValor
{
    public string Etiqueta { get; init; } = string.Empty;
    public int Valor { get; init; }
}

/// <summary>Ocupación de un grupo respecto de su cupo máximo.</summary>
public class OcupacionGrupo
{
    public int GrupoId { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public string NombreCurso { get; init; } = string.Empty;
    public int CupoMaximo { get; init; }
    public int Inscritos { get; init; }

    public int Disponibles => Math.Max(0, CupoMaximo - Inscritos);

    public int PorcentajeOcupacion => CupoMaximo == 0
        ? 0
        : (int)Math.Round(Inscritos * 100d / CupoMaximo);
}
