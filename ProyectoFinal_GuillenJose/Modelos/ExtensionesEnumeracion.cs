using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ProyectoFinal_GuillenJose.Modelos;

/// <summary>
/// Traduce una enumeración al texto que la persona debe leer. Sin esto, el estado
/// MatriculaAbierta llegaría a la pantalla escrito de corrido, sin tilde y sin espacio.
/// </summary>
public static class ExtensionesEnumeracion
{
    /// <summary>
    /// Devuelve el nombre declarado en el atributo Display y, si no lo hay, el nombre del valor.
    /// </summary>
    public static string Describir(this Enum valor)
    {
        var campo = valor.GetType().GetField(valor.ToString());

        return campo?.GetCustomAttribute<DisplayAttribute>()?.Name ?? valor.ToString();
    }
}
