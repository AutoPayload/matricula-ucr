using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoFinal_GuillenJose.Controladores;

/// <summary>
/// Direcciones heredadas del Avance 2. Aquel avance nombraba los controladores y las acciones
/// en inglés, seguía la convención del andamiaje de Visual Studio y montaba el registro sobre
/// las páginas de Identity UI. La entrega final renombró todo al español y reorganizó el
/// dominio, de modo que ninguna de esas direcciones existiría hoy.
///
/// Este controlador las mantiene vivas: cada dirección del avance responde con una redirección
/// a su equivalente actual. Sirve para que las capturas, el video y los enlaces de la entrega
/// anterior sigan llevando a alguna parte, y para que el salto entre una entrega y otra se
/// pueda recorrer en el navegador y no solo leerse en la documentación.
///
/// Todas las rutas se declaran por atributo y solo cubren direcciones que hoy darían 404. Las
/// que ya existen con nombre en español —«Carreras/Index», por ejemplo— se dejan fuera a
/// propósito: duplicarlas con una ruta por atributo provocaría una coincidencia ambigua en
/// tiempo de ejecución. La redirección es temporal y no permanente, porque se trata de una
/// ayuda de continuidad y no de un cambio definitivo de dirección pública.
///
/// La autorización la sigue aplicando el destino, no este controlador: quien llegue por una
/// dirección antigua a una pantalla restringida termina igual en la de acceso denegado.
/// </summary>
[AllowAnonymous]
public class CompatibilidadController : Controller
{
    // =================================================================================
    //  Portada y páginas de servicio. HomeController pasó a llamarse InicioController.
    // =================================================================================

    [HttpGet("Home")]
    [HttpGet("Home/Index")]
    public IActionResult Portada() => RedirectToAction("Index", "Inicio");

    /// <summary>La página de privacidad del andamiaje se convirtió en «Acerca del sistema».</summary>
    [HttpGet("Home/Privacy")]
    public IActionResult Privacidad() => RedirectToAction("Acerca", "Inicio");

    [HttpGet("Home/Error")]
    public IActionResult Error() => RedirectToAction("Error", "Inicio");

    // =================================================================================
    //  Matrícula del estudiantado. El controlador era «Matriculas», en plural.
    // =================================================================================

    [HttpGet("Matriculas")]
    [HttpGet("Matriculas/MisCursos")]
    public IActionResult MisCursos() => RedirectToAction("MisCursos", "Matricula");

    /// <summary>
    /// En el Avance 2 se matriculaba un curso de un tirón desde su ficha. Hoy el curso se
    /// ofrece en grupos y lo que se matricula es el grupo, así que la dirección antigua lleva
    /// a la ficha del curso y explica el cambio en lugar de intentar adivinar el grupo.
    /// </summary>
    [HttpGet("Matriculas/Matricular")]
    [HttpPost("Matriculas/Matricular")]
    public IActionResult Matricular(int cursoId)
    {
        TempData["Aviso"] = "La matrícula ahora se hace por grupo: elija el horario y la persona " +
                            "docente que le convengan y agréguelo a su matrícula del periodo.";

        return cursoId > 0
            ? RedirectToAction("Detalle", "Cursos", new { id = cursoId })
            : RedirectToAction("Disponibles", "Cursos");
    }

    // =================================================================================
    //  Mantenimientos de la oficina de registro. Las acciones del andamiaje en inglés
    //  (Index, Details, Create, Edit, Delete) pasaron a Index, Detalles, Crear, Editar
    //  y Eliminar, y el mantenimiento de cursos se separó en CursosAdmin para dejar
    //  «Cursos» al catálogo del estudiantado.
    // =================================================================================

    [HttpGet("Cursos/Index")]
    public IActionResult CursosIndice() => RedirectToAction("Index", "CursosAdmin");

    [HttpGet("Cursos/Details/{id:int}")]
    public IActionResult CursosDetalle(int id) => RedirectToAction("Detalles", "CursosAdmin", new { id });

    [HttpGet("Cursos/Create")]
    public IActionResult CursosCrear() => RedirectToAction("Crear", "CursosAdmin");

    [HttpGet("Cursos/Edit/{id:int}")]
    public IActionResult CursosEditar(int id) => RedirectToAction("Editar", "CursosAdmin", new { id });

    [HttpGet("Cursos/Delete/{id:int}")]
    public IActionResult CursosEliminar(int id) => RedirectToAction("Eliminar", "CursosAdmin", new { id });

    [HttpGet("Carreras/Details/{id:int}")]
    public IActionResult CarrerasDetalle(int id) => RedirectToAction("Detalles", "Carreras", new { id });

    [HttpGet("Carreras/Create")]
    public IActionResult CarrerasCrear() => RedirectToAction("Crear", "Carreras");

    [HttpGet("Carreras/Edit/{id:int}")]
    public IActionResult CarrerasEditar(int id) => RedirectToAction("Editar", "Carreras", new { id });

    [HttpGet("Carreras/Delete/{id:int}")]
    public IActionResult CarrerasEliminar(int id) => RedirectToAction("Eliminar", "Carreras", new { id });

    [HttpGet("Docentes/Details/{id:int}")]
    public IActionResult DocentesDetalle(int id) => RedirectToAction("Detalles", "Docentes", new { id });

    [HttpGet("Docentes/Create")]
    public IActionResult DocentesCrear() => RedirectToAction("Crear", "Docentes");

    [HttpGet("Docentes/Edit/{id:int}")]
    public IActionResult DocentesEditar(int id) => RedirectToAction("Editar", "Docentes", new { id });

    [HttpGet("Docentes/Delete/{id:int}")]
    public IActionResult DocentesEliminar(int id) => RedirectToAction("Eliminar", "Docentes", new { id });

    // =================================================================================
    //  Páginas de Identity UI. El Avance 2 usaba el área «Identity» con Razor Pages; la
    //  entrega final escribió su propio CuentaController para tener las pantallas en
    //  español y con la identidad visual del sitio.
    // =================================================================================

    [HttpGet("Identity/Account/Register")]
    public IActionResult Registro() => RedirectToAction("Registro", "Cuenta");

    [HttpGet("Identity/Account/Login")]
    public IActionResult Ingreso() => RedirectToAction("Ingreso", "Cuenta");

    /// <summary>
    /// El cierre de sesión del Avance 2 era un enlace hacia una página del área Identity.
    /// Hoy es una acción POST del menú de la persona usuaria, así que redirigirla directamente
    /// devolvería un 405: la dirección antigua lleva a la portada y explica dónde está ahora.
    /// </summary>
    [HttpGet("Identity/Account/Logout")]
    [HttpPost("Identity/Account/Logout")]
    public IActionResult Salir()
    {
        TempData["Aviso"] = "La sesión se cierra desde el menú de su nombre, en la barra superior.";
        return RedirectToAction("Index", "Inicio");
    }

    [HttpGet("Identity/Account/Manage/{*resto}")]
    public IActionResult Perfil() => RedirectToAction("Perfil", "Cuenta");
}
