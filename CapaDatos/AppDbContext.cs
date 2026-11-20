using CapaEntidad;
using CapaEntidad.Financiero;
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
        public DbSet<HistorialEstadoMiembro> HistorialEstadosMiembro { get; set; }
        public DbSet<HistorialPertenencia> HistorialPertenencias { get; set; }
        public DbSet<ObjetivoAnual> ObjetivosAnuales { get; set; }
        public DbSet<ConfigInformes> ConfigInformes { get; set; }
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
        public DbSet<SeguimientoZonaLider> SeguimientoZonasLider { get; set; }
        public DbSet<SeguimientoAsignacion> SeguimientoAsignaciones { get; set; }
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
        public DbSet<CalendarioServicio> CalendariosServicio { get; set; }
        public DbSet<CalendarioServicioAsignacion> CalendarioServicioAsignaciones { get; set; }

        // Plataforma (no filtradas por iglesia: las usa el administrador de Congrega)
        public DbSet<Iglesia> Iglesias { get; set; }
        public DbSet<AccesoPlataforma> AccesosPlataforma { get; set; }
        public DbSet<UsuarioSede> UsuarioSedes { get; set; }

        // Inscripciones a los webinars de presentación. Van aquí y no en el
        // bloque por iglesia porque las rellena gente de fuera, desde la página
        // pública, cuando todavía no hay ninguna iglesia asociada.
        public DbSet<WebinarInscripcion> WebinarInscripciones { get; set; }

        // Artículos del blog público. Globales por el mismo motivo: son de
        // Congrega y se leen desde la página pública, sin sesión.
        public DbSet<BlogArticulo> BlogArticulos { get; set; }

        // ── Módulo financiero ────────────────────────────────────────────────
        // Las 48 tablas del área financiera. Las 46 que tienen organization_id
        // implementan ITieneIglesia, así que el filtro global por iglesia se les
        // aplica solo en OnModelCreating, sin listarlas aquí.
        // Las dos restantes (Permission y RolePermission) son catálogo común a
        // todas las iglesias.
        // AccountingTemplate y AccountingTemplateAccount SÍ son por iglesia: las
        // plantillas del plan contable las crea cada iglesia (ver esas entidades).
        public DbSet<FiscalYear> FiscalYears { get; set; }
        public DbSet<AccountingPeriod> AccountingPeriods { get; set; }
        public DbSet<DocumentSequence> DocumentSequences { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<LedgerAccount> LedgerAccounts { get; set; }
        public DbSet<AccountingTemplate> AccountingTemplates { get; set; }
        public DbSet<AccountingTemplateAccount> AccountingTemplateAccounts { get; set; }
        public DbSet<Fund> Funds { get; set; }
        public DbSet<FundSiteLink> FundSiteLinks { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<FinancialConcept> FinancialConcepts { get; set; }
        public DbSet<PostingRuleSet> PostingRuleSets { get; set; }
        public DbSet<PostingRule> PostingRules { get; set; }
        public DbSet<Party> Parties { get; set; }
        public DbSet<DonorProfile> DonorProfiles { get; set; }
        public DbSet<TreasuryAccount> TreasuryAccounts { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalEntryLine> JournalEntryLines { get; set; }
        public DbSet<FinancialTransaction> FinancialTransactions { get; set; }
        public DbSet<FinancialTransactionLine> FinancialTransactionLines { get; set; }
        public DbSet<ReversalRequest> ReversalRequests { get; set; }
        public DbSet<TreasuryMovement> TreasuryMovements { get; set; }
        public DbSet<CashSession> CashSessions { get; set; }
        public DbSet<CashCountLine> CashCountLines { get; set; }
        public DbSet<FundMovement> FundMovements { get; set; }
        public DbSet<Transfer> Transfers { get; set; }
        public DbSet<Payable> Payables { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentAllocation> PaymentAllocations { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<BudgetLine> BudgetLines { get; set; }
        public DbSet<BudgetCommitment> BudgetCommitments { get; set; }
        public DbSet<ApprovalWorkflow> ApprovalWorkflows { get; set; }
        public DbSet<ApprovalRule> ApprovalRules { get; set; }
        public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
        public DbSet<ApprovalDecision> ApprovalDecisions { get; set; }
        public DbSet<BankStatement> BankStatements { get; set; }
        public DbSet<BankStatementLine> BankStatementLines { get; set; }
        public DbSet<ReconciliationMatch> ReconciliationMatches { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentLink> DocumentLinks { get; set; }
        public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
        public DbSet<AuditEvent> AuditEvents { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

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

            // Las tres tablas de enlace del módulo financiero no tienen columna id:
            // su clave es la pareja que relacionan. EF no lo adivina y, sin esto, el
            // modelo ni siquiera se construye.
            modelBuilder.Entity<RolePermission>()
                .HasKey(e => new { e.role_id, e.permission_id });
            modelBuilder.Entity<FundSiteLink>()
                .HasKey(e => new { e.fund_id, e.site_id });
            modelBuilder.Entity<PaymentAllocation>()
                .HasKey(e => new { e.payment_id, e.payable_id });

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
            PrepararHistorialPertenencia();
            var altas = PrepararHistorialEstados();
            AsignarIglesia();
            if (altas.Count == 0) return base.SaveChanges(acceptAllChangesOnSuccess);

            // Hay altas: su fila de historial necesita el id que da la base al
            // insertar, así que se guarda en dos pasos dentro de una transacción
            // (o de la que ya hubiera abierta) para que no quede uno sin el otro.
            using var transaccion = Database.CurrentTransaction is null ? Database.BeginTransaction() : null;
            var filas = base.SaveChanges(acceptAllChangesOnSuccess);
            AnadirAltas(altas);
            filas += base.SaveChanges(acceptAllChangesOnSuccess);
            transaccion?.Commit();
            return filas;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            PrepararHistorialPertenencia();
            var altas = PrepararHistorialEstados();
            AsignarIglesia();
            if (altas.Count == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            await using var transaccion = Database.CurrentTransaction is null
                ? await Database.BeginTransactionAsync(cancellationToken) : null;
            var filas = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            AnadirAltas(altas);
            filas += await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            if (transaccion is not null) await transaccion.CommitAsync(cancellationToken);
            return filas;
        }

        /// <summary>
        /// Deja en el guardado una fila de historial por cada cambio de estado, baja,
        /// reactivación o borrado de un congregante, y devuelve las altas pendientes.
        /// </summary>
        /// <remarks>
        /// Va aquí y no en CD_Miembros porque el estado se cambia desde muchos sitios
        /// (formularios de cada listado, avanzar, retroceder...). Al hacerlo en el
        /// guardado, el historial no depende de que cada pantalla se acuerde y entra
        /// en la misma transacción que el cambio.
        ///
        /// Se compara valor original con valor actual, no IsModified: varias
        /// pantallas marcan la entidad entera como modificada aunque el estado no
        /// cambie, y eso no debe dejar rastro.
        ///
        /// Las altas se devuelven en vez de añadirse porque el congregante aún no
        /// tiene id (ver SaveChanges). El resto se añade ya, antes de AsignarIglesia,
        /// para que reciban la iglesia de la sesión como cualquier otra alta.
        /// </remarks>
        private List<(Miembro Miembro, DateTime Fecha)> PrepararHistorialEstados()
        {
            var ahora = DateTime.Now;
            var altas = new List<(Miembro, DateTime)>();
            var nuevas = new List<HistorialEstadoMiembro>();

            // ToList: se añaden entidades al ChangeTracker después de recorrerlo
            foreach (var entrada in ChangeTracker.Entries<Miembro>().ToList())
            {
                var m = entrada.Entity;
                switch (entrada.State)
                {
                    case EntityState.Added:
                        // La fecha de llegada que escriben en el formulario dice mejor
                        // cuándo vino la persona que el momento en que alguien la apuntó.
                        // Una fecha futura es un error de tecleo: se usa la de hoy.
                        altas.Add((m, m.fecha_llegada_iglesia is { } llegada && llegada <= ahora ? llegada : ahora));
                        break;

                    case EntityState.Modified:
                        var estadoAntes = entrada.Property(x => x.estado).OriginalValue;
                        if (!string.Equals(estadoAntes, m.estado, StringComparison.Ordinal))
                            nuevas.Add(FilaHistorial(m, HistorialEstadoMiembro.TipoCambioEstado, estadoAntes, m.estado, ahora));

                        bool eraBaja = entrada.Property(x => x.miembro_activo).OriginalValue == "No";
                        bool esBaja = m.miembro_activo == "No";
                        if (eraBaja != esBaja)
                            nuevas.Add(FilaHistorial(m,
                                esBaja ? HistorialEstadoMiembro.TipoBaja : HistorialEstadoMiembro.TipoReactivacion,
                                m.estado, m.estado, ahora));
                        break;

                    case EntityState.Deleted:
                        nuevas.Add(FilaHistorial(m, HistorialEstadoMiembro.TipoEliminado,
                            entrada.Property(x => x.estado).OriginalValue, null, ahora));
                        break;
                }
            }

            if (nuevas.Count > 0) HistorialEstadosMiembro.AddRange(nuevas);
            return altas;
        }

        /// <summary>
        /// Deja en el guardado las entradas y salidas de zonas, grupos, ministerios y
        /// liderazgo (ver HistorialPertenencia).
        /// </summary>
        /// <remarks>
        /// Se compara lo que había con lo que queda, no fila a fila: al editar un
        /// congregante, SincronizarZGM borra todas sus filas y las vuelve a crear en el
        /// mismo guardado. Mirando filas saldría "sale de la zona 3 / entra en la zona
        /// 3" cada vez; comparando los conjuntos, solo queda lo que cambió de verdad.
        /// Una fila puede llevar zona, grupo y ministerio a la vez: cuenta como tres
        /// pertenencias.
        /// </remarks>
        private void PrepararHistorialPertenencia()
        {
            var cambios = new Dictionary<(int Miembro, string Tipo, int Destino), (int Saldo, int Sede)>();
            void Sumar(int miembro, int sede, string tipo, int destino, int signo)
            {
                if (destino == 0 && tipo != HistorialPertenencia.TipoLider) return;
                var clave = (miembro, tipo, destino);
                cambios[clave] = cambios.TryGetValue(clave, out var v) ? (v.Saldo + signo, sede) : (signo, sede);
            }
            void SumarFila(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Miembro_zona_grupo_ministerio> e, bool original, int signo)
            {
                int Valor(string p) => (int)(original ? e.Property(p).OriginalValue! : e.Property(p).CurrentValue!);
                int miembro = Valor(nameof(Miembro_zona_grupo_ministerio.ID_miembro));
                int sede = Valor(nameof(Miembro_zona_grupo_ministerio.ID_sede));
                Sumar(miembro, sede, HistorialPertenencia.TipoZona, Valor(nameof(Miembro_zona_grupo_ministerio.ID_zona)), signo);
                Sumar(miembro, sede, HistorialPertenencia.TipoGrupo, Valor(nameof(Miembro_zona_grupo_ministerio.ID_grupo)), signo);
                Sumar(miembro, sede, HistorialPertenencia.TipoMinisterio, Valor(nameof(Miembro_zona_grupo_ministerio.ID_ministerio)), signo);
            }

            foreach (var e in ChangeTracker.Entries<Miembro_zona_grupo_ministerio>().ToList())
            {
                switch (e.State)
                {
                    case EntityState.Added: SumarFila(e, false, +1); break;
                    case EntityState.Deleted: SumarFila(e, true, -1); break;
                    case EntityState.Modified: SumarFila(e, true, -1); SumarFila(e, false, +1); break;
                }
            }
            foreach (var e in ChangeTracker.Entries<Lider>().ToList())
            {
                if (e.State == EntityState.Added) Sumar(e.Entity.ID_miembro, e.Entity.ID_sede, HistorialPertenencia.TipoLider, 0, +1);
                else if (e.State == EntityState.Deleted)
                    Sumar((int)e.Property(nameof(Lider.ID_miembro)).OriginalValue!, (int)e.Property(nameof(Lider.ID_sede)).OriginalValue!,
                          HistorialPertenencia.TipoLider, 0, -1);
            }

            var ahora = DateTime.Now;
            int? usuario = _contextoIglesia?.IdUsuario is > 0 and var u ? u : null;
            foreach (var ((miembro, tipo, destino), (saldo, sede)) in cambios.Where(c => c.Value.Saldo != 0))
            {
                HistorialPertenencias.Add(new HistorialPertenencia
                {
                    id_miembro = miembro,
                    ID_sede = sede,
                    tipo = tipo,
                    id_destino = destino,
                    evento = saldo > 0 ? HistorialPertenencia.Entra : HistorialPertenencia.Sale,
                    fecha = ahora,
                    id_usuario = usuario,
                    origen = HistorialEstadoMiembro.OrigenApp
                });
            }
        }

        private void AnadirAltas(List<(Miembro Miembro, DateTime Fecha)> altas)
        {
            foreach (var (m, fecha) in altas)
            {
                HistorialEstadosMiembro.Add(FilaHistorial(m, HistorialEstadoMiembro.TipoAlta, null, m.estado, fecha));
                if (m.miembro_activo == "No")
                    HistorialEstadosMiembro.Add(FilaHistorial(m, HistorialEstadoMiembro.TipoBaja, m.estado, m.estado, DateTime.Now));
            }
        }

        private HistorialEstadoMiembro FilaHistorial(Miembro m, string tipo, string? anterior, string? nuevo, DateTime fecha) => new()
        {
            ID_iglesia = m.ID_iglesia,
            ID_sede = m.id_sede,
            id_miembro = m.id_miembro,
            tipo_evento = tipo,
            estado_anterior = anterior,
            estado_nuevo = nuevo,
            fecha_cambio = fecha,
            id_usuario = _contextoIglesia?.IdUsuario is > 0 and var u ? u : null,
            origen = HistorialEstadoMiembro.OrigenApp
        };

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
