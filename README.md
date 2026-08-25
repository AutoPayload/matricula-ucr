# MatrículaUCR — Sistema de Matrícula Universitaria

Proyecto final de Programación Avanzada en C# · Universidad Fidélitas · II Cuatrimestre 2026
José Andrés Guillén Agüero · 118330875

Aplicación web construida con **ASP.NET Core 10 (MVC)**, **Entity Framework Core** sobre
**SQL Server LocalDB** y **ASP.NET Identity**. Reúne el plan de estudios, la oferta de grupos y la
transacción de matrícula en un solo lugar, y aplica las reglas académicas antes de aceptar
cualquier movimiento.

---

## Puesta en marcha

    dotnet restore
    dotnet run --project ProyectoFinal_GuillenJose

La aplicación queda en `https://localhost:7150` y en `http://localhost:5150`. En el primer arranque
crea la base de datos, aplica las migraciones y carga los datos de demostración.

Cuentas de prueba en `../Documentacion/Instrucciones_de_uso.md`, y también dentro del sistema, en
**Acerca del sistema**.

---

## Estructura de la solución

    ProyectoFinal_GuillenJose/          Aplicación web
    ├─ Configuracion/                   Opciones tipadas, roles, políticas y esquemas
    ├─ Modelos/                         Entidades del dominio y enumeraciones
    ├─ ModelosVista/                    Modelos de vista y objetos de transferencia
    ├─ Datos/                           Contexto de EF Core, migraciones y sembrador
    ├─ Servicios/                       Reglas de negocio, almacén, tokens, PDF y bitácora
    ├─ Controladores/                   Controladores MVC por rol
    ├─ Api/                             Servicios REST consumidos por AJAX e integraciones
    ├─ Componentes/                     View components propios
    ├─ AyudantesEtiqueta/               Tag helpers propios
    ├─ Validaciones/                    Atributos de validación propios
    ├─ Vistas/                          Vistas Razor (ubicación personalizada)
    ├─ Almacenamiento/                  Archivos cargados y generados (fuera de wwwroot)
    └─ wwwroot/                         Hoja de estilos y cliente asíncrono propios

    ProyectoFinal_GuillenJose.Pruebas/  47 pruebas con xUnit y SQLite en memoria

Todo el código está en español: espacios de nombres, clases, métodos, variables, rutas y
comentarios. Las vistas Razor residen en `Vistas/` gracias a una configuración propia de
`RazorViewEngineOptions` en `Program.cs`.

---

## Decisiones de diseño

**Las reglas viven en un servicio, no en los controladores.** `ServicioMatricula.AgregarGrupoAsync`
aplica las ocho reglas de aceptación. La vista Razor y la API consultan el mismo servicio, así que
el catálogo y la matrícula nunca se contradicen, y las reglas se prueban sin levantar el servidor.

**Tres mecanismos de autenticación conviviendo.** Cookie de Identity para el navegador, token JWT
para la API interna y las peticiones asíncronas, y clave de aplicación para integraciones entre
servidores.

**Borrado conservador.** Ninguna entidad con historial se elimina: se desactiva o se cancela. Solo
las líneas de matrícula desaparecen con su cabecera.

**Sin marcos de trabajo de interfaz.** La hoja de estilos, la retícula, los componentes y el
cliente asíncrono son propios. No hay Bootstrap ni bibliotecas de gráficos.

---

## Comandos útiles

| Tarea | Comando |
|---|---|
| Compilar la solución | `dotnet build ProyectoFinal_GuillenJose.slnx` |
| Ejecutar las pruebas | `dotnet test ProyectoFinal_GuillenJose.slnx` |
| Recrear la base de datos | `dotnet ef database drop --force --project ProyectoFinal_GuillenJose` |
| Agregar una migración | `dotnet ef migrations add Nombre -o Datos/Migraciones --project ProyectoFinal_GuillenJose` |
| Exportar el esquema | `dotnet ef migrations script --idempotent -o esquema.sql --project ProyectoFinal_GuillenJose` |

---

## Paquetes

| Paquete | Para qué |
|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Cuentas, roles y contraseñas |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Token de la API interna |
| `Microsoft.EntityFrameworkCore.SqlServer` | Acceso a datos |
| `QuestPDF` | Comprobante de matrícula y acta de notas |
| `xunit` y `Microsoft.EntityFrameworkCore.Sqlite` | Pruebas automatizadas |
