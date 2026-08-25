using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoFinal_GuillenJose.Datos.Migraciones
{
    /// <summary>
    /// Tercera migración del proyecto y la que separa el Avance 2 de la entrega final.
    ///
    /// El avance modelaba la matrícula como una fila que unía a una persona con un curso en un
    /// periodo escrito a mano. Esta migración lleva el esquema al modelo académico real: el curso
    /// se ofrece en grupos, el grupo es lo que se matricula, el periodo es una entidad con su
    /// ventana de fechas, la matrícula pasa a ser una transacción de cabecera y líneas, y
    /// aparecen los requisitos, el plan de estudios, los documentos, la bitácora y los avisos.
    ///
    /// Las columnas que sí conservan significado se renombran para no perder el dato
    /// (ApplicationUserId pasa a EstudianteId y FechaMatricula a FechaCreacion). Las que dejaron
    /// de tener sentido se eliminan y se crean de nuevo, en lugar de reaprovecharse por tener el
    /// mismo tipo, que es lo que propuso el andamiaje automático.
    /// </summary>
    public partial class EvolucionAlModeloAcademico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cursos_Carreras_CarreraId",
                table: "Cursos");

            migrationBuilder.DropForeignKey(
                name: "FK_Cursos_Docentes_DocenteId",
                table: "Cursos");

            migrationBuilder.DropForeignKey(
                name: "FK_Matriculas_AspNetUsers_ApplicationUserId",
                table: "Matriculas");

            migrationBuilder.DropForeignKey(
                name: "FK_Matriculas_Cursos_CursoId",
                table: "Matriculas");

            migrationBuilder.DropIndex(
                name: "IX_Matriculas_ApplicationUserId_CursoId_Periodo",
                table: "Matriculas");

            migrationBuilder.DropIndex(
                name: "IX_Matriculas_CursoId",
                table: "Matriculas");

            migrationBuilder.DropIndex(
                name: "IX_Cursos_CarreraId",
                table: "Cursos");

            migrationBuilder.DropIndex(
                name: "IX_Cursos_DocenteId",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "Periodo",
                table: "Matriculas");

            migrationBuilder.DropColumn(
                name: "DocenteId",
                table: "Cursos");

            migrationBuilder.RenameColumn(
                name: "FechaMatricula",
                table: "Matriculas",
                newName: "FechaCreacion");

            // La matrícula deja de apuntar a un curso: pasa a ser la cabecera del periodo, y son
            // sus líneas, en DetallesMatricula, las que apuntan a cada grupo. La columna se
            // elimina en lugar de renombrarse porque el dato que guardaba no tiene equivalente.
            migrationBuilder.DropColumn(
                name: "CursoId",
                table: "Matriculas");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "Matriculas",
                newName: "EstudianteId");

            // El curso deja de conocer el cupo y la carrera. El cupo se muda al grupo, que es la
            // oferta concreta de un periodo, y la carrera se muda a CursosCarrera, de modo que un
            // curso de servicio como Matemática General sirva a varios planes sin duplicarse.
            migrationBuilder.DropColumn(
                name: "Cupos",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "CarreraId",
                table: "Cursos");

            migrationBuilder.AddColumn<int>(
                name: "ComprobanteDocumentoId",
                table: "Matriculas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Matriculas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaConfirmacion",
                table: "Matriculas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTotal",
                table: "Matriculas",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NumeroComprobante",
                table: "Matriculas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeriodoAcademicoId",
                table: "Matriculas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCreditos",
                table: "Matriculas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Docentes",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Especialidad",
                table: "Docentes",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Docentes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Apellidos",
                table: "Docentes",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorreoInstitucional",
                table: "Docentes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Identificacion",
                table: "Docentes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Docentes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "Docentes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Cursos",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Cursos",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Cursos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Cursos",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HorasSemanales",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Modalidad",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Carreras",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Carreras",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<bool>(
                name: "Activa",
                table: "Carreras",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Carreras",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreditosPlan",
                table: "Carreras",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistro",
                table: "Carreras",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TituloOtorgado",
                table: "Carreras",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Apellidos",
                table: "AspNetUsers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNacimiento",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistro",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FotografiaDocumentoId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Identificacion",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "AspNetUsers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateTable(
                name: "Bitacora",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DireccionIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bitacora", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bitacora_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CursosCarrera",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarreraId = table.Column<int>(type: "int", nullable: false),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    Ciclo = table.Column<int>(type: "int", nullable: false),
                    EsObligatorio = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CursosCarrera", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CursosCarrera_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CursosCarrera_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NombreAlmacenado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoContenido = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    PropietarioUsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_AspNetUsers_PropietarioUsuarioId",
                        column: x => x.PropietarioUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Enlace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Leida = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InicioMatricula = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinMatricula = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MaximoCreditos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosAcademicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Requisitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    CursoRequisitoId = table.Column<int>(type: "int", nullable: false),
                    NotaMinima = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisitos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requisitos_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requisitos_Cursos_CursoRequisitoId",
                        column: x => x.CursoRequisitoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Grupos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    DocenteId = table.Column<int>(type: "int", nullable: true),
                    PeriodoAcademicoId = table.Column<int>(type: "int", nullable: false),
                    NumeroGrupo = table.Column<int>(type: "int", nullable: false),
                    Horario = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Aula = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CupoMaximo = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    ProgramaDocumentoId = table.Column<int>(type: "int", nullable: true),
                    ActaCerrada = table.Column<bool>(type: "bit", nullable: false),
                    FechaCierreActa = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grupos_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Grupos_Docentes_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Grupos_Documentos_ProgramaDocumentoId",
                        column: x => x.ProgramaDocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Grupos_PeriodosAcademicos_PeriodoAcademicoId",
                        column: x => x.PeriodoAcademicoId,
                        principalTable: "PeriodosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesMatricula",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatriculaId = table.Column<int>(type: "int", nullable: false),
                    GrupoId = table.Column<int>(type: "int", nullable: false),
                    FechaInclusion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    NotaFinal = table.Column<int>(type: "int", nullable: true),
                    FechaRegistroNota = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesMatricula", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesMatricula_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesMatricula_Matriculas_MatriculaId",
                        column: x => x.MatriculaId,
                        principalTable: "Matriculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_ComprobanteDocumentoId",
                table: "Matriculas",
                column: "ComprobanteDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_EstudianteId_PeriodoAcademicoId",
                table: "Matriculas",
                columns: new[] { "EstudianteId", "PeriodoAcademicoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_NumeroComprobante",
                table: "Matriculas",
                column: "NumeroComprobante",
                unique: true,
                filter: "[NumeroComprobante] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_PeriodoAcademicoId",
                table: "Matriculas",
                column: "PeriodoAcademicoId");

            migrationBuilder.CreateIndex(
                name: "IX_Docentes_CorreoInstitucional",
                table: "Docentes",
                column: "CorreoInstitucional",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Docentes_Identificacion",
                table: "Docentes",
                column: "Identificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Docentes_UsuarioId",
                table: "Docentes",
                column: "UsuarioId",
                unique: true,
                filter: "[UsuarioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Carreras_Codigo",
                table: "Carreras",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FotografiaDocumentoId",
                table: "AspNetUsers",
                column: "FotografiaDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Identificacion",
                table: "AspNetUsers",
                column: "Identificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bitacora_FechaHora",
                table: "Bitacora",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_Bitacora_UsuarioId",
                table: "Bitacora",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CursosCarrera_CarreraId_CursoId",
                table: "CursosCarrera",
                columns: new[] { "CarreraId", "CursoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CursosCarrera_CursoId",
                table: "CursosCarrera",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesMatricula_GrupoId",
                table: "DetallesMatricula",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesMatricula_MatriculaId_GrupoId",
                table: "DetallesMatricula",
                columns: new[] { "MatriculaId", "GrupoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_HashSha256",
                table: "Documentos",
                column: "HashSha256");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_PropietarioUsuarioId",
                table: "Documentos",
                column: "PropietarioUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_CursoId_PeriodoAcademicoId_NumeroGrupo",
                table: "Grupos",
                columns: new[] { "CursoId", "PeriodoAcademicoId", "NumeroGrupo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_DocenteId",
                table: "Grupos",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_PeriodoAcademicoId",
                table: "Grupos",
                column: "PeriodoAcademicoId");

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_ProgramaDocumentoId",
                table: "Grupos",
                column: "ProgramaDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId_Leida",
                table: "Notificaciones",
                columns: new[] { "UsuarioId", "Leida" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosAcademicos_Codigo",
                table: "PeriodosAcademicos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requisitos_CursoId_CursoRequisitoId",
                table: "Requisitos",
                columns: new[] { "CursoId", "CursoRequisitoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requisitos_CursoRequisitoId",
                table: "Requisitos",
                column: "CursoRequisitoId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Documentos_FotografiaDocumentoId",
                table: "AspNetUsers",
                column: "FotografiaDocumentoId",
                principalTable: "Documentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Docentes_AspNetUsers_UsuarioId",
                table: "Docentes",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Matriculas_AspNetUsers_EstudianteId",
                table: "Matriculas",
                column: "EstudianteId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matriculas_Documentos_ComprobanteDocumentoId",
                table: "Matriculas",
                column: "ComprobanteDocumentoId",
                principalTable: "Documentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matriculas_PeriodosAcademicos_PeriodoAcademicoId",
                table: "Matriculas",
                column: "PeriodoAcademicoId",
                principalTable: "PeriodosAcademicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Documentos_FotografiaDocumentoId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Docentes_AspNetUsers_UsuarioId",
                table: "Docentes");

            migrationBuilder.DropForeignKey(
                name: "FK_Matriculas_AspNetUsers_EstudianteId",
                table: "Matriculas");

            migrationBuilder.DropForeignKey(
                name: "FK_Matriculas_Documentos_ComprobanteDocumentoId",
                table: "Matriculas");

            migrationBuilder.DropForeignKey(
                name: "FK_Matriculas_PeriodosAcademicos_PeriodoAcademicoId",
                table: "Matriculas");

            migrationBuilder.DropTable(
                name: "Bitacora");

            migrationBuilder.DropTable(
                name: "CursosCarrera");

            migrationBuilder.DropTable(
                name: "DetallesMatricula");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "Requisitos");

            migrationBuilder.DropTable(
                name: "Grupos");

            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "PeriodosAcademicos");

            migrationBuilder.DropIndex(
                name: "IX_Matriculas_ComprobanteDocumentoId",
                table: "Matriculas");

            migrationBuilder.DropIndex(
                name: "IX_Matriculas_EstudianteId_PeriodoAcademicoId",
                table: "Matriculas");

            migrationBuilder.DropIndex(
                name: "IX_Matriculas_NumeroComprobante",
                table: "Matriculas");

            migrationBuilder.DropIndex(
                name: "IX_Matriculas_PeriodoAcademicoId",
                table: "Matriculas");

            migrationBuilder.DropIndex(
                name: "IX_Docentes_CorreoInstitucional",
                table: "Docentes");

            migrationBuilder.DropIndex(
                name: "IX_Docentes_Identificacion",
                table: "Docentes");

            migrationBuilder.DropIndex(
                name: "IX_Docentes_UsuarioId",
                table: "Docentes");

            migrationBuilder.DropIndex(
                name: "IX_Carreras_Codigo",
                table: "Carreras");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_FotografiaDocumentoId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Identificacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ComprobanteDocumentoId",
                table: "Matriculas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Matriculas");

            migrationBuilder.DropColumn(
                name: "FechaConfirmacion",
                table: "Matriculas");

            migrationBuilder.DropColumn(
                name: "MontoTotal",
                table: "Matriculas");

            migrationBuilder.DropColumn(
                name: "NumeroComprobante",
                table: "Matriculas");

            migrationBuilder.DropColumn(
                name: "PeriodoAcademicoId",
                table: "Matriculas");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Docentes");

            migrationBuilder.DropColumn(
                name: "Apellidos",
                table: "Docentes");

            migrationBuilder.DropColumn(
                name: "CorreoInstitucional",
                table: "Docentes");

            migrationBuilder.DropColumn(
                name: "Identificacion",
                table: "Docentes");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Docentes");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Docentes");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "Activa",
                table: "Carreras");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Carreras");

            migrationBuilder.DropColumn(
                name: "CreditosPlan",
                table: "Carreras");

            migrationBuilder.DropColumn(
                name: "FechaRegistro",
                table: "Carreras");

            migrationBuilder.DropColumn(
                name: "TituloOtorgado",
                table: "Carreras");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Apellidos",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FechaRegistro",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FotografiaDocumentoId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Identificacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotalCreditos",
                table: "Matriculas");

            migrationBuilder.AddColumn<int>(
                name: "CursoId",
                table: "Matriculas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.RenameColumn(
                name: "FechaCreacion",
                table: "Matriculas",
                newName: "FechaMatricula");

            migrationBuilder.RenameColumn(
                name: "EstudianteId",
                table: "Matriculas",
                newName: "ApplicationUserId");

            migrationBuilder.DropColumn(
                name: "Modalidad",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "HorasSemanales",
                table: "Cursos");

            migrationBuilder.AddColumn<int>(
                name: "Cupos",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CarreraId",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Periodo",
                table: "Matriculas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Docentes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<string>(
                name: "Especialidad",
                table: "Docentes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Cursos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Cursos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12);

            migrationBuilder.AddColumn<int>(
                name: "DocenteId",
                table: "Cursos",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Carreras",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Carreras",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(600)",
                oldMaxLength: 600);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_ApplicationUserId_CursoId_Periodo",
                table: "Matriculas",
                columns: new[] { "ApplicationUserId", "CursoId", "Periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_CursoId",
                table: "Matriculas",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cursos_CarreraId",
                table: "Cursos",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_Cursos_DocenteId",
                table: "Cursos",
                column: "DocenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cursos_Carreras_CarreraId",
                table: "Cursos",
                column: "CarreraId",
                principalTable: "Carreras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cursos_Docentes_DocenteId",
                table: "Cursos",
                column: "DocenteId",
                principalTable: "Docentes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Matriculas_AspNetUsers_ApplicationUserId",
                table: "Matriculas",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matriculas_Cursos_CursoId",
                table: "Matriculas",
                column: "CursoId",
                principalTable: "Cursos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
