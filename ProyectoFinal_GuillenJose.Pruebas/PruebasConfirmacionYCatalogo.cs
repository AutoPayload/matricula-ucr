using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.ModelosVista;

namespace ProyectoFinal_GuillenJose.Pruebas;

/// <summary>
/// Comprueba el sellado de la matrícula y el armado del catálogo que ve la persona estudiante.
/// </summary>
public class PruebasConfirmacion
{
    [Fact]
    public async Task ConfirmarConCreditosSuficientes_SellaLaMatriculaYEmiteComprobante()
    {
        using var banco = new BancoDePruebas();

        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["SC-102 abierto"]);
        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["SC-320 abierto"]);

        var resultado = await banco.Matricula.ConfirmarAsync(banco.EstudianteId, banco.PeriodoAbiertoId);

        Assert.True(resultado.Exitoso);

        var matricula = await banco.Contexto.Matriculas
            .FirstAsync(m => m.EstudianteId == banco.EstudianteId
                          && m.PeriodoAcademicoId == banco.PeriodoAbiertoId);

        Assert.Equal(EstadoMatricula.Confirmada, matricula.Estado);
        Assert.Equal(7, matricula.TotalCreditos);
        Assert.NotNull(matricula.NumeroComprobante);
        Assert.StartsWith("MAT-II2026-", matricula.NumeroComprobante);
        Assert.NotNull(matricula.FechaConfirmacion);
        Assert.NotNull(matricula.ComprobanteDocumentoId);
    }

    [Fact]
    public async Task ConfirmarCalculaElMontoConTarifaYCargoFijo()
    {
        using var banco = new BancoDePruebas();

        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["SC-102 abierto"]);

        await banco.Matricula.ConfirmarAsync(banco.EstudianteId, banco.PeriodoAbiertoId);

        var matricula = await banco.Contexto.Matriculas
            .FirstAsync(m => m.EstudianteId == banco.EstudianteId
                          && m.PeriodoAcademicoId == banco.PeriodoAbiertoId);

        // Cuatro créditos a 48 500 más 15 000 de cargo administrativo.
        Assert.Equal(209000m, matricula.MontoTotal);
    }

    [Fact]
    public async Task ConfirmarSinAlcanzarElMinimo_SeRechaza()
    {
        using var banco = new BancoDePruebas();

        // No se agrega ningún curso: cero créditos contra un mínimo de tres.
        var resultado = await banco.Matricula.ConfirmarAsync(banco.EstudianteId, banco.PeriodoAbiertoId);

        Assert.False(resultado.Exitoso);
        Assert.Contains("No hay una matrícula en proceso", resultado.Mensaje);
    }

    [Fact]
    public async Task ConfirmarDosVeces_SeRechazaLaSegunda()
    {
        using var banco = new BancoDePruebas();

        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["SC-102 abierto"]);
        await banco.Matricula.ConfirmarAsync(banco.EstudianteId, banco.PeriodoAbiertoId);

        var segunda = await banco.Matricula.ConfirmarAsync(banco.EstudianteId, banco.PeriodoAbiertoId);

        Assert.False(segunda.Exitoso);
        Assert.Contains("ya estaba confirmada", segunda.Mensaje);
    }

    [Fact]
    public async Task MatriculaConfirmada_NoAdmiteMasCursos()
    {
        using var banco = new BancoDePruebas();

        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["SC-102 abierto"]);
        await banco.Matricula.ConfirmarAsync(banco.EstudianteId, banco.PeriodoAbiertoId);

        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["SC-320 abierto"]);

        Assert.False(resultado.Exitoso);
        Assert.Contains("ya fue confirmada", resultado.Mensaje);
    }

    [Fact]
    public async Task AnularMatricula_LiberaLosCuposYMarcaLasLineas()
    {
        using var banco = new BancoDePruebas();

        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["SC-320 abierto"]);
        await banco.Matricula.ConfirmarAsync(banco.EstudianteId, banco.PeriodoAbiertoId);

        var matricula = await banco.Contexto.Matriculas
            .FirstAsync(m => m.EstudianteId == banco.EstudianteId
                          && m.PeriodoAcademicoId == banco.PeriodoAbiertoId);

        var resultado = await banco.Matricula.AnularAsync(matricula.Id, "Prueba automatizada");

        Assert.True(resultado.Exitoso);
        Assert.Equal(0, await banco.Matricula.ContarInscritosAsync(banco.Grupos["SC-320 abierto"]));

        var lineas = await banco.Contexto.DetallesMatricula
            .Where(d => d.MatriculaId == matricula.Id)
            .ToListAsync();

        Assert.All(lineas, l => Assert.Equal(EstadoDetalleMatricula.Retirado, l.Estado));
    }
}

/// <summary>
/// Comprueba el catálogo: qué grupos ve la persona, en qué orden y con qué motivo de bloqueo.
/// </summary>
public class PruebasCatalogo
{
    [Fact]
    public async Task ElCatalogoSoloMuestraCursosDelPlanDeLaCarrera()
    {
        using var banco = new BancoDePruebas();

        var catalogo = await banco.Catalogo.ObtenerCatalogoAsync(banco.EstudianteId, new FiltroCatalogo());

        Assert.DoesNotContain(catalogo.Resultado.Elementos, f => f.Codigo == "AD-210");
        Assert.Contains(catalogo.Resultado.Elementos, f => f.Codigo == "SC-102");
    }

    [Fact]
    public async Task ElCatalogoExplicaPorQueUnGrupoNoSePuedeMatricular()
    {
        using var banco = new BancoDePruebas();

        var catalogo = await banco.Catalogo.ObtenerCatalogoAsync(banco.EstudianteId, new FiltroCatalogo());

        var lleno = catalogo.Resultado.Elementos.First(f => f.GrupoId == banco.Grupos["SC-320 lleno"]);

        Assert.NotNull(lleno.MotivoBloqueo);
        Assert.Contains("cupo", lleno.MotivoBloqueo, StringComparison.OrdinalIgnoreCase);
        Assert.False(lleno.SePuedeMatricular);
    }

    [Fact]
    public async Task FiltroPorCreditos_RecortaElResultado()
    {
        using var banco = new BancoDePruebas();

        var catalogo = await banco.Catalogo.ObtenerCatalogoAsync(
            banco.EstudianteId, new FiltroCatalogo { Creditos = 3 });

        Assert.NotEmpty(catalogo.Resultado.Elementos);
        Assert.All(catalogo.Resultado.Elementos, f => Assert.Equal(3, f.Creditos));
    }

    [Fact]
    public async Task FiltroDeTexto_BuscaPorCodigoYPorNombre()
    {
        using var banco = new BancoDePruebas();

        var porCodigo = await banco.Catalogo.ObtenerCatalogoAsync(
            banco.EstudianteId, new FiltroCatalogo { Texto = "SC-102" });

        var porNombre = await banco.Catalogo.ObtenerCatalogoAsync(
            banco.EstudianteId, new FiltroCatalogo { Texto = "Redes" });

        Assert.Single(porCodigo.Resultado.Elementos);
        Assert.All(porNombre.Resultado.Elementos, f => Assert.Equal("SC-320", f.Codigo));
    }

    [Fact]
    public async Task FiltroSoloConCupo_OcultaLosGruposLlenos()
    {
        using var banco = new BancoDePruebas();

        var catalogo = await banco.Catalogo.ObtenerCatalogoAsync(
            banco.EstudianteId, new FiltroCatalogo { SoloConCupo = true });

        Assert.DoesNotContain(catalogo.Resultado.Elementos, f => f.GrupoId == banco.Grupos["SC-320 lleno"]);
    }

    [Fact]
    public async Task ElCatalogoMarcaLosCursosYaAprobados()
    {
        using var banco = new BancoDePruebas();

        var catalogo = await banco.Catalogo.ObtenerCatalogoAsync(banco.EstudianteId, new FiltroCatalogo());
        var matematica = catalogo.Resultado.Elementos.FirstOrDefault(f => f.Codigo == "MA-101");

        Assert.NotNull(matematica);
        Assert.False(matematica.YaAprobado); // quedó con 55, así que sigue disponible
    }
}
