using MapboxMegaservicios.API.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MapboxMegaservicios.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<LugarTrabajo> LugaresTrabajo { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Ubicacion> Ubicaciones { get; set; }
        public DbSet<AlertaGeocerca> AlertasGeocerca { get; set; }
        public DbSet<EstadoAlerta> EstadosAlerta { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<HistorialLugarTrabajo> HistorialLugaresTrabajo { get; set; }
        public DbSet<RegistroAsistencia> RegistrosAsistencia { get; set; }
        public DbSet<JornadaTrabajo> JornadasTrabajo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============ EMPLEADO ============
            modelBuilder.Entity<Empleado>(entity =>
            {
                entity.ToTable("Empleados");

                // Mapear propiedades con nombres diferentes
                entity.Property(e => e.LugarTrabajoActualId)
                      .HasColumnName("LugarTrabajoActualId");

                // Claves foráneas
                entity.HasOne(e => e.LugarTrabajoActual)
                      .WithMany(l => l.Empleados)
                      .HasForeignKey(e => e.LugarTrabajoActualId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Rol)
                      .WithMany(r => r.Empleados)
                      .HasForeignKey(e => e.IdRol)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índices
                entity.HasIndex(e => e.Usuario).IsUnique();
                entity.HasIndex(e => e.Ci).IsUnique();
                entity.HasIndex(e => e.LugarTrabajoActualId);
                entity.HasIndex(e => e.IdRol);
            });

            // ============ LUGAR TRABAJO ============
            modelBuilder.Entity<LugarTrabajo>(entity =>
            {
                entity.ToTable("LugaresTrabajo");

                entity.HasOne(l => l.Departamento)
                      .WithMany(d => d.LugaresTrabajo)
                      .HasForeignKey(l => l.DepartamentoId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índice espacial para Geocerca
                entity.HasIndex(l => l.Geocerca)
                      .HasMethod("GIST");
            });

            // ============ UBICACION ============
            modelBuilder.Entity<Ubicacion>(entity =>
            {
                entity.ToTable("Ubicaciones");

                entity.Property(e => e.EmpleadoId)
                      .HasColumnName("EmpleadoId");

                entity.HasOne(u => u.Empleado)
                      .WithMany(e => e.Ubicaciones)
                      .HasForeignKey(u => u.EmpleadoId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(u => u.EmpleadoId);
                entity.HasIndex(u => u.FechaHora).IsDescending();

                // Índice espacial
                entity.HasIndex(u => u.UbicacionEmp)
                      .HasMethod("GIST");
            });

            // ============ ALERTA GEOCERCA ============
            modelBuilder.Entity<AlertaGeocerca>(entity =>
            {
                entity.ToTable("AlertasGeocerca");

                entity.Property(a => a.EmpleadoId)
                      .HasColumnName("EmpleadoId");

                entity.Property(a => a.EstadoAlertaId)
                      .HasColumnName("EstadoAlertaId");

                entity.HasOne(a => a.Empleado)
                      .WithMany(e => e.Alertas)
                      .HasForeignKey(a => a.EmpleadoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.EstadoAlerta)
                      .WithMany(e => e.Alertas)
                      .HasForeignKey(a => a.EstadoAlertaId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índices
                entity.HasIndex(a => a.EmpleadoId);
                entity.HasIndex(a => a.FechaHora).IsDescending();
                entity.HasIndex(a => a.EstadoAlertaId);
            });

            // ============ HISTORIAL LUGAR TRABAJO ============
            modelBuilder.Entity<HistorialLugarTrabajo>(entity =>
            {
                entity.ToTable("HistorialLugaresTrabajo");

                entity.Property(h => h.EmpleadoId)
                      .HasColumnName("EmpleadoId");

                entity.Property(h => h.LugarTrabajoId)
                      .HasColumnName("LugarTrabajoId");

                entity.HasOne(h => h.Empleado)
                      .WithMany(e => e.HistorialLugaresTrabajo)
                      .HasForeignKey(h => h.EmpleadoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.LugarTrabajo)
                      .WithMany(l => l.Historial)
                      .HasForeignKey(h => h.LugarTrabajoId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(h => h.EmpleadoId);
                entity.HasIndex(h => h.FechaCambio).IsDescending();
            });

            // ============ REGISTRO ASISTENCIA ============
            modelBuilder.Entity<RegistroAsistencia>(entity =>
            {
                entity.ToTable("RegistrosAsistencia");

                entity.Property(r => r.TipoRegistro)
                      .HasConversion<string>()
                      .HasMaxLength(10);

                entity.HasOne(r => r.Empleado)
                      .WithMany()
                      .HasForeignKey(r => r.EmpleadoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Ubicacion)
                      .WithMany()
                      .HasForeignKey(r => r.UbicacionId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Índices
                entity.HasIndex(r => r.EmpleadoId);
                entity.HasIndex(r => r.FechaHora).IsDescending();
                entity.HasIndex(r => r.TipoRegistro);
            });

            // ============ JORNADA TRABAJO ============
            modelBuilder.Entity<JornadaTrabajo>(entity =>
            {
                entity.ToTable("JornadasTrabajo");

                entity.Property(j => j.Estado)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.HasOne(j => j.Empleado)
                      .WithMany()
                      .HasForeignKey(j => j.EmpleadoId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índice único: un empleado solo una jornada por fecha
                entity.HasIndex(j => new { j.EmpleadoId, j.Fecha })
                      .IsUnique();

                entity.HasIndex(j => j.Fecha).IsDescending();
            });

            // ============ DATOS INICIALES ============
            // Configurar datos seed si es necesario
        }
    }
}