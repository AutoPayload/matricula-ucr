using Microsoft.AspNetCore.Mvc;
using ProyectoFinal_GuillenJose.Modelos;
using ProyectoFinal_GuillenJose.Servicios;

namespace ProyectoFinal_GuillenJose.Componentes;

/// <summary>
/// Componente propio número tres: el estado de la ventana de matrícula. Traduce las fechas del
/// periodo a un mensaje entendible y calcula los días que quedan, que es la información que la
/// persona estudiante realmente necesita antes de decidir si arma su matrícula hoy o mañana.
/// </summary>
public class AvisoPeriodoViewComponent(ServicioMatricula matricula) : ViewComponent
{
    private const string Vista = "~/Vistas/Compartidas/Componentes/AvisoPeriodo.cshtml";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var periodo = await matricula.ObtenerPeriodoVigenteAsync();

        if (periodo is null)
        {
            return View(Vista, new EstadoVentana
            {
                Titulo = "Sin periodo configurado",
                Mensaje = "La oficina de registro todavía no ha publicado el calendario académico.",
                Tono = "atencion"
            });
        }

        var hoy = DateTime.Today;
        var abierta = periodo.AceptaMatricula(DateTime.Now);

        if (abierta)
        {
            var diasRestantes = (periodo.FinMatricula.Date - hoy).Days;

            return View(Vista, new EstadoVentana
            {
                Periodo = periodo,
                Titulo = $"Matrícula abierta · {periodo.Nombre}",
                Mensaje = diasRestantes switch
                {
                    0 => "Hoy es el último día para confirmar su matrícula.",
                    1 => "Queda un día para confirmar su matrícula.",
                    _ => $"Quedan {diasRestantes} días para confirmar su matrícula."
                },
                DiasRestantes = diasRestantes,
                Abierta = true,
                Tono = diasRestantes <= 3 ? "atencion" : "acento"
            });
        }

        var mensaje = hoy < periodo.InicioMatricula.Date
            ? $"La matrícula abre el {periodo.InicioMatricula:dd 'de' MMMM 'de' yyyy}."
            : $"La matrícula cerró el {periodo.FinMatricula:dd 'de' MMMM 'de' yyyy}.";

        return View(Vista, new EstadoVentana
        {
            Periodo = periodo,
            Titulo = $"Matrícula cerrada · {periodo.Nombre}",
            Mensaje = mensaje,
            Tono = "atencion"
        });
    }

    /// <summary>Estado de la ventana que consume la vista del componente.</summary>
    public class EstadoVentana
    {
        public PeriodoAcademico? Periodo { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Mensaje { get; init; } = string.Empty;
        public string Tono { get; init; } = "acento";
        public bool Abierta { get; init; }
        public int DiasRestantes { get; init; }
    }
}
