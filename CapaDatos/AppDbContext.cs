using CapaEntidad;
using Microsoft.EntityFrameworkCore;
namespace CapaDatos
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
             : base(options)
        {
        }
        public DbSet<Miembro> Miembros { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Curso> Curso { get; set; }
        public DbSet<Familia> Familia { get; set; }
        public DbSet<Grupos> Grupos { get; set; }
        public DbSet<Municipio> Municipio { get; set; }
        public DbSet<Provincia> Provincia { get; set; }
        public DbSet<Permisos> Permisos { get; set; }
        public DbSet<Zona> Zona { get; set; }
        public DbSet<Diezmo> Diezmo { get; set; }
        public DbSet<Concepto> Concepto { get; set; }
        public DbSet<Sedes> Sedes { get; set; }
        public DbSet<Asistencia_culto> Asistencia_Culto { get; set; }
        public DbSet<Ministerio> Ministerios { get; set; }

        public DbSet<ConfigDiezmo> ConfigDiezmo { get; set; }
        public DbSet<Miembro_zona_grupo_ministerio> Miembros_Zona_Grupo_Ministerio { get; set; }
        public DbSet<Pais> Paises { get; set; }
        public DbSet<Seguimiento> Seguimientos { get; set; }
        public DbSet<DetalleSeguimiento> DetallesSeguimiento { get; set; }
        public DbSet<Lider> Lideres { get; set; }
        public DbSet<Matrimonio> Matrimonios { get; set; }
        public DbSet<VistaUsuariosPermisos> VistaPermisosUsuarios { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VistaUsuariosPermisos>()
                .HasNoKey();

            modelBuilder.Entity<Permisos>(entity =>
            {
                entity.HasIndex(p => p.ID_usuario)
                      .IsUnique();

                entity.HasOne<Usuario>()
                      .WithOne()
                      .HasForeignKey<Permisos>(p => p.ID_usuario);
            });

            base.OnModelCreating(modelBuilder);
        }

    }
}
