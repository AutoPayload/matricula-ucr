using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Datos;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Componentes;

/// <summary>
/// Componente propio número dos: la ficha académica de una persona estudiante. Resume créditos
/// aprobados, cursos llevados y promedio ponderado. Se reutiliza en la portada del estudiantado,
/// en el expediente y en la pantalla de matrícula sin repetir el cálculo en cada vista.
/// </summary>
public class ResumenAcademicoViewComponent(ContextoMatricula contexto) : ViewComponent
{
    private const string Vista = "~/Vistas/Compartidas/Componentes/ResumenAcademico.cshtml";

    public async Task<IViewComponentResult> InvokeAsync(string estudianteId)
    {
        var lineas = await contexto.DetallesMatricula
            .AsNoTracking()
            .Include(d => d.Grupo!).ThenInclude(g => g.Curso)
            .Where(d => d.Matricula!.EstudianteId == estudianteId
                     && d.Matricula.Estado == EstadoMatricula.Confirmada
                     && d.Estado == EstadoDetalleMatricula.Activo)
            .ToListAsync();

        var calificadas = lineas.Where(l => l.NotaFinal.HasValue).ToList();
        var aprobadas = calificadas.Where(l => l.Aprobado).ToList();

        // El promedio se pondera por créditos: un curso de cuatro créditos pesa el doble
        // que uno de dos, tal como lo calcula la oficina de registro.
        var creditosCalificados = calificadas.Sum(l => l.Grupo?.Curso?.Creditos ?? 0);
        var sumaPonderada = calificadas.Sum(l => (l.NotaFinal ?? 0) * (l.Grupo?.Curso?.Creditos ?? 0));

        var carrera = await contexto.Users
            .AsNoTracking()
            .Where(u => u.Id == estudianteId)
            .Select(u => u.Carrera)
            .FirstOrDefaultAsync();

        var modelo = new FichaAcademica
        {
            NombreCarrera = carrera?.Nombre ?? "Sin carrera asignada",
            CreditosPlan = carrera?.CreditosPlan ?? 0,
            CursosLlevados = lineas.Count,
            CursosAprobados = aprobadas.Count,
            CreditosAprobados = aprobadas.Sum(l => l.Grupo?.Curso?.Creditos ?? 0),
            PromedioPonderado = creditosCalificados == 0
                ? 0
                : Math.Round(sumaPonderada / (double)creditosCalificados, 1),
            CursosPendientesDeNota = lineas.Count - calificadas.Count
        };

        return View(Vista, modelo);
    }

    /// <summary>Cifras del expediente que consume la vista del componente.</summary>
    public class FichaAcademica
    {
        public string NombreCarrera { get; init; } = string.Empty;
        public int CreditosPlan { get; init; }
        public int CursosLlevados { get; init; }
        public int CursosAprobados { get; init; }
        public int CreditosAprobados { get; init; }
        public double PromedioPonderado { get; init; }
        public int CursosPendientesDeNota { get; init; }

        /// <summary>Porcentaje del plan de estudios ya cubierto.</summary>
        public int PorcentajeAvance => CreditosPlan == 0
            ? 0
            : Math.Min(100, (int)Math.Round(CreditosAprobados * 100d / CreditosPlan));
    }
}
