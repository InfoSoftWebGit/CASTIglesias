using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Presupuestos y su seguimiento contra el gasto real.
    /// </summary>
    /// <remarks>
    /// Decisión D7: el presupuesto AVISA cuando se supera, pero no bloquea nada.
    /// Por eso aquí no hay ninguna comprobación que impida gastar: solo se calcula
    /// lo presupuestado, lo ejecutado y la diferencia.
    /// </remarks>
    public class CD_Presupuestos
    {
        private readonly AppDbContext _context;

        public CD_Presupuestos(AppDbContext context) => _context = context;

        public const string Borrador = "draft";
        public const string Activo = "active";
        public const string Cerrado = "closed";

        /// <summary>Una línea de presupuesto con lo gastado de verdad al lado.</summary>
        public class SeguimientoLineaDTO
        {
            public int id { get; set; }
            public int? ledger_account_id { get; set; }
            public string? cuenta { get; set; }
            public int? fund_id { get; set; }
            public string? fondo { get; set; }
            public int? site_id { get; set; }
            public string? sede { get; set; }
            public int? ministry_id { get; set; }
            public string? ministerio { get; set; }
            public decimal presupuestado { get; set; }
            public decimal ejecutado { get; set; }
            public string? notas { get; set; }

            public decimal Disponible => presupuestado - ejecutado;

            /// <summary>Porcentaje consumido. Sin presupuesto no hay porcentaje que dar.</summary>
            public decimal PorcentajeConsumido =>
                presupuestado == 0 ? 0 : Math.Round(ejecutado / presupuestado * 100, 1);

            public bool Superado => ejecutado > presupuestado;
        }

        public List<Budget> Listar()
            => _context.Budgets.AsNoTracking()
                .OrderByDescending(b => b.fiscal_year_id).ThenBy(b => b.code).ToList();

        public Budget? Obtener(int id)
            => _context.Budgets.AsNoTracking().FirstOrDefault(b => b.id == id);

        public List<BudgetLine> LineasDe(int idPresupuesto)
            => _context.BudgetLines.AsNoTracking()
                .Where(l => l.budget_id == idPresupuesto).ToList();

        public int GuardarPresupuesto(Budget presupuesto, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (presupuesto.id == 0)
                {
                    presupuesto.created_at = DateTime.UtcNow;
                    if (presupuesto.version_number <= 0) presupuesto.version_number = 1;
                    _context.Budgets.Add(presupuesto);
                }
                else
                {
                    var actual = _context.Budgets.FirstOrDefault(b => b.id == presupuesto.id);
                    if (actual == null)
                    {
                        mensaje = "El presupuesto no existe.";
                        return 0;
                    }
                    actual.code = presupuesto.code;
                    actual.name = presupuesto.name;
                    actual.fiscal_year_id = presupuesto.fiscal_year_id;
                    actual.scope_type = presupuesto.scope_type;
                    actual.status = presupuesto.status;
                }

                _context.SaveChanges();
                mensaje = "Presupuesto guardado.";
                return presupuesto.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar el presupuesto: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool GuardarLinea(BudgetLine linea, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (linea.id == 0)
                {
                    _context.BudgetLines.Add(linea);
                }
                else
                {
                    var actual = _context.BudgetLines.FirstOrDefault(l => l.id == linea.id);
                    if (actual == null)
                    {
                        mensaje = "La línea no existe.";
                        return false;
                    }
                    actual.ledger_account_id = linea.ledger_account_id;
                    actual.fund_id = linea.fund_id;
                    actual.site_id = linea.site_id;
                    actual.ministry_id = linea.ministry_id;
                    actual.amount = linea.amount;
                    actual.notes = linea.notes;
                }

                _context.SaveChanges();
                mensaje = "Línea guardada.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar la línea: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool EliminarLinea(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var linea = _context.BudgetLines.FirstOrDefault(l => l.id == id);
                if (linea == null)
                {
                    mensaje = "La línea no existe.";
                    return false;
                }
                _context.BudgetLines.Remove(linea);
                _context.SaveChanges();
                mensaje = "Línea eliminada.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar la línea: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool EliminarPresupuesto(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var lineas = _context.BudgetLines.Where(l => l.budget_id == id).ToList();
                _context.BudgetLines.RemoveRange(lineas);

                var presupuesto = _context.Budgets.FirstOrDefault(b => b.id == id);
                if (presupuesto == null)
                {
                    mensaje = "El presupuesto no existe.";
                    return false;
                }
                _context.Budgets.Remove(presupuesto);
                _context.SaveChanges();
                mensaje = "Presupuesto eliminado.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el presupuesto: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>
        /// Compara cada línea del presupuesto con lo realmente gastado.
        /// </summary>
        /// <remarks>
        /// Lo ejecutado se mide sobre los asientos CONTABILIZADOS del ejercicio, no
        /// sobre las operaciones registradas: lo que aún no está contabilizado no ha
        /// consumido presupuesto todavía.
        ///
        /// Cada línea se compara con el gasto que coincide en sus dimensiones. Una
        /// línea que solo indica cuenta consume todo lo de esa cuenta; si además
        /// indica fondo o sede, solo lo de ese fondo o esa sede. Así una misma cuenta
        /// puede tener presupuesto separado por sede sin duplicar nada.
        /// </remarks>
        public List<SeguimientoLineaDTO> Seguimiento(int idPresupuesto, DateTime desde, DateTime hasta)
        {
            var lineas = LineasDe(idPresupuesto);
            if (lineas.Count == 0) return new List<SeguimientoLineaDTO>();

            // Solo cuentas de GASTO, y esto no es un detalle menor.
            //
            // Un presupuesto mide lo que se gasta. Si se sumaran todas las líneas del
            // asiento, cada gasto aportaría su cuenta de gasto al Debe y su cuenta de
            // caja al Haber, que se anulan: una línea de presupuesto por fondo o por
            // ministerio, sin cuenta concreta, daría siempre cero ejecutado. Se veía
            // como "no se ha gastado nada" en vez de como un fallo, que es lo peor
            // que puede hacer un informe.
            var cuentasGasto = _context.LedgerAccounts.AsNoTracking()
                .Where(c => c.account_type == "expense")
                .Select(c => c.id)
                .ToList();

            // Las líneas de asiento del periodo, ya en memoria: son pocas comparadas
            // con el coste de una consulta por cada línea de presupuesto.
            var movimientos = (from l in _context.JournalEntryLines.AsNoTracking()
                               join a in _context.JournalEntries.AsNoTracking()
                                   on l.journal_entry_id equals a.id
                               where a.posting_date >= desde && a.posting_date <= hasta
                                  && cuentasGasto.Contains(l.ledger_account_id)
                               select new
                               {
                                   l.ledger_account_id,
                                   l.fund_id,
                                   l.site_id,
                                   l.ministry_id,
                                   l.debit_amount,
                                   l.credit_amount
                               }).ToList();

            var cuentas = _context.LedgerAccounts.AsNoTracking()
                .ToDictionary(c => c.id, c => c);
            var fondos = _context.Funds.AsNoTracking().ToDictionary(f => f.id, f => f.name ?? "");
            var sedes = _context.Sedes.AsNoTracking().ToDictionary(s => s.ID, s => s.nombre_sede ?? "");
            var ministerios = _context.Ministerios.AsNoTracking()
                .ToDictionary(m => m.ID, m => m.Descripcion ?? "");

            var resultado = new List<SeguimientoLineaDTO>();

            foreach (var linea in lineas)
            {
                var coincidentes = movimientos.Where(m =>
                    (linea.ledger_account_id == null || m.ledger_account_id == linea.ledger_account_id) &&
                    (linea.fund_id == null || m.fund_id == linea.fund_id) &&
                    (linea.site_id == null || m.site_id == linea.site_id) &&
                    (linea.ministry_id == null || m.ministry_id == linea.ministry_id));

                // Un gasto es una cuenta deudora: consume presupuesto lo que va al Debe
                // menos lo que vuelve por el Haber (las reversiones).
                decimal ejecutado = coincidentes.Sum(m => m.debit_amount - m.credit_amount);

                resultado.Add(new SeguimientoLineaDTO
                {
                    id = linea.id,
                    ledger_account_id = linea.ledger_account_id,
                    cuenta = linea.ledger_account_id.HasValue && cuentas.ContainsKey(linea.ledger_account_id.Value)
                        ? cuentas[linea.ledger_account_id.Value].code + " · " + cuentas[linea.ledger_account_id.Value].name
                        : "",
                    fund_id = linea.fund_id,
                    fondo = linea.fund_id.HasValue && fondos.ContainsKey(linea.fund_id.Value)
                        ? fondos[linea.fund_id.Value] : "",
                    site_id = linea.site_id,
                    sede = linea.site_id.HasValue && sedes.ContainsKey(linea.site_id.Value)
                        ? sedes[linea.site_id.Value] : "",
                    ministry_id = linea.ministry_id,
                    ministerio = linea.ministry_id.HasValue && ministerios.ContainsKey(linea.ministry_id.Value)
                        ? ministerios[linea.ministry_id.Value] : "",
                    presupuestado = linea.amount,
                    ejecutado = ejecutado,
                    notas = linea.notes
                });
            }

            return resultado;
        }
    }
}
