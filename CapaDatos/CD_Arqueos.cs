using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Arqueos de caja: contar el efectivo y dejar constancia de la diferencia.
    /// </summary>
    /// <remarks>
    /// La regla que da sentido a esta pantalla: la diferencia NO se oculta ni se
    /// "ajusta" en silencio. Si faltan 12,40 €, el arqueo lo dice, pide una explicación
    /// y lo deja escrito. Una caja que siempre cuadra a la primera es una caja en la
    /// que nadie ha contado nada.
    ///
    /// Solo puede haber UNA sesión abierta por caja a la vez. Si no, dos personas
    /// contarían el mismo dinero y el saldo de apertura del día siguiente saldría de
    /// una de las dos, sin saber de cuál.
    /// </remarks>
    public class CD_Arqueos
    {
        private readonly AppDbContext _context;

        public CD_Arqueos(AppDbContext context) => _context = context;

        public const string Abierta = "open";
        public const string PendienteValidar = "pending_validation";
        public const string Cerrada = "closed";

        /// <summary>Un arqueo con los nombres ya resueltos.</summary>
        public class ArqueoDTO
        {
            public int id { get; set; }
            public int treasury_account_id { get; set; }
            public string? caja { get; set; }
            public string? sede { get; set; }
            public DateTime opened_at { get; set; }
            public string? abierta_por { get; set; }
            public decimal opening_balance { get; set; }
            public decimal? expected_balance { get; set; }
            public decimal? counted_balance { get; set; }
            public decimal? difference_amount { get; set; }
            public string? status { get; set; }
            public DateTime? closed_at { get; set; }
            public string? cerrada_por { get; set; }
            public string? validador { get; set; }
            public string? notes { get; set; }
        }

        public List<ArqueoDTO> Listar(int? sedeID, string? estado, int tope = 200)
        {
            var sesiones = _context.CashSessions.AsNoTracking().AsQueryable();

            if (sedeID.HasValue && sedeID.Value > 0 && sedeID.Value != 1000)
                sesiones = sesiones.Where(s => s.site_id == sedeID.Value);
            if (!string.IsNullOrWhiteSpace(estado))
                sesiones = sesiones.Where(s => s.status == estado);

            var filas = sesiones.OrderByDescending(s => s.opened_at).Take(tope).ToList();
            if (filas.Count == 0) return new List<ArqueoDTO>();

            var cajas = _context.TreasuryAccounts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");
            var sedes = _context.Sedes.AsNoTracking()
                .ToDictionary(s => s.ID, s => s.nombre_sede ?? "");
            // Sin filtro de iglesia: quien abre o valida puede ser el administrador de
            // plataforma, cuya fila está en la iglesia interna de Congrega.
            var usuarios = _context.Usuarios.AsNoTracking().IgnoreQueryFilters()
                .ToDictionary(u => u.ID_usuario, u => u.nombre_usuario ?? "");

            string? Nombre(int? id) => id.HasValue
                ? (usuarios.ContainsKey(id.Value) ? usuarios[id.Value] : "#" + id.Value)
                : null;

            return filas.Select(s => new ArqueoDTO
            {
                id = s.id,
                treasury_account_id = s.treasury_account_id,
                caja = cajas.ContainsKey(s.treasury_account_id) ? cajas[s.treasury_account_id] : "",
                sede = sedes.ContainsKey(s.site_id) ? sedes[s.site_id] : "",
                opened_at = s.opened_at,
                abierta_por = Nombre(s.opened_by),
                opening_balance = s.opening_balance,
                expected_balance = s.expected_balance,
                counted_balance = s.counted_balance,
                difference_amount = s.difference_amount,
                status = s.status,
                closed_at = s.closed_at,
                cerrada_por = Nombre(s.closed_by),
                validador = Nombre(s.second_validator_id),
                notes = s.notes
            }).ToList();
        }

        public CashSession? Obtener(int id)
            => _context.CashSessions.AsNoTracking().FirstOrDefault(s => s.id == id);

        /// <summary>La sesión abierta de una caja, si la hay.</summary>
        public CashSession? AbiertaDe(int idCaja)
            => _context.CashSessions.AsNoTracking()
                .FirstOrDefault(s => s.treasury_account_id == idCaja && s.status == Abierta);

        public List<CashCountLine> LineasDe(int idSesion)
            => _context.CashCountLines.AsNoTracking()
                .Where(l => l.cash_session_id == idSesion)
                .OrderByDescending(l => l.denomination)
                .ToList();

        /// <summary>
        /// Saldo que debería haber en la caja ahora mismo, según la contabilidad.
        /// </summary>
        /// <remarks>
        /// Sale de los movimientos de tesorería, igual que el saldo del panel: es el
        /// mismo número en los dos sitios porque sale del mismo sitio. Si se calculara
        /// aparte, un día dirían cosas distintas y nadie sabría a cuál creer.
        /// </remarks>
        public decimal SaldoContable(int idCaja)
            => _context.TreasuryMovements.AsNoTracking()
                .Where(m => m.treasury_account_id == idCaja)
                .Sum(m => (decimal?)m.signed_amount) ?? 0m;

        /// <summary>Abre una sesión de arqueo.</summary>
        public int Abrir(int idCaja, int idSede, int idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (AbiertaDe(idCaja) != null)
                {
                    mensaje = "Esa caja ya tiene un arqueo abierto. Ciérralo antes de abrir otro.";
                    return 0;
                }

                var sesion = new CashSession
                {
                    site_id = idSede,
                    treasury_account_id = idCaja,
                    opened_at = DateTime.UtcNow,
                    opened_by = idUsuario,
                    // El saldo de apertura es el contable del momento: contra eso se
                    // comparará lo que se cuente al cerrar.
                    opening_balance = SaldoContable(idCaja),
                    status = Abierta,
                    row_version = 1
                };

                _context.CashSessions.Add(sesion);
                _context.SaveChanges();

                mensaje = "Arqueo abierto.";
                return sesion.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al abrir el arqueo: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Guarda el recuento y cierra el arqueo.
        /// </summary>
        /// <remarks>
        /// El recuento y el cierre van en la misma transacción: un arqueo cerrado sin
        /// su desglose no se podría revisar después, que es justo para lo que sirve.
        ///
        /// No se genera ningún asiento de la diferencia aquí. Ajustar la contabilidad
        /// por una diferencia de caja es una decisión contable, no automática: se
        /// registra como un gasto o un ingreso desde su pantalla, con su concepto, y
        /// así queda explicado. Un apunte automático escondería el problema.
        /// </remarks>
        public bool Cerrar(int idSesion, List<CashCountLine> lineas, int idUsuario,
                           int? idValidador, string? notas, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var sesion = _context.CashSessions.FirstOrDefault(s => s.id == idSesion);
                if (sesion == null)
                {
                    mensaje = "El arqueo no existe.";
                    return false;
                }
                if (sesion.status != Abierta)
                {
                    mensaje = "Este arqueo ya está cerrado.";
                    return false;
                }

                // Se borra lo que hubiera: guardar sin cerrar puede hacerse varias veces
                var anteriores = _context.CashCountLines
                    .Where(l => l.cash_session_id == idSesion).ToList();
                _context.CashCountLines.RemoveRange(anteriores);

                decimal contado = 0;
                foreach (var linea in lineas.Where(l => l.quantity > 0))
                {
                    linea.cash_session_id = idSesion;
                    linea.line_amount = linea.denomination * linea.quantity;
                    contado += linea.line_amount;
                    _context.CashCountLines.Add(linea);
                }

                decimal esperado = SaldoContable(sesion.treasury_account_id);

                sesion.expected_balance = esperado;
                sesion.counted_balance = contado;
                sesion.difference_amount = contado - esperado;
                sesion.second_validator_id = idValidador;
                sesion.notes = notas;
                sesion.closed_at = DateTime.UtcNow;
                sesion.closed_by = idUsuario;
                // Con validador queda pendiente de que esa persona lo vea; sin él, cerrado.
                sesion.status = idValidador.HasValue ? PendienteValidar : Cerrada;
                sesion.row_version++;

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = sesion.difference_amount == 0
                    ? "Arqueo cerrado. La caja cuadra."
                    : $"Arqueo cerrado con una diferencia de {sesion.difference_amount:N2}.";
                return true;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al cerrar el arqueo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>El segundo validador da el visto bueno.</summary>
        public bool Validar(int idSesion, int idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var sesion = _context.CashSessions.FirstOrDefault(s => s.id == idSesion);
                if (sesion == null)
                {
                    mensaje = "El arqueo no existe.";
                    return false;
                }
                if (sesion.status != PendienteValidar)
                {
                    mensaje = "Este arqueo no está pendiente de validación.";
                    return false;
                }
                // Quien cerró no se valida a sí mismo: si no, el segundo par de ojos
                // que justifica todo esto no existe.
                if (sesion.closed_by == idUsuario)
                {
                    mensaje = "No puedes validar un arqueo que has cerrado tú.";
                    return false;
                }

                sesion.status = Cerrada;
                sesion.second_validator_id = idUsuario;
                sesion.row_version++;

                _context.SaveChanges();
                mensaje = "Arqueo validado.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al validar: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
