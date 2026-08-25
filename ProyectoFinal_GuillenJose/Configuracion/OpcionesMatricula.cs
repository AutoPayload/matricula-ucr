namespace ProyectoFinal_GuillenJose.Configuracion;

/// <summary>
/// Reglas de negocio de la matrícula que la universidad puede ajustar sin recompilar.
/// Se leen de la sección "Matricula" de appsettings.json.
/// </summary>
public class OpcionesMatricula
{
    public const string Seccion = "Matricula";

    /// <summary>Cantidad mínima de créditos para que una matrícula pueda confirmarse.</summary>
    public int CreditosMinimos { get; set; } = 3;

    /// <summary>Tarifa en colones por crédito matriculado.</summary>
    public decimal CostoPorCredito { get; set; } = 48500m;

    /// <summary>Cargo administrativo fijo que se suma a toda matrícula confirmada.</summary>
    public decimal CargoAdministrativo { get; set; } = 15000m;

    /// <summary>Cantidad de filas por página en los listados paginados.</summary>
    public int TamanoPagina { get; set; } = 8;
}
