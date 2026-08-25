using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProyectoFinal_GuillenJose.Modelos;

namespace ProyectoFinal_GuillenJose.Datos;

/// <summary>
/// Contexto de Entity Framework Core del sistema de matrícula. Hereda de la variante de Identity
/// para que las siete tablas de seguridad y las doce tablas del dominio convivan en la misma
/// base de datos y participen de la misma transacción.
/// </summary>
public class ContextoMatricula(DbContextOptions<ContextoMatricula> opciones)
    : IdentityDbContext<Usuario>(opciones)
{
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<CursoCarrera> CursosCarrera => Set<CursoCarrera>();
    public DbSet<Requisito> Requisitos => Set<Requisito>();
    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<PeriodoAcademico> PeriodosAcademicos => Set<PeriodoAcademico>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<DetalleMatricula> DetallesMatricula => Set<DetalleMatricula>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<Bitacora> Bitacoras => Set<Bitacora>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        base.OnModelCreating(constructor);

        // -----------------------------------------------------------------------------
        // Nombres de tabla en español. Las de Identity conservan el nombre original
        // porque el andamiaje del marco de trabajo las busca por convención.
        // -----------------------------------------------------------------------------
        constructor.Entity<Carrera>().ToTable("Carreras");
        constructor.Entity<Curso>().ToTable("Cursos");
        constructor.Entity<CursoCarrera>().ToTable("CursosCarrera");
        constructor.Entity<Requisito>().ToTable("Requisitos");
        constructor.Entity<Docente>().ToTable("Docentes");
        constructor.Entity<PeriodoAcademico>().ToTable("PeriodosAcademicos");
        constructor.Entity<Grupo>().ToTable("Grupos");
        constructor.Entity<Matricula>().ToTable("Matriculas");
        constructor.Entity<DetalleMatricula>().ToTable("DetallesMatricula");
        constructor.Entity<Documento>().ToTable("Documentos");
        constructor.Entity<Bitacora>().ToTable("Bitacora");
        constructor.Entity<Notificacion>().ToTable("Notificaciones");

        // -----------------------------------------------------------------------------
        // Claves alternas: lo que la universidad considera irrepetible.
        // -----------------------------------------------------------------------------
        constructor.Entity<Carrera>().HasIndex(c => c.Codigo).IsUnique();
        constructor.Entity<Curso>().HasIndex(c => c.Codigo).IsUnique();
        constructor.Entity<PeriodoAcademico>().HasIndex(p => p.Codigo).IsUnique();
        constructor.Entity<Docente>().HasIndex(d => d.Identificacion).IsUnique();
        constructor.Entity<Docente>().HasIndex(d => d.CorreoInstitucional).IsUnique();
        constructor.Entity<Usuario>().HasIndex(u => u.Identificacion).IsUnique();

        // Un curso no se repite dentro del mismo plan de estudios.
        constructor.Entity<CursoCarrera>()
            .HasIndex(cc => new { cc.CarreraId, cc.CursoId }).IsUnique();

        // Un requisito no se declara dos veces para el mismo curso.
        constructor.Entity<Requisito>()
            .HasIndex(r => new { r.CursoId, r.CursoRequisitoId }).IsUnique();

        // La numeración de grupos es única dentro de un curso y un periodo.
        constructor.Entity<Grupo>()
            .HasIndex(g => new { g.CursoId, g.PeriodoAcademicoId, g.NumeroGrupo }).IsUnique();

        // Cada estudiante tiene a lo sumo una matrícula por periodo.
        constructor.Entity<Matricula>()
            .HasIndex(m => new { m.EstudianteId, m.PeriodoAcademicoId }).IsUnique();

        // El comprobante solo existe cuando la matrícula se confirma, de ahí el índice filtrado.
        constructor.Entity<Matricula>()
            .HasIndex(m => m.NumeroComprobante)
            .IsUnique()
            .HasFilter("[NumeroComprobante] IS NOT NULL");

        // Un mismo grupo no se agrega dos veces a la misma matrícula.
        constructor.Entity<DetalleMatricula>()
            .HasIndex(d => new { d.MatriculaId, d.GrupoId }).IsUnique();

        // Índices de consulta frecuente.
        constructor.Entity<Bitacora>().HasIndex(b => b.FechaHora);
        constructor.Entity<Notificacion>().HasIndex(n => new { n.UsuarioId, n.Leida });
        constructor.Entity<Documento>().HasIndex(d => d.HashSha256);

        // -----------------------------------------------------------------------------
        // Relaciones y comportamiento de borrado. Se declaran de forma explícita porque la
        // convención de Entity Framework Core generaría cascadas múltiples que SQL Server
        // rechaza, y porque en un sistema académico el borrado en cadena es indeseable:
        // eliminar una carrera no puede arrastrar las matrículas históricas.
        // -----------------------------------------------------------------------------
        constructor.Entity<CursoCarrera>()
            .HasOne(cc => cc.Carrera).WithMany(c => c.CursosCarrera)
            .HasForeignKey(cc => cc.CarreraId).OnDelete(DeleteBehavior.Cascade);

        constructor.Entity<CursoCarrera>()
            .HasOne(cc => cc.Curso).WithMany(c => c.CursosCarrera)
            .HasForeignKey(cc => cc.CursoId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Requisito>()
            .HasOne(r => r.Curso).WithMany(c => c.Requisitos)
            .HasForeignKey(r => r.CursoId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Requisito>()
            .HasOne(r => r.CursoRequisito).WithMany(c => c.EsRequisitoDe)
            .HasForeignKey(r => r.CursoRequisitoId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Docente>()
            .HasOne(d => d.Usuario).WithOne()
            .HasForeignKey<Docente>(d => d.UsuarioId).OnDelete(DeleteBehavior.SetNull);

        constructor.Entity<Grupo>()
            .HasOne(g => g.Curso).WithMany(c => c.Grupos)
            .HasForeignKey(g => g.CursoId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Grupo>()
            .HasOne(g => g.Docente).WithMany(d => d.Grupos)
            .HasForeignKey(g => g.DocenteId).OnDelete(DeleteBehavior.SetNull);

        constructor.Entity<Grupo>()
            .HasOne(g => g.PeriodoAcademico).WithMany(p => p.Grupos)
            .HasForeignKey(g => g.PeriodoAcademicoId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Grupo>()
            .HasOne(g => g.ProgramaDocumento).WithMany()
            .HasForeignKey(g => g.ProgramaDocumentoId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Matricula>()
            .HasOne(m => m.Estudiante).WithMany(u => u.Matriculas)
            .HasForeignKey(m => m.EstudianteId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Matricula>()
            .HasOne(m => m.PeriodoAcademico).WithMany(p => p.Matriculas)
            .HasForeignKey(m => m.PeriodoAcademicoId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Matricula>()
            .HasOne(m => m.ComprobanteDocumento).WithMany()
            .HasForeignKey(m => m.ComprobanteDocumentoId).OnDelete(DeleteBehavior.Restrict);

        // Las líneas sí desaparecen con su cabecera: no tienen sentido por separado.
        constructor.Entity<DetalleMatricula>()
            .HasOne(d => d.Matricula).WithMany(m => m.Detalles)
            .HasForeignKey(d => d.MatriculaId).OnDelete(DeleteBehavior.Cascade);

        constructor.Entity<DetalleMatricula>()
            .HasOne(d => d.Grupo).WithMany(g => g.Detalles)
            .HasForeignKey(d => d.GrupoId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Usuario>()
            .HasOne(u => u.Carrera).WithMany(c => c.Estudiantes)
            .HasForeignKey(u => u.CarreraId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Usuario>()
            .HasOne(u => u.FotografiaDocumento).WithMany()
            .HasForeignKey(u => u.FotografiaDocumentoId).OnDelete(DeleteBehavior.Restrict);

        constructor.Entity<Documento>()
            .HasOne(d => d.PropietarioUsuario).WithMany()
            .HasForeignKey(d => d.PropietarioUsuarioId).OnDelete(DeleteBehavior.Restrict);

        // La bitácora sobrevive a la cuenta que originó el movimiento.
        constructor.Entity<Bitacora>()
            .HasOne(b => b.Usuario).WithMany()
            .HasForeignKey(b => b.UsuarioId).OnDelete(DeleteBehavior.SetNull);

        constructor.Entity<Notificacion>()
            .HasOne(n => n.Usuario).WithMany(u => u.Notificaciones)
            .HasForeignKey(n => n.UsuarioId).OnDelete(DeleteBehavior.Cascade);

        // -----------------------------------------------------------------------------
        // Precisión monetaria: el dinero nunca se guarda como punto flotante.
        // -----------------------------------------------------------------------------
        constructor.Entity<Matricula>().Property(m => m.MontoTotal).HasPrecision(12, 2);
    }
}
