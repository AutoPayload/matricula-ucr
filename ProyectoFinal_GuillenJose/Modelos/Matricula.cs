using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Cabecera de la transacción de matrícula: una por estudiante y periodo. Nace en estado
/// borrador cuando la persona agrega el primer grupo y se sella al confirmar, momento en el
/// que se emite el número de comprobante y se congela el total de créditos.
/// </summary>
public class Matricula
{
    public int Id { get; set; }

    /// <summary>Consecutivo visible para la persona usuaria, con el formato MAT-II2026-000014.</summary>
    [StringLength(30)]
    [Display(Name = "Comprobante")]
    public string? NumeroComprobante { get; set; }

    [Required]
    [Display(Name = "Estudiante")]
    public string EstudianteId { get; set; } = string.Empty;
    public Usuario? Estudiante { get; set; }

    [Display(Name = "Periodo")]
    public int PeriodoAcademicoId { get; set; }
    public PeriodoAcademico? PeriodoAcademico { get; set; }

    [Display(Name = "Estado")]
    public EstadoMatricula Estado { get; set; } = EstadoMatricula.Borrador;

    [Display(Name = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [Display(Name = "Fecha de confirmación")]
    public DateTime? FechaConfirmacion { get; set; }

    [Display(Name = "Total de créditos")]
    public int TotalCreditos { get; set; }

    /// <summary>Costo del periodo. Se calcula al confirmar con la tarifa vigente por crédito.</summary>
    [DataType(DataType.Currency)]
    [Display(Name = "Monto total")]
    public decimal MontoTotal { get; set; }

    /// <summary>Comprobante en PDF generado al confirmar la matrícula.</summary>
    [Display(Name = "Comprobante en PDF")]
    public int? ComprobanteDocumentoId { get; set; }
    public Documento? ComprobanteDocumento { get; set; }

    public ICollection<DetalleMatricula> Detalles { get; set; } = [];
}
