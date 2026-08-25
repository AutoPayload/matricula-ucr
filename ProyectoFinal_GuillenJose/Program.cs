using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.Servicios;
using QuestPDF.Infrastructure;

// =====================================================================================
//  Sistema de Matrícula Universitaria — MatrículaUCR
//  Proyecto final de Programación Avanzada en C#
//  Universidad Fidélitas · II Cuatrimestre 2026
//  José Andrés Guillén Agüero · 118330875
// =====================================================================================

var constructor = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------------------------
// 1. Cultura. Fechas, montos y mensajes se manejan en español de Costa Rica.
// -------------------------------------------------------------------------------------
var culturaEspanol = new CultureInfo("es-CR");
CultureInfo.DefaultThreadCurrentCulture = culturaEspanol;
CultureInfo.DefaultThreadCurrentUICulture = culturaEspanol;

// -------------------------------------------------------------------------------------
// 2. Acceso a datos sobre SQL Server LocalDB.
// -------------------------------------------------------------------------------------
var cadenaConexion = constructor.Configuration.GetConnectionString("ConexionPredeterminada")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'ConexionPredeterminada' en appsettings.json.");

constructor.Services.AddDbContext<ContextoMatricula>(opciones =>
    opciones.UseSqlServer(cadenaConexion, sql => sql.EnableRetryOnFailure()));

constructor.Services.AddDatabaseDeveloperPageExceptionFilter();

// -------------------------------------------------------------------------------------
// 3. Opciones de configuración con enlace fuertemente tipado.
// -------------------------------------------------------------------------------------
constructor.Services.Configure<OpcionesJwt>(constructor.Configuration.GetSection(OpcionesJwt.Seccion));
constructor.Services.Configure<OpcionesAlmacenamiento>(constructor.Configuration.GetSection(OpcionesAlmacenamiento.Seccion));
constructor.Services.Configure<OpcionesMatricula>(constructor.Configuration.GetSection(OpcionesMatricula.Seccion));

// -------------------------------------------------------------------------------------
// 4. Primer mecanismo de autenticación: ASP.NET Identity con cookie de sesión.
//    Es el que usa la persona cuando entra por el navegador.
// -------------------------------------------------------------------------------------
constructor.Services
    .AddIdentity<Usuario, IdentityRole>(opciones =>
    {
        opciones.SignIn.RequireConfirmedAccount = false;
        opciones.User.RequireUniqueEmail = true;

        opciones.Password.RequiredLength = 8;
        opciones.Password.RequireDigit = true;
        opciones.Password.RequireUppercase = true;
        opciones.Password.RequireNonAlphanumeric = false;

        // Tres intentos fallidos bloquean la cuenta por cinco minutos.
        opciones.Lockout.MaxFailedAccessAttempts = 5;
        opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ContextoMatricula>()
    .AddClaimsPrincipalFactory<FabricaAfirmacionesUsuario>()
    .AddDefaultTokenProviders();

constructor.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.LoginPath = "/Cuenta/Ingreso";
    opciones.LogoutPath = "/Cuenta/Salir";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    // El parámetro de retorno también va en español, para no mezclar idiomas en la barra de direcciones.
    opciones.ReturnUrlParameter = "rutaRetorno";
    opciones.ExpireTimeSpan = TimeSpan.FromHours(4);
    opciones.SlidingExpiration = true;
    opciones.Cookie.Name = "MatriculaUCR.Sesion";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SameSite = SameSiteMode.Lax;
});

// -------------------------------------------------------------------------------------
// 5. Segundo mecanismo: token JWT para la API interna que atiende las peticiones asíncronas.
//    Se registra como esquema adicional; la cookie sigue siendo el esquema predeterminado.
// -------------------------------------------------------------------------------------
var opcionesJwt = constructor.Configuration.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>()
    ?? throw new InvalidOperationException("No se encontró la sección 'Jwt' en appsettings.json.");

if (opcionesJwt.ClaveSecreta.Length < 32)
{
    throw new InvalidOperationException(
        "La clave secreta del JWT debe tener al menos 32 caracteres. Revise la sección 'Jwt'.");
}

constructor.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = opcionesJwt.Emisor,
            ValidAudience = opcionesJwt.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcionesJwt.ClaveSecreta)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // La API responde en JSON aun cuando el rechazo ocurre antes de llegar al controlador.
        opciones.Events = new JwtBearerEvents
        {
            OnChallenge = async contexto =>
            {
                contexto.HandleResponse();
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                contexto.Response.ContentType = "application/problem+json";
                await contexto.Response.WriteAsJsonAsync(new
                {
                    titulo = "No autorizado",
                    estado = 401,
                    detalle = "El token está ausente, es inválido o ya expiró. Solicite uno nuevo en /api/autenticacion/token."
                });
            },
            OnForbidden = async contexto =>
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                contexto.Response.ContentType = "application/problem+json";
                await contexto.Response.WriteAsJsonAsync(new
                {
                    titulo = "Acceso denegado",
                    estado = 403,
                    detalle = "Su rol no tiene permiso para esta operación."
                });
            }
        };
    });

// -------------------------------------------------------------------------------------
// 6. Políticas de autorización. Los controladores nombran la política, no el rol suelto.
// -------------------------------------------------------------------------------------
constructor.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.SoloAdministracion, politica =>
        politica.RequireRole(RolesSistema.Administrador))
    .AddPolicy(Politicas.SoloDocencia, politica =>
        politica.RequireRole(RolesSistema.Docente))
    .AddPolicy(Politicas.SoloEstudiantado, politica =>
        politica.RequireRole(RolesSistema.Estudiante))
    .AddPolicy(Politicas.PersonalAcademico, politica =>
        politica.RequireRole(RolesSistema.Administrador, RolesSistema.Docente));

// -------------------------------------------------------------------------------------
// 7. Servicios propios de la aplicación.
// -------------------------------------------------------------------------------------
constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<IAlmacenamientoArchivos, AlmacenamientoLocal>();
constructor.Services.AddScoped<ServicioBitacora>();
constructor.Services.AddScoped<ServicioNotificaciones>();
constructor.Services.AddScoped<ServicioComprobantes>();
constructor.Services.AddScoped<ServicioMatricula>();
constructor.Services.AddScoped<ServicioCatalogo>();
constructor.Services.AddScoped<ServicioEstadisticas>();
constructor.Services.AddSingleton<ServicioTokens>();

// -------------------------------------------------------------------------------------
// 8. MVC con las vistas Razor reubicadas en la carpeta Vistas, para que el proyecto quede
//    íntegramente en español y no mezcle convenciones de dos idiomas.
// -------------------------------------------------------------------------------------
constructor.Services.AddControllersWithViews();

constructor.Services.Configure<RazorViewEngineOptions>(opciones =>
{
    opciones.ViewLocationFormats.Clear();
    opciones.ViewLocationFormats.Add("/Vistas/{1}/{0}" + RazorViewEngine.ViewExtension);
    opciones.ViewLocationFormats.Add("/Vistas/Compartidas/{0}" + RazorViewEngine.ViewExtension);

    opciones.AreaViewLocationFormats.Clear();
    opciones.AreaViewLocationFormats.Add("/Areas/{2}/Vistas/{1}/{0}" + RazorViewEngine.ViewExtension);
    opciones.AreaViewLocationFormats.Add("/Areas/{2}/Vistas/Compartidas/{0}" + RazorViewEngine.ViewExtension);
    opciones.AreaViewLocationFormats.Add("/Vistas/Compartidas/{0}" + RazorViewEngine.ViewExtension);
});

// QuestPDF se distribuye con licencia comunitaria para uso académico y proyectos pequeños.
QuestPDF.Settings.License = LicenseType.Community;

var aplicacion = constructor.Build();

// -------------------------------------------------------------------------------------
// 9. Preparación de la base de datos: migraciones pendientes y datos iniciales.
// -------------------------------------------------------------------------------------
await PrepararBaseDatosAsync(aplicacion);

// -------------------------------------------------------------------------------------
// 10. Canalización de peticiones.
// -------------------------------------------------------------------------------------
aplicacion.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaEspanol),
    SupportedCultures = [culturaEspanol],
    SupportedUICultures = [culturaEspanol]
});

if (aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseMigrationsEndPoint();
}
else
{
    aplicacion.UseExceptionHandler("/Inicio/Error");
    aplicacion.UseHsts();
}

// Convierte los códigos 403 y 404 en páginas propias, con la misma identidad visual del sitio.
aplicacion.UseStatusCodePagesWithReExecute("/Inicio/CodigoEstado/{0}");

aplicacion.UseHttpsRedirection();
aplicacion.UseStaticFiles();
aplicacion.UseRouting();

aplicacion.UseAuthentication();
aplicacion.UseAuthorization();

aplicacion.MapControllerRoute(
    name: "predeterminada",
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

aplicacion.Run();

// =====================================================================================
//  Función local: aplica migraciones y siembra los datos de demostración.
// =====================================================================================
static async Task PrepararBaseDatosAsync(WebApplication aplicacion)
{
    using var alcance = aplicacion.Services.CreateScope();
    var proveedor = alcance.ServiceProvider;
    var registrador = proveedor.GetRequiredService<ILoggerFactory>().CreateLogger("InicioBaseDatos");

    try
    {
        var contexto = proveedor.GetRequiredService<ContextoMatricula>();

        registrador.LogInformation("Aplicando migraciones pendientes...");
        await contexto.Database.MigrateAsync();

        registrador.LogInformation("Verificando datos iniciales...");
        await SembradorDatos.SembrarAsync(proveedor);

        registrador.LogInformation("La base de datos está lista.");
    }
    catch (Exception excepcion)
    {
        registrador.LogError(excepcion,
            "No fue posible preparar la base de datos. Revise la cadena de conexión en appsettings.json.");
        throw;
    }
}

/// <summary>Declaración explícita para que el proyecto de pruebas referencie el ensamblado.</summary>
public partial class Program;
