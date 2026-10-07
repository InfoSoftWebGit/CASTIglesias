using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Circuito de aprobación de operaciones.
    /// </summary>
    /// <remarks>
    /// Un paso, no varios. Las tablas admiten circuitos de varios pasos con
    /// condiciones en JSON, pero aquí se usa un solo paso a propósito: es lo que una
    /// iglesia necesita de verdad (que un gasto grande lo vea alguien más antes de
    /// contabilizarlo) y lo que se puede explicar en una pantalla. El esquema queda
    /// preparado para más pasos el día que haga falta, sin migración.
    ///
    /// La regla que de verdad aporta algo es allow_self_approval: si está en false,
    /// quien registró la operación no puede aprobar la suya. Sin eso, el circuito es
    /// decorativo.
    /// </remarks>
    public class CD_Aprobaciones
    {
        private readonly AppDbContext _context;

        public CD_Aprobaciones(AppDbContext context) => _context = context;

        /// <summary>Tipo de entidad que se aprueba. Coincide con el nombre de la tabla.</summary>
        public const string EntidadOperaciones = "financial_transactions";

        // Estados de una solicitud
        public const string Pendiente = "pending";
        public const string Aprobada = "approved";
        public const string Rechazada = "rejected";

        // Valores de financial_transactions.approval_status
        public const string NoHaceFalta = "not_required";

        /// <summary>Una solicitud con los datos de la operación a la que se refiere.</summary>
        public class SolicitudDTO
        {
            public int id { get; set; }
            public int entity_id { get; set; }
            public string? status { get; set; }
            public DateTime requested_at { get; set; }
            public string? solicitante { get; set; }
            public DateTime? completed_at { get; set; }

            // Datos de la operación, para poder decidir sin salir de la pantalla
            public string? numero { get; set; }
            public DateTime operacion_fecha { get; set; }
            public decimal importe { get; set; }
            public string? concepto { get; set; }
            public string? sede { get; set; }
            public string? descripcion { get; set; }
            public int requested_by { get; set; }

            // Resolución, cuando ya está decidida
            public string? resuelto_por { get; set; }
            public string? comentarios { get; set; }
        }

        // --------------------------------------------------------------------
        // El circuito
        // --------------------------------------------------------------------

        /// <summary>Circuito activo para las operaciones, si hay alguno.</summary>
        public ApprovalWorkflow? CircuitoVigente()
        {
            return _context.ApprovalWorkflows.AsNoTracking()
                .Where(w => w.entity_type == EntidadOperaciones && w.status == "active")
                .OrderByDescending(w => w.version)
                .FirstOrDefault();
        }

        public ApprovalRule? ReglaDe(int idCircuito)
        {
            return _context.ApprovalRules.AsNoTracking()
                .Where(r => r.workflow_id == idCircuito)
                .OrderBy(r => r.step_number)
                .FirstOrDefault();
        }

        /// <summary>
        /// Crea el circuito básico de un paso.
        /// </summary>
        /// <remarks>
        /// Se crea desde la pantalla, no insertando datos en producción: es la misma
        /// decisión que se tomó con las reglas de contabilización. Así cada iglesia
        /// decide si quiere aprobaciones y quién aprueba, y nadie se encuentra un
        /// circuito que no pidió.
        /// </remarks>
        public int CrearCircuitoBasico(string rolAprobador, bool permitirAutoaprobacion,
                                       out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                if (CircuitoVigente() != null)
                {
                    mensaje = "Ya hay un circuito de aprobación activo.";
                    return 0;
                }

                var circuito = new ApprovalWorkflow
                {
                    code = "APROB-OPER",
                    name = "Aprobación de operaciones",
                    entity_type = EntidadOperaciones,
                    version = 1,
                    status = "active",
                    valid_from = DateTime.UtcNow.Date
                };
                _context.ApprovalWorkflows.Add(circuito);
                _context.SaveChanges();   // hace falta el id para la regla

                _context.ApprovalRules.Add(new ApprovalRule
                {
                    workflow_id = circuito.id,
                    step_number = 1,
                    approver_type = "role",
                    approver_reference = rolAprobador,
                    minimum_approvals = 1,
                    allow_self_approval = permitirAutoaprobacion
                });

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Circuito de aprobación creado.";
                return circuito.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al crear el circuito: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>Cambia quién aprueba y si se permite aprobarse a uno mismo.</summary>
        public bool GuardarRegla(int idRegla, string rolAprobador, bool permitirAutoaprobacion,
                                 out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var regla = _context.ApprovalRules.FirstOrDefault(r => r.id == idRegla);
                if (regla == null)
                {
                    mensaje = "La regla no existe.";
                    return false;
                }

                regla.approver_reference = rolAprobador;
                regla.allow_self_approval = permitirAutoaprobacion;

                _context.SaveChanges();
                mensaje = "Circuito guardado.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar el circuito: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>Activa o desactiva el circuito sin borrarlo.</summary>
        public bool CambiarEstadoCircuito(int idCircuito, bool activo, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var circuito = _context.ApprovalWorkflows.FirstOrDefault(w => w.id == idCircuito);
                if (circuito == null)
                {
                    mensaje = "El circuito no existe.";
                    return false;
                }

                circuito.status = activo ? "active" : "inactive";
                _context.SaveChanges();

                mensaje = activo
                    ? "Circuito activado. Las operaciones de conceptos que piden aprobación quedarán pendientes."
                    : "Circuito desactivado. Las operaciones nuevas ya no necesitarán aprobación. " +
                      "Las que estén pendientes siguen pendientes.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al cambiar el circuito: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        // --------------------------------------------------------------------
        // Solicitudes
        // --------------------------------------------------------------------

        /// <summary>
        /// Deja una operación pendiente de aprobación.
        /// </summary>
        /// <remarks>
        /// La solicitud y el cambio de estado de la operación van en la misma
        /// transacción: una operación marcada como pendiente sin solicitud que la
        /// saque de ahí quedaría atascada para siempre, que es justo lo que se quiso
        /// evitar cuando no había pantalla.
        /// </remarks>
        public bool Solicitar(int idCircuito, int idOperacion, int idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                // Una operación no puede tener dos solicitudes pendientes a la vez
                bool yaPendiente = _context.ApprovalRequests
                    .Any(s => s.entity_type == EntidadOperaciones
                           && s.entity_id == idOperacion
                           && s.status == Pendiente);
                if (yaPendiente)
                {
                    mensaje = "Esa operación ya está pendiente de aprobación.";
                    return false;
                }

                _context.ApprovalRequests.Add(new ApprovalRequest
                {
                    workflow_id = idCircuito,
                    entity_type = EntidadOperaciones,
                    entity_id = idOperacion,
                    current_step = 1,
                    status = Pendiente,
                    requested_at = DateTime.UtcNow,
                    requested_by = idUsuario
                });

                var operacion = _context.FinancialTransactions.FirstOrDefault(t => t.id == idOperacion);
                if (operacion == null)
                {
                    mensaje = "La operación no existe.";
                    return false;
                }
                operacion.approval_status = Pendiente;
                operacion.row_version++;

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Operación enviada a aprobación.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al enviar a aprobación: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>Solicitudes con los datos de su operación.</summary>
        public List<SolicitudDTO> Listar(string? estado, int? sedeID)
        {
            var solicitudes = _context.ApprovalRequests.AsNoTracking()
                .Where(s => s.entity_type == EntidadOperaciones);

            if (!string.IsNullOrWhiteSpace(estado))
                solicitudes = solicitudes.Where(s => s.status == estado);

            var filas = solicitudes
                .OrderByDescending(s => s.requested_at)
                .ToList();

            if (filas.Count == 0) return new List<SolicitudDTO>();

            var idsOperacion = filas.Select(s => s.entity_id).ToList();

            var operaciones = _context.FinancialTransactions.AsNoTracking()
                .Where(t => idsOperacion.Contains(t.id))
                .ToList();

            // La sede se filtra por la operación, no por la solicitud: la solicitud no
            // guarda sede, y lo que importa es de qué sede es el dinero.
            if (sedeID.HasValue && sedeID.Value > 0 && sedeID.Value != 1000)
            {
                operaciones = operaciones.Where(t => t.site_id == sedeID.Value).ToList();
                var permitidas = operaciones.Select(t => t.id).ToHashSet();
                filas = filas.Where(s => permitidas.Contains(s.entity_id)).ToList();
            }

            var porId = operaciones.ToDictionary(t => t.id, t => t);

            // IgnoreQueryFilters en los NOMBRES de usuario, a propósito.
            //
            // La tabla de usuarios va filtrada por iglesia, y quien aprueba o registra
            // puede ser el administrador de plataforma, cuya fila está en la iglesia
            // interna de Congrega. Sin esto, justo esa persona aparecía como "#1" en
            // lugar de con su nombre, que es lo contrario de lo que busca un registro
            // de quién hizo qué.
            //
            // No es una fuga de datos: solo se traduce a nombre un id que YA está en
            // una fila de esta iglesia. Nunca se listan usuarios de otras iglesias.
            var usuarios = _context.Usuarios.AsNoTracking().IgnoreQueryFilters()
                .ToDictionary(u => u.ID_usuario, u => u.nombre_usuario ?? "");
            var sedes = _context.Sedes.AsNoTracking()
                .ToDictionary(s => s.ID, s => s.nombre_sede ?? "");
            var conceptos = _context.FinancialConcepts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");

            // La última decisión de cada solicitud, para las ya resueltas
            var decisiones = _context.ApprovalDecisions.AsNoTracking()
                .Where(d => filas.Select(f => f.id).Contains(d.approval_request_id))
                .ToList()
                .GroupBy(d => d.approval_request_id)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.decided_at).First());

            return filas.Select(s =>
            {
                porId.TryGetValue(s.entity_id, out var op);
                decisiones.TryGetValue(s.id, out var decision);

                return new SolicitudDTO
                {
                    id = s.id,
                    entity_id = s.entity_id,
                    status = s.status,
                    requested_at = s.requested_at,
                    requested_by = s.requested_by,
                    solicitante = usuarios.ContainsKey(s.requested_by)
                        ? usuarios[s.requested_by] : "#" + s.requested_by,
                    completed_at = s.completed_at,
                    numero = op?.transaction_number,
                    operacion_fecha = op?.operation_date ?? s.requested_at,
                    importe = op?.total_amount ?? 0,
                    concepto = op != null && conceptos.ContainsKey(op.concept_id)
                        ? conceptos[op.concept_id] : "",
                    sede = op != null && sedes.ContainsKey(op.site_id) ? sedes[op.site_id] : "",
                    descripcion = op?.description,
                    resuelto_por = decision != null && usuarios.ContainsKey(decision.approver_user_id)
                        ? usuarios[decision.approver_user_id] : null,
                    comentarios = decision?.comments
                };
            }).ToList();
        }

        public ApprovalRequest? Obtener(int id)
            => _context.ApprovalRequests.AsNoTracking().FirstOrDefault(s => s.id == id);

        /// <summary>Cuántas solicitudes están pendientes, para el aviso del panel y del menú.</summary>
        public int ContarPendientes()
            => _context.ApprovalRequests
                .Count(s => s.entity_type == EntidadOperaciones && s.status == Pendiente);

        /// <summary>
        /// Aprueba o rechaza una solicitud.
        /// </summary>
        /// <remarks>
        /// La decisión, el cierre de la solicitud y el estado de la operación van en
        /// una sola transacción: si se guardara la decisión y no el estado, la
        /// operación seguiría pendiente con una aprobación ya dada.
        ///
        /// Una operación rechazada NO se borra. Queda con approval_status rejected y su
        /// motivo, porque el rastro de lo que se decidió no gastar también vale.
        /// </remarks>
        public bool Decidir(int idSolicitud, int idUsuario, bool aprobar, string? comentarios,
                            bool permitirAutoaprobacion, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var solicitud = _context.ApprovalRequests.FirstOrDefault(s => s.id == idSolicitud);
                if (solicitud == null)
                {
                    mensaje = "La solicitud no existe.";
                    return false;
                }

                if (solicitud.status != Pendiente)
                {
                    mensaje = "Esa solicitud ya está resuelta.";
                    return false;
                }

                // Quien la pidió no la aprueba, salvo que el circuito lo permita. Es la
                // regla que da sentido a todo el circuito.
                if (!permitirAutoaprobacion && solicitud.requested_by == idUsuario)
                {
                    mensaje = "No puedes aprobar una operación que has registrado tú. " +
                              "Tiene que verla otra persona.";
                    return false;
                }

                if (!aprobar && string.IsNullOrWhiteSpace(comentarios))
                {
                    mensaje = "Para rechazar hay que explicar el motivo.";
                    return false;
                }

                _context.ApprovalDecisions.Add(new ApprovalDecision
                {
                    approval_request_id = solicitud.id,
                    step_number = solicitud.current_step,
                    approver_user_id = idUsuario,
                    decision = aprobar ? Aprobada : Rechazada,
                    comments = comentarios,
                    decided_at = DateTime.UtcNow
                });

                solicitud.status = aprobar ? Aprobada : Rechazada;
                solicitud.completed_at = DateTime.UtcNow;

                var operacion = _context.FinancialTransactions
                    .FirstOrDefault(t => t.id == solicitud.entity_id);
                if (operacion == null)
                {
                    mensaje = "La operación de esa solicitud ya no existe.";
                    return false;
                }

                operacion.approval_status = aprobar ? Aprobada : Rechazada;
                // Una operación rechazada no debe poder contabilizarse por despiste, así
                // que además de la aprobación se marca su estado general.
                if (!aprobar) operacion.status = "rejected";
                operacion.updated_at = DateTime.UtcNow;
                operacion.updated_by = idUsuario;
                operacion.row_version++;

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = aprobar
                    ? "Operación aprobada. Ya se puede contabilizar."
                    : "Operación rechazada.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al decidir: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
