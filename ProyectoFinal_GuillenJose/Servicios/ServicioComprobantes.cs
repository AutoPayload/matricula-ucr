using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Servicios;

/// <summary>
/// Genera los documentos oficiales del sistema en PDF con la biblioteca QuestPDF, que no formó
/// parte del temario del curso. Se eligió sobre las alternativas de conversión desde HTML porque
/// describe el documento con código tipado, no depende de un motor de navegador instalado en el
/// servidor y produce el mismo resultado en cualquier máquina.
///
/// Los archivos generados no se devuelven sueltos: se guardan en el almacén como cualquier otra
/// carga, de modo que quedan versionados, auditados y sujetos al mismo control de descarga.
/// </summary>
public class ServicioComprobantes(ContextoMatricula contexto, IAlmacenamientoArchivos almacen)
{
    private const string ColorAcento = "#C15F3C";
    private const string ColorTexto = "#1F1E1D";
    private const string ColorTenue = "#63615C";
    private const string ColorLinea = "#DEDBD3";

    /// <summary>
    /// Arma el comprobante de matrícula y lo deja guardado en el almacén de archivos.
    /// </summary>
    public async Task<Documento> GenerarComprobanteMatriculaAsync(Matricula matricula)
    {
        ArgumentNullException.ThrowIfNull(matricula);

        var estudiante = matricula.Estudiante
            ?? await contexto.Users.AsNoTracking().FirstAsync(u => u.Id == matricula.EstudianteId);

        var carrera = estudiante.CarreraId is null
            ? null
            : await contexto.Carreras.AsNoTracking().FirstOrDefaultAsync(c => c.Id == estudiante.CarreraId);

        var lineas = await contexto.DetallesMatricula
            .AsNoTracking()
            .Include(d => d.Grupo!).ThenInclude(g => g.Curso)
            .Include(d => d.Grupo!).ThenInclude(g => g.Docente)
            .Where(d => d.MatriculaId == matricula.Id && d.Estado == EstadoDetalleMatricula.Activo)
            .OrderBy(d => d.Grupo!.Curso!.Codigo)
            .ToListAsync();

        var periodo = matricula.PeriodoAcademico
            ?? await contexto.PeriodosAcademicos.AsNoTracking()
                .FirstAsync(p => p.Id == matricula.PeriodoAcademicoId);

        var contenido = QuestPDF.Fluent.Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                ConfigurarPagina(pagina);

                pagina.Header().Element(marco => DibujarEncabezado(
                    marco, "Comprobante de matrícula", matricula.NumeroComprobante ?? "En proceso"));

                pagina.Content().PaddingVertical(18).Column(columna =>
                {
                    columna.Spacing(16);

                    columna.Item().Element(marco => DibujarFicha(marco,
                    [
                        ("Estudiante", estudiante.NombreCompleto),
                        ("Identificación", estudiante.Identificacion),
                        ("Carrera", carrera?.Nombre ?? "Sin carrera asignada"),
                        ("Periodo lectivo", periodo.Nombre),
                        ("Fecha de confirmación",
                            matricula.FechaConfirmacion?.ToString("dd 'de' MMMM 'de' yyyy, HH:mm") ?? "Sin confirmar")
                    ]));

                    columna.Item().Element(marco => DibujarTablaCursos(marco, lineas));

                    columna.Item().Element(marco => DibujarTotales(marco, matricula, lineas.Count));

                    columna.Item().PaddingTop(10).Text(texto =>
                    {
                        texto.DefaultTextStyle(estilo => estilo.FontSize(8).FontColor(ColorTenue));
                        texto.Span(
                            "Este comprobante fue generado de forma automática por el sistema de matrícula " +
                            "y no requiere firma. Conserve el número de comprobante para cualquier gestión " +
                            "ante la oficina de registro.");
                    });
                });

                pagina.Footer().Element(DibujarPie);
            });
        }).GeneratePdf();

        var nombre = $"Comprobante_{matricula.NumeroComprobante ?? matricula.Id.ToString()}.pdf";

        return await almacen.GuardarContenidoAsync(
            contenido, nombre, "application/pdf", CategoriaDocumento.Comprobante, matricula.EstudianteId);
    }

    /// <summary>
    /// Arma el acta de notas de un grupo, con la lista de estudiantes y su condición final.
    /// </summary>
    public async Task<Documento> GenerarActaNotasAsync(Grupo grupo, string? generadaPorUsuarioId)
    {
        ArgumentNullException.ThrowIfNull(grupo);

        var lineas = await contexto.DetallesMatricula
            .AsNoTracking()
            .Include(d => d.Matricula!).ThenInclude(m => m.Estudiante)
            .Where(d => d.GrupoId == grupo.Id
                     && d.Estado == EstadoDetalleMatricula.Activo
                     && d.Matricula!.Estado == EstadoMatricula.Confirmada)
            .OrderBy(d => d.Matricula!.Estudiante!.Apellidos)
            .ToListAsync();

        var curso = grupo.Curso ?? await contexto.Cursos.AsNoTracking().FirstAsync(c => c.Id == grupo.CursoId);
        var periodo = grupo.PeriodoAcademico
            ?? await contexto.PeriodosAcademicos.AsNoTracking().FirstAsync(p => p.Id == grupo.PeriodoAcademicoId);
        var docente = grupo.Docente;

        if (docente is null && grupo.DocenteId is not null)
        {
            docente = await contexto.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == grupo.DocenteId);
        }

        var aprobados = lineas.Count(l => l.Aprobado);
        var conNota = lineas.Count(l => l.NotaFinal.HasValue);
        var promedio = conNota == 0 ? 0 : lineas.Where(l => l.NotaFinal.HasValue).Average(l => l.NotaFinal!.Value);

        var contenido = QuestPDF.Fluent.Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                ConfigurarPagina(pagina);

                pagina.Header().Element(marco => DibujarEncabezado(
                    marco, "Acta de notas", $"{curso.Codigo} grupo {grupo.NumeroGrupo:00}"));

                pagina.Content().PaddingVertical(18).Column(columna =>
                {
                    columna.Spacing(16);

                    columna.Item().Element(marco => DibujarFicha(marco,
                    [
                        ("Curso", $"{curso.Codigo} — {curso.Nombre}"),
                        ("Créditos", curso.Creditos.ToString()),
                        ("Docente", docente?.NombreCompleto ?? "Sin asignar"),
                        ("Periodo lectivo", periodo.Nombre),
                        ("Horario", $"{grupo.Horario} · Aula {grupo.Aula}")
                    ]));

                    columna.Item().Element(marco => DibujarTablaNotas(marco, lineas));

                    columna.Item().Element(marco => DibujarFicha(marco,
                    [
                        ("Personas matriculadas", lineas.Count.ToString()),
                        ("Notas registradas", conNota.ToString()),
                        ("Aprobadas", aprobados.ToString()),
                        ("Reprobadas", (conNota - aprobados).ToString()),
                        ("Promedio del grupo", promedio.ToString("N2"))
                    ]));

                    columna.Item().PaddingTop(28).Row(fila =>
                    {
                        fila.RelativeItem().Column(bloque =>
                        {
                            bloque.Item().BorderTop(1).BorderColor(ColorLinea).PaddingTop(6)
                                .Text(docente?.NombreCompleto ?? "Persona docente")
                                .FontSize(9).FontColor(ColorTexto);
                            bloque.Item().Text("Firma de la persona docente")
                                .FontSize(8).FontColor(ColorTenue);
                        });

                        fila.ConstantItem(40);

                        fila.RelativeItem().Column(bloque =>
                        {
                            bloque.Item().BorderTop(1).BorderColor(ColorLinea).PaddingTop(6)
                                .Text("Oficina de Registro").FontSize(9).FontColor(ColorTexto);
                            bloque.Item().Text("Sello institucional").FontSize(8).FontColor(ColorTenue);
                        });
                    });
                });

                pagina.Footer().Element(DibujarPie);
            });
        }).GeneratePdf();

        var nombre = $"Acta_{curso.Codigo}_G{grupo.NumeroGrupo:00}_{periodo.Codigo}.pdf";

        return await almacen.GuardarContenidoAsync(
            contenido, nombre, "application/pdf", CategoriaDocumento.ActaNotas, generadaPorUsuarioId);
    }

    // =================================================================================
    //  Piezas visuales compartidas por los dos documentos
    // =================================================================================

    private static void ConfigurarPagina(PageDescriptor pagina)
    {
        pagina.Size(PageSizes.A4);
        pagina.Margin(2, Unit.Centimetre);
        pagina.DefaultTextStyle(estilo => estilo.FontSize(9.5f).FontFamily("Arial").FontColor(ColorTexto));
    }

    private static void DibujarEncabezado(IContainer marco, string titulo, string referencia) =>
        marco.BorderBottom(1).BorderColor(ColorLinea).PaddingBottom(10).Row(fila =>
        {
            fila.RelativeItem().Column(columna =>
            {
                columna.Item().Text("MATRÍCULAUCR")
                    .FontSize(13).Bold().FontColor(ColorAcento).LetterSpacing(0.14f);
                columna.Item().Text("Sistema de Matrícula Universitaria")
                    .FontSize(8).FontColor(ColorTenue);
            });

            fila.RelativeItem().AlignRight().Column(columna =>
            {
                columna.Item().Text(titulo).FontSize(13).FontFamily("Times New Roman");
                columna.Item().Text(referencia).FontSize(8.5f).FontColor(ColorTenue);
            });
        });

    private static void DibujarPie(IContainer marco) =>
        marco.BorderTop(1).BorderColor(ColorLinea).PaddingTop(6).Row(fila =>
        {
            fila.RelativeItem().Text($"Emitido el {DateTime.Now:dd/MM/yyyy HH:mm}")
                .FontSize(7.5f).FontColor(ColorTenue);

            fila.RelativeItem().AlignRight().Text(texto =>
            {
                texto.DefaultTextStyle(estilo => estilo.FontSize(7.5f).FontColor(ColorTenue));
                texto.Span("Página ");
                texto.CurrentPageNumber();
                texto.Span(" de ");
                texto.TotalPages();
            });
        });

    /// <summary>Bloque de pares etiqueta/valor en dos columnas.</summary>
    private static void DibujarFicha(IContainer marco, (string Etiqueta, string Valor)[] datos) =>
        marco.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.ConstantColumn(120);
                columnas.RelativeColumn();
            });

            foreach (var (etiqueta, valor) in datos)
            {
                tabla.Cell().PaddingVertical(3).Text(etiqueta.ToUpperInvariant())
                    .FontSize(7.5f).FontColor(ColorTenue).LetterSpacing(0.08f);
                tabla.Cell().PaddingVertical(3).Text(valor).FontSize(9.5f);
            }
        });

    private static void DibujarTablaCursos(IContainer marco, List<DetalleMatricula> lineas) =>
        marco.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.ConstantColumn(58);
                columnas.RelativeColumn(3);
                columnas.ConstantColumn(38);
                columnas.RelativeColumn(2);
                columnas.RelativeColumn(2);
            });

            tabla.Header(encabezado =>
            {
                foreach (var titulo in new[] { "Código", "Curso", "Créd.", "Horario", "Docente" })
                {
                    encabezado.Cell().Element(EstiloEncabezado).Text(titulo.ToUpperInvariant())
                        .FontSize(7.5f).FontColor(ColorTenue).LetterSpacing(0.08f);
                }
            });

            foreach (var linea in lineas)
            {
                tabla.Cell().Element(EstiloCelda).Text(linea.Grupo?.Curso?.Codigo ?? "—");
                tabla.Cell().Element(EstiloCelda).Text(linea.Grupo?.Curso?.Nombre ?? "—");
                tabla.Cell().Element(EstiloCelda).Text((linea.Grupo?.Curso?.Creditos ?? 0).ToString());
                tabla.Cell().Element(EstiloCelda).Text(linea.Grupo?.Horario ?? "—");
                tabla.Cell().Element(EstiloCelda).Text(linea.Grupo?.Docente?.NombreCompleto ?? "Sin asignar");
            }
        });

    private static void DibujarTablaNotas(IContainer marco, List<DetalleMatricula> lineas) =>
        marco.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.ConstantColumn(28);
                columnas.RelativeColumn(3);
                columnas.RelativeColumn(2);
                columnas.ConstantColumn(46);
                columnas.ConstantColumn(70);
            });

            tabla.Header(encabezado =>
            {
                foreach (var titulo in new[] { "N.º", "Estudiante", "Identificación", "Nota", "Condición" })
                {
                    encabezado.Cell().Element(EstiloEncabezado).Text(titulo.ToUpperInvariant())
                        .FontSize(7.5f).FontColor(ColorTenue).LetterSpacing(0.08f);
                }
            });

            var numero = 1;

            foreach (var linea in lineas)
            {
                var estudiante = linea.Matricula?.Estudiante;

                tabla.Cell().Element(EstiloCelda).Text((numero++).ToString());
                tabla.Cell().Element(EstiloCelda).Text(estudiante?.NombreCompleto ?? "—");
                tabla.Cell().Element(EstiloCelda).Text(estudiante?.Identificacion ?? "—");
                tabla.Cell().Element(EstiloCelda).Text(linea.NotaFinal?.ToString() ?? "—");
                tabla.Cell().Element(EstiloCelda).Text(
                    linea.NotaFinal is null ? "Pendiente" : linea.Aprobado ? "Aprobado" : "Reprobado");
            }
        });

    private static IContainer EstiloEncabezado(IContainer celda) =>
        celda.BorderBottom(1).BorderColor(ColorTexto).PaddingVertical(5).PaddingRight(6);

    private static IContainer EstiloCelda(IContainer celda) =>
        celda.BorderBottom(1).BorderColor(ColorLinea).PaddingVertical(5).PaddingRight(6);

    private static void DibujarTotales(IContainer marco, Matricula matricula, int cantidadCursos) =>
        marco.PaddingTop(4).Row(fila =>
        {
            fila.RelativeItem().Column(columna =>
            {
                columna.Item().Text($"{cantidadCursos} curso(s) matriculado(s)")
                    .FontSize(9).FontColor(ColorTenue);
                columna.Item().Text($"{matricula.TotalCreditos} créditos en total")
                    .FontSize(9).FontColor(ColorTenue);
            });

            fila.ConstantItem(190).BorderTop(1).BorderColor(ColorTexto).PaddingTop(8).Column(columna =>
            {
                columna.Item().Row(interna =>
                {
                    interna.RelativeItem().Text("Monto del periodo").FontSize(8.5f).FontColor(ColorTenue);
                    interna.RelativeItem().AlignRight()
                        .Text(matricula.MontoTotal.ToString("C0")).FontSize(12).Bold();
                });
            });
        });
}
