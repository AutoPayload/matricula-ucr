using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using ProyectoFinal_GuillenJose.Configuracion;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;
using ProyectoFinal_GuillenJose.Servicios;
using ProyectoFinal_GuillenJose.Validaciones;

namespace ProyectoFinal_GuillenJose.Pruebas;

/// <summary>Comprueba la emisión de los tokens JWT que protegen la API interna.</summary>
public class PruebasServicioTokens
{
    private static ServicioTokens CrearServicio() => new(Options.Create(new OpcionesJwt
    {
        Emisor = "MatriculaUCR.Servidor",
        Audiencia = "MatriculaUCR.Cliente",
        ClaveSecreta = "ClaveDeFirmaParaElProyectoFinalDeProgramacionAvanzada2026",
        MinutosDeVigencia = 45
    }));

    private static Usuario CrearUsuario() => new()
    {
        Id = "usuario-1",
        UserName = "jose.guillen@matriculaucr.cr",
        Email = "jose.guillen@matriculaucr.cr",
        Identificacion = "118330875",
        Nombre = "José",
        Apellidos = "Guillén"
    };

    [Fact]
    public void ElTokenLlevaLaIdentidadYLosRoles()
    {
        var (token, _) = CrearServicio().GenerarToken(CrearUsuario(), ["Estudiante", "Docente"]);
        var leido = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("MatriculaUCR.Servidor", leido.Issuer);
        Assert.Contains(leido.Claims, c => c.Type == "nombreCompleto" && c.Value == "José Guillén");
        Assert.Contains(leido.Claims, c => c.Type == "identificacion" && c.Value == "118330875");

        var roles = leido.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        Assert.Contains("Estudiante", roles);
        Assert.Contains("Docente", roles);
    }

    [Fact]
    public void ElTokenExpiraSegunLaVigenciaConfigurada()
    {
        var (_, expiraEn) = CrearServicio().GenerarToken(CrearUsuario(), ["Estudiante"]);
        var minutos = (expiraEn - DateTime.UtcNow).TotalMinutes;

        Assert.InRange(minutos, 43, 46);
    }

    [Fact]
    public void DosTokensDelMismoUsuarioTienenIdentificadoresDistintos()
    {
        var servicio = CrearServicio();
        var usuario = CrearUsuario();

        var manejador = new JwtSecurityTokenHandler();
        var primero = manejador.ReadJwtToken(servicio.GenerarToken(usuario, ["Estudiante"]).Token);
        var segundo = manejador.ReadJwtToken(servicio.GenerarToken(usuario, ["Estudiante"]).Token);

        Assert.NotEqual(primero.Id, segundo.Id);
    }
}

/// <summary>Comprueba los atributos de validación propios del proyecto.</summary>
public class PruebasValidaciones
{
    [Theory]
    [InlineData("118330875", true)]     // cédula nacional
    [InlineData("1-1833-0875", true)]   // con guiones
    [InlineData("155812345678", true)]  // documento migratorio de 12
    [InlineData("12345678", false)]     // ocho dígitos
    [InlineData("1234567890", false)]   // diez dígitos
    [InlineData("11833087A", false)]    // con letra
    public void CedulaCostarricense_ValidaElFormato(string valor, bool esperado)
    {
        var atributo = new CedulaCostarricenseAttribute();
        Assert.Equal(esperado, atributo.IsValid(valor));
    }

    [Fact]
    public void CedulaVacia_NoSeValidaAqui()
    {
        // La obligatoriedad la declara [Required]; este atributo solo revisa el formato.
        Assert.True(new CedulaCostarricenseAttribute().IsValid(null));
        Assert.True(new CedulaCostarricenseAttribute().IsValid(string.Empty));
    }

    [Fact]
    public void MayorDeEdad_RechazaAQuienNoAlcanzaLaEdadMinima()
    {
        var atributo = new MayorDeEdadAttribute(15);

        Assert.True(atributo.IsValid(DateTime.Today.AddYears(-20)));
        Assert.True(atributo.IsValid(DateTime.Today.AddYears(-15)));
        Assert.False(atributo.IsValid(DateTime.Today.AddYears(-14)));
        Assert.False(atributo.IsValid(DateTime.Today.AddDays(1)));
    }

    [Fact]
    public void FechaPosteriorA_ExigeElOrdenDeLasFechas()
    {
        var periodo = new PeriodoAcademico
        {
            Codigo = "III-2026",
            Nombre = "III Cuatrimestre 2026",
            InicioMatricula = new DateTime(2026, 8, 1),
            FinMatricula = new DateTime(2026, 7, 1),
            FechaInicio = new DateTime(2026, 9, 1),
            FechaFin = new DateTime(2026, 12, 1)
        };

        var resultados = new List<ValidationResult>();
        var valido = Validator.TryValidateObject(periodo, new ValidationContext(periodo), resultados, true);

        Assert.False(valido);
        Assert.Contains(resultados, r => r.ErrorMessage!.Contains("posterior"));
    }
}

/// <summary>Comprueba el cálculo de la paginación que usan el catálogo y los mantenimientos.</summary>
public class PruebasPaginacion
{
    [Fact]
    public void CalculaLasPaginasYElRangoDeFilas()
    {
        var pagina = ResultadoPaginado<int>.Crear([9, 10, 11, 12, 13, 14, 15, 16], pagina: 2, tamano: 8, total: 21);

        Assert.Equal(3, pagina.TotalPaginas);
        Assert.Equal(9, pagina.PrimeraFila);
        Assert.Equal(16, pagina.UltimaFila);
        Assert.True(pagina.HayAnterior);
        Assert.True(pagina.HaySiguiente);
    }

    [Fact]
    public void LaUltimaPaginaNoOfreceSiguiente()
    {
        var pagina = ResultadoPaginado<int>.Crear([17, 18, 19, 20, 21], pagina: 3, tamano: 8, total: 21);

        Assert.Equal(21, pagina.UltimaFila);
        Assert.False(pagina.HaySiguiente);
        Assert.True(pagina.HayAnterior);
    }

    [Fact]
    public void SinResultados_LaPaginacionQuedaEnCeros()
    {
        var pagina = ResultadoPaginado<int>.Crear([], pagina: 1, tamano: 8, total: 0);

        Assert.Equal(1, pagina.TotalPaginas);
        Assert.Equal(0, pagina.PrimeraFila);
        Assert.Equal(0, pagina.UltimaFila);
        Assert.False(pagina.HayAnterior);
        Assert.False(pagina.HaySiguiente);
    }
}

/// <summary>Comprueba el cálculo del estado del periodo y de la condición de aprobación.</summary>
public class PruebasReglasDeDominio
{
    [Fact]
    public void PeriodoAceptaMatriculaSoloDentroDeLaVentanaYEnEstadoAbierto()
    {
        var periodo = new PeriodoAcademico
        {
            Estado = EstadoPeriodo.MatriculaAbierta,
            InicioMatricula = new DateTime(2026, 8, 1),
            FinMatricula = new DateTime(2026, 8, 20)
        };

        Assert.True(periodo.AceptaMatricula(new DateTime(2026, 8, 10)));
        Assert.True(periodo.AceptaMatricula(new DateTime(2026, 8, 1)));
        Assert.True(periodo.AceptaMatricula(new DateTime(2026, 8, 20, 23, 59, 0)));
        Assert.False(periodo.AceptaMatricula(new DateTime(2026, 7, 31)));
        Assert.False(periodo.AceptaMatricula(new DateTime(2026, 8, 21)));

        periodo.Estado = EstadoPeriodo.EnCurso;
        Assert.False(periodo.AceptaMatricula(new DateTime(2026, 8, 10)));
    }

    [Theory]
    [InlineData(70, true)]
    [InlineData(69, false)]
    [InlineData(100, true)]
    [InlineData(null, false)]
    public void LaCondicionDeAprobacionSigueLaNotaMinima(int? nota, bool esperado)
    {
        var detalle = new DetalleMatricula { NotaFinal = nota };
        Assert.Equal(esperado, detalle.Aprobado);
    }

    [Fact]
    public void LasInicialesSalenDelNombreYDelPrimerApellido()
    {
        var usuario = new Usuario { Nombre = "José Andrés", Apellidos = "Guillén Agüero" };
        Assert.Equal("JG", usuario.Iniciales);
        Assert.Equal("José Andrés Guillén Agüero", usuario.NombreCompleto);
    }

    [Fact]
    public void ElTamanoDelDocumentoSeMuestraEnLaUnidadQueCorresponde()
    {
        Assert.Equal("512 B", new Documento { TamanoBytes = 512 }.TamanoLegible);
        Assert.Equal("2,0 KB", new Documento { TamanoBytes = 2048 }.TamanoLegible);
        Assert.Equal("1,5 MB", new Documento { TamanoBytes = 1572864 }.TamanoLegible);
    }
}
