using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal_GuillenJose.Validaciones;

/// <summary>
/// Valida un número de identificación costarricense. Acepta la cédula nacional de nueve dígitos
/// y el documento de identidad migratorio, que llega con once o doce. Los guiones y espacios se
/// ignoran, porque la persona los escribe o no según cómo tenga a mano el documento.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CedulaCostarricenseAttribute : ValidationAttribute
{
    public CedulaCostarricenseAttribute()
        : base("El número de identificación debe tener 9 dígitos para cédula nacional o entre 11 y 12 para DIMEX.")
    {
    }

    public override bool IsValid(object? valor)
    {
        if (valor is not string texto || string.IsNullOrWhiteSpace(texto))
        {
            // La obligatoriedad se declara aparte con [Required]; aquí solo se valida el formato.
            return true;
        }

        var digitos = new string([.. texto.Where(char.IsDigit)]);

        if (digitos.Length != texto.Replace("-", string.Empty).Replace(" ", string.Empty).Length)
        {
            return false;
        }

        return digitos.Length is 9 or 11 or 12;
    }
}

/// <summary>
/// Exige una edad mínima a partir de la fecha de nacimiento. La universidad no matricula
/// personas menores de quince años, así que el formulario lo rechaza antes de llegar a la base.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class MayorDeEdadAttribute(int edadMinima) : ValidationAttribute
{
    public int EdadMinima { get; } = edadMinima;

    public override bool IsValid(object? valor)
    {
        if (valor is null)
        {
            return true;
        }

        if (valor is not DateTime nacimiento)
        {
            return false;
        }

        if (nacimiento.Date > DateTime.Today)
        {
            ErrorMessage = "La fecha de nacimiento no puede estar en el futuro.";
            return false;
        }

        var edad = DateTime.Today.Year - nacimiento.Year;

        if (nacimiento.Date > DateTime.Today.AddYears(-edad))
        {
            edad--;
        }

        if (edad < EdadMinima)
        {
            ErrorMessage = $"Debe tener al menos {EdadMinima} años para matricularse.";
            return false;
        }

        return true;
    }
}

/// <summary>
/// Compara dos fechas del mismo modelo y exige que esta sea posterior a la indicada. Se usa en
/// el periodo académico, donde el cierre de matrícula no puede quedar antes de la apertura ni
/// el fin de lecciones antes del inicio.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FechaPosteriorAAttribute(string propiedadAnterior) : ValidationAttribute
{
    public string PropiedadAnterior { get; } = propiedadAnterior;

    protected override ValidationResult? IsValid(object? valor, ValidationContext contexto)
    {
        if (valor is not DateTime fechaFinal)
        {
            return ValidationResult.Success;
        }

        var propiedad = contexto.ObjectType.GetProperty(PropiedadAnterior);

        if (propiedad is null)
        {
            return new ValidationResult($"No se encontró la propiedad {PropiedadAnterior} para comparar.");
        }

        if (propiedad.GetValue(contexto.ObjectInstance) is not DateTime fechaInicial)
        {
            return ValidationResult.Success;
        }

        if (fechaFinal.Date <= fechaInicial.Date)
        {
            var nombreAnterior = propiedad.GetCustomAttributes(typeof(DisplayAttribute), false)
                .OfType<DisplayAttribute>()
                .FirstOrDefault()?.Name ?? PropiedadAnterior;

            return new ValidationResult(
                $"La fecha debe ser posterior a {nombreAnterior.ToLowerInvariant()}.",
                [contexto.MemberName ?? string.Empty]);
        }

        return ValidationResult.Success;
    }
}
