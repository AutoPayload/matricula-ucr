using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>Forma en que se imparte un curso.</summary>
public enum ModalidadCurso
{
    [Display(Name = "Presencial")] Presencial = 1,
    [Display(Name = "Virtual")] Virtual = 2,
    [Display(Name = "Bimodal")] Bimodal = 3
}

/// <summary>
/// Situación de un periodo lectivo. Determina si la matrícula acepta movimientos:
/// solo MatriculaAbierta permite confirmar una matrícula nueva.
/// </summary>
public enum EstadoPeriodo
{
    [Display(Name = "Planificado")] Planificado = 1,
    [Display(Name = "Matrícula abierta")] MatriculaAbierta = 2,
    [Display(Name = "Lecciones en curso")] EnCurso = 3,
    [Display(Name = "Cerrado")] Cerrado = 4
}

/// <summary>Situación de un grupo dentro de la oferta del periodo.</summary>
public enum EstadoGrupo
{
    [Display(Name = "Abierto")] Abierto = 1,
    [Display(Name = "Cerrado por cupo")] CerradoPorCupo = 2,
    [Display(Name = "Cancelado")] Cancelado = 3
}

/// <summary>
/// Situación de la transacción de matrícula. Mientras esté en borrador el estudiante
/// puede agregar y quitar grupos sin consecuencias académicas.
/// </summary>
public enum EstadoMatricula
{
    [Display(Name = "Borrador")] Borrador = 1,
    [Display(Name = "Confirmada")] Confirmada = 2,
    [Display(Name = "Anulada")] Anulada = 3
}

/// <summary>Situación de una línea de matrícula.</summary>
public enum EstadoDetalleMatricula
{
    [Display(Name = "Activo")] Activo = 1,
    [Display(Name = "Retirado")] Retirado = 2
}

/// <summary>Clasificación de los archivos que resguarda el sistema.</summary>
public enum CategoriaDocumento
{
    [Display(Name = "Fotografía de perfil")] Fotografia = 1,
    [Display(Name = "Comprobante de matrícula")] Comprobante = 2,
    [Display(Name = "Programa del curso")] ProgramaCurso = 3,
    [Display(Name = "Acta de notas")] ActaNotas = 4
}
