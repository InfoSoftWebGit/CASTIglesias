using CapaEntidad;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CapaDatos
{
    public class AppDbContext : DbContext
    {
        private readonly IContextoIglesia? _contextoIglesia;

        public AppDbContext(DbContextOptions<AppDbContext> options, IContextoIglesia contextoIglesia)
             : base(options)
        {
            _contextoIglesia = contextoIglesia;
        }

        /// <summary>
        /// Iglesia de la petición actual. EF Core lee esta propiedad en cada consulta
        /// (no al construir el modelo), así que el filtro global cambia con la sesión.
        /// </summary>
        public int IdIglesiaActual => _contextoIglesia?.IdIglesia ?? 0;

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
        public DbSet<ConfigJovenes> ConfigJovenes { get; set; }
        public DbSet<Gasto> Gastos { get; set; }
        public DbSet<DetallePago> DetallePagos { get; set; }
        public DbSet<VistaUsuariosPermisos> VistaPermisosUsuarios { get; set; }
        public DbSet<Culto> Cultos { get; set; }
        public DbSet<BloqueCulto> BloquesCulto { get; set; }
        public DbSet<RequerimientoCulto> RequerimientosCulto { get; set; }
        public DbSet<Sala> Salas { get; set; }
        public DbSet<EventoCalendario> EventosCalendario { get; set; }

        // Plataforma (no filtradas por iglesia: las usa el administrador de Congrega)
        public DbSet<Iglesia> Iglesias { get; set; }
        public DbSet<AccesoPlataforma> AccesosPlataforma { get; set; }

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

            // Filtro global por iglesia en todas las entidades que implementan ITieneIglesia.
            // Se recorre el modelo en vez de listar entidades a mano para que una tabla
            // nueva quede protegida con solo implementar la interfaz.
            var metodo = typeof(AppDbContext).GetMethod(nameof(AplicarFiltroIglesia),
                BindingFlags.NonPublic | BindingFlags.Instance)!;

            foreach (var tipo in modelBuilder.Model.GetEntityTypes())
            {
                // Solo la raíz de cada jerarquía admite filtro en EF Core
                if (tipo.BaseType == null && typeof(ITieneIglesia).IsAssignableFrom(tipo.ClrType))
                {
                    metodo.MakeGenericMethod(tipo.ClrType).Invoke(this, new object[] { modelBuilder });
                }
            }

            base.OnModelCreating(modelBuilder);
        }

        private void AplicarFiltroIglesia<T>(ModelBuilder modelBuilder) where T : class, ITieneIglesia
        {
            // EF.Property en vez de e.ID_iglesia: evita que EF tenga que traducir el
            // acceso a través de la interfaz. IdIglesiaActual se evalúa en cada consulta.
            modelBuilder.Entity<T>().HasQueryFilter(e => EF.Property<int>(e, "ID_iglesia") == IdIglesiaActual);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AsignarIglesia();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AsignarIglesia();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// Garantiza la regla "o tiene iglesia o nada" antes de escribir.
        /// </summary>
        /// <remarks>
        /// - Alta: si la entidad llega sin iglesia (lo normal, el JSON de las vistas no la
        ///   trae), se le asigna la de la sesión. Así un usuario creado por Salem queda en
        ///   Salem y no en la iglesia 1 como ocurría con el DEFAULT de la BBDD.
        /// - Alta sin sesión: se rechaza. La BBDD también lo impediría (FK a iglesias),
        ///   pero así el error es claro.
        /// - Edición: ID_iglesia nunca se envía en el UPDATE, aunque la entidad venga de
        ///   un DTO con 0, para que ningún formulario pueda mover un registro de iglesia.
        /// </remarks>
        private void AsignarIglesia()
        {
            foreach (var entrada in ChangeTracker.Entries<ITieneIglesia>())
            {
                if (entrada.State == EntityState.Added)
                {
                    if (entrada.Entity.ID_iglesia == 0)
                    {
                        if (IdIglesiaActual == 0)
                            throw new InvalidOperationException(
                                $"No se puede guardar {entrada.Metadata.ClrType.Name} sin iglesia: no hay iglesia activa en la sesión.");

                        entrada.Entity.ID_iglesia = IdIglesiaActual;
                    }
                }
                else if (entrada.State == EntityState.Modified)
                {
                    entrada.Property(nameof(ITieneIglesia.ID_iglesia)).IsModified = false;
                }
            }
        }
    }
}
