using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Pruebas;

/// <summary>
/// Comprueba una por una las reglas que gobiernan la matrícula. Cada prueba levanta su propio
/// escenario, así que el orden en que se ejecuten no altera el resultado.
/// </summary>
public class PruebasReglasMatricula
{
    [Fact]
    public async Task GrupoValido_SeAgregaAlBorrador()
    {
        using var banco = new BancoDePruebas();

        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["SC-102 abierto"]);

        Assert.True(resultado.Exitoso);
        Assert.Contains("SC-102", resultado.Mensaje);

        var propias = await banco.Contexto.DetallesMatricula
            .CountAsync(d => d.Matricula!.EstudianteId == banco.EstudianteId);

        Assert.Equal(3, propias); // dos del historial y la recién agregada
    }

    [Fact]
    public async Task CursoSinRequisitoAprobado_SeRechaza()
    {
        // El escenario deja Matemática reprobada con 55, y Programación II exige Programación I,
        // que sí está aprobada. Para probar el requisito faltante se retira la nota aprobatoria.
        using var banco = new BancoDePruebas();

        var aprobada = await banco.Contexto.DetallesMatricula
            .FirstAsync(d => d.GrupoId == banco.Grupos["SC-101 cerrado"]);

        aprobada.NotaFinal = 60;
        await banco.Contexto.SaveChangesAsync();

        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["SC-102 abierto"]);

        Assert.False(resultado.Exitoso);
        Assert.Contains("SC-101", resultado.Mensaje);
        Assert.Contains("requisito", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GrupoSinCupo_SeRechaza()
    {
        using var banco = new BancoDePruebas();

        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["SC-320 lleno"]);

        Assert.False(resultado.Exitoso);
        Assert.Contains("cupo", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GrupoCerrado_SeRechaza()
    {
        using var banco = new BancoDePruebas();

        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["SC-320 cerrado por cupo"]);

        Assert.False(resultado.Exitoso);
        Assert.Contains("no está abierto", resultado.Mensaje);
    }

    [Fact]
    public async Task CursoYaAprobado_SeRechaza()
    {
        using var banco = new BancoDePruebas();

        // Programación I quedó aprobada con 90 en el periodo cerrado, y el periodo abierto
        // vuelve a ofrecerla: el sistema debe impedir que se lleve de nuevo.
        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["SC-101 abierto"]);

        Assert.False(resultado.Exitoso);
        Assert.Contains("aprobado", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CursoRepetidoEnOtroGrupo_SeRechaza()
    {
        using var banco = new BancoDePruebas();

        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["SC-320 abierto"]);

        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["SC-320 lleno"]);

        Assert.False(resultado.Exitoso);
        Assert.Contains("SC-320", resultado.Mensaje);
    }

    [Fact]
    public async Task CursoFueraDelPlanDeEstudios_SeRechaza()
    {
        using var banco = new BancoDePruebas();

        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["AD-210 ajeno"]);

        Assert.False(resultado.Exitoso);
        Assert.Contains("plan de estudios", resultado.Mensaje);
    }

    [Fact]
    public async Task PeriodoConVentanaCerrada_RechazaLaMatricula()
    {
        using var banco = new BancoDePruebas();

        var periodo = await banco.Contexto.PeriodosAcademicos.FirstAsync(p => p.Id == banco.PeriodoAbiertoId);
        periodo.FinMatricula = DateTime.Today.AddDays(-1);
        await banco.Contexto.SaveChangesAsync();

        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["SC-102 abierto"]);

        Assert.False(resultado.Exitoso);
        Assert.Contains("matrícula abierta", resultado.Mensaje);
    }

    [Fact]
    public async Task SuperarElTopeDeCreditos_SeRechaza()
    {
        // El periodo abierto del escenario tiene un tope de 10 créditos.
        using var banco = new BancoDePruebas();

        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["SC-102 abierto"]);   // 4
        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["MA-101 abierto"]);   // 4, total 8

        var resultado = await banco.Matricula.AgregarGrupoAsync(
            banco.EstudianteId, banco.Grupos["SC-320 abierto"]);                                       // 3, total 11

        Assert.False(resultado.Exitoso);
        Assert.Contains("tope", resultado.Mensaje);
        Assert.Contains("10", resultado.Mensaje);
    }

    [Fact]
    public async Task QuitarGrupo_LiberaElCreditoYElCupo()
    {
        using var banco = new BancoDePruebas();

        await banco.Matricula.AgregarGrupoAsync(banco.EstudianteId, banco.Grupos["SC-102 abierto"]);

        var detalle = await banco.Contexto.DetallesMatricula
            .FirstAsync(d => d.GrupoId == banco.Grupos["SC-102 abierto"]);

        var resultado = await banco.Matricula.QuitarGrupoAsync(banco.EstudianteId, detalle.Id);

        Assert.True(resultado.Exitoso);
        Assert.Equal(0, await banco.Matricula.ContarCreditosAsync(detalle.MatriculaId));
    }

    [Fact]
    public async Task QuitarGrupoDeOtraPersona_SeRechaza()
    {
        using var banco = new BancoDePruebas();

        var ajeno = await banco.Contexto.DetallesMatricula
            .FirstAsync(d => d.GrupoId == banco.Grupos["SC-320 lleno"]);

        var resultado = await banco.Matricula.QuitarGrupoAsync(banco.EstudianteId, ajeno.Id);

        Assert.False(resultado.Exitoso);
        Assert.Contains("no pertenece", resultado.Mensaje);
    }

    [Fact]
    public async Task ContarInscritos_IgnoraLasMatriculasAnuladas()
    {
        using var banco = new BancoDePruebas();

        Assert.Equal(1, await banco.Matricula.ContarInscritosAsync(banco.Grupos["SC-320 lleno"]));

        var ajena = await banco.Contexto.Matriculas.FirstAsync(m => m.EstudianteId == "otro-estudiante");
        ajena.Estado = EstadoMatricula.Anulada;
        await banco.Contexto.SaveChangesAsync();

        Assert.Equal(0, await banco.Matricula.ContarInscritosAsync(banco.Grupos["SC-320 lleno"]));
    }

    [Fact]
    public async Task CursosAprobados_SoloIncluyeLosQueAlcanzanLaNota()
    {
        using var banco = new BancoDePruebas();

        var aprobados = await banco.Matricula.ObtenerCursosAprobadosAsync(banco.EstudianteId);

        var programacionUno = await banco.Contexto.Cursos.FirstAsync(c => c.Codigo == "SC-101");
        var matematica = await banco.Contexto.Cursos.FirstAsync(c => c.Codigo == "MA-101");

        Assert.Contains(programacionUno.Id, aprobados);   // nota 90
        Assert.DoesNotContain(matematica.Id, aprobados);  // nota 55
    }
}
