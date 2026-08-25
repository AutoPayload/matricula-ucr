using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.Validaciones;

namespace ProyectoFinal_GuillenJose.ModelosVista;

/// <summary>
/// Datos del formulario de registro. Corresponde a la pantalla 1 del prototipo, ampliada con
/// la identificación y el nombre porque el comprobante de matrícula los necesita.
/// </summary>
public class ModeloRegistro
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Escriba un correo electrónico válido.")]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La identificación es obligatoria.")]
    [CedulaCostarricense]
    [Display(Name = "Número de identificación")]
    public string Identificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 80 caracteres.")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione la carrera en la que desea empadronarse.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la carrera en la que desea empadronarse.")]
    [Display(Name = "Carrera")]
    public int CarreraId { get; set; }

    [DataType(DataType.Date)]
    [MayorDeEdad(15)]
    [Display(Name = "Fecha de nacimiento")]
    public DateTime? FechaNacimiento { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Clave), ErrorMessage = "Las dos contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmacionClave { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> Carreras { get; set; } = [];
}

/// <summary>Datos del formulario de inicio de sesión. Es la pantalla 2 del prototipo.</summary>
public class ModeloIngreso
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Escriba un correo electrónico válido.")]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;

    [Display(Name = "Mantener la sesión abierta")]
    public bool Recordarme { get; set; }

    public string? RutaRetorno { get; set; }
}

/// <summary>Información que se muestra y se edita en la pantalla de perfil.</summary>
public class ModeloPerfil
{
    public string UsuarioId { get; set; } = string.Empty;

    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = string.Empty;

    [Display(Name = "Identificación")]
    public string Identificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(80)]
    public string Apellidos { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de nacimiento")]
    public DateTime? FechaNacimiento { get; set; }

    [Display(Name = "Carrera")]
    public string NombreCarrera { get; set; } = "Sin carrera asignada";

    public string Rol { get; set; } = string.Empty;
    public int? FotografiaDocumentoId { get; set; }
    public DateTime FechaRegistro { get; set; }

    /// <summary>Fotografía que la persona sube desde la pantalla de perfil.</summary>
    [Display(Name = "Fotografía")]
    public IFormFile? Fotografia { get; set; }
}

/// <summary>Cambio de contraseña desde el perfil.</summary>
public class ModeloCambioClave
{
    [Required(ErrorMessage = "Escriba su contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string ClaveActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escriba la contraseña nueva.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña nueva")]
    public string ClaveNueva { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(ClaveNueva), ErrorMessage = "Las dos contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña nueva")]
    public string ConfirmacionClave { get; set; } = string.Empty;
}

/// <summary>Bandeja de avisos de la persona usuaria.</summary>
public class ModeloAvisos
{
    public List<Notificacion> Avisos { get; init; } = [];
    public int Pendientes { get; init; }
}
