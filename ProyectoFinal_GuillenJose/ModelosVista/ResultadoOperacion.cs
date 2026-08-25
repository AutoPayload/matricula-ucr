namespace ProyectoFinal_GuillenJose.ModelosVista;

/// <summary>
/// Respuesta uniforme de los servicios de dominio. Evita lanzar excepciones para comunicar
/// reglas de negocio incumplidas, que son situaciones esperables y no fallas del sistema.
/// </summary>
public class ResultadoOperacion
{
    public bool Exitoso { get; private init; }
    public string Mensaje { get; private init; } = string.Empty;

    /// <summary>Identificador del registro afectado, cuando la operación lo produce.</summary>
    public int? Identificador { get; private init; }

    public static ResultadoOperacion Correcto(string mensaje, int? identificador = null) =>
        new() { Exitoso = true, Mensaje = mensaje, Identificador = identificador };

    public static ResultadoOperacion Fallido(string mensaje) =>
        new() { Exitoso = false, Mensaje = mensaje };
}
