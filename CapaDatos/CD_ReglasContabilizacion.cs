using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Reglas de contabilización: qué cuenta va al Debe y cuál al Haber en cada
    /// tipo de operación.
    /// </summary>
    /// <remarks>
    /// Es la configuración que hace que el motor contable no conozca ningún país.
    /// Hasta ahora estas filas solo se podían crear por SQL, lo que obligaba a entrar
    /// en la base de datos de cada cliente nuevo. Con esta pantalla, una iglesia se
    /// configura sola.
    ///
    /// Los conjuntos llevan versión y vigencia a propósito: cada asiento guarda con
    /// qué versión se generó, así que cambiar las reglas no reescribe la historia.
    /// Por eso un conjunto que ya ha contabilizado algo no se toca, se versiona.
    /// </remarks>
    public class CD_ReglasContabilizacion
    {
        private readonly AppDbContext _context;

        public CD_ReglasContabilizacion(AppDbContext context) => _context = context;

        public const string Activo = "active";
        public const string Inactivo = "inactive";

        // --------------------------------------------------------------------
        // Conjuntos
        // --------------------------------------------------------------------

        public List<PostingRuleSet> ListarConjuntos()
            => _context.PostingRuleSets.AsNoTracking()
                .OrderByDescending(c => c.version).ToList();

        public PostingRuleSet? ObtenerConjunto(int id)
            => _context.PostingRuleSets.AsNoTracking().FirstOrDefault(c => c.id == id);

        public PostingRuleSet? ConjuntoActivo()
            => _context.PostingRuleSets.AsNoTracking()
                .Where(c => c.status == Activo)
                .OrderByDescending(c => c.version)
                .FirstOrDefault();

        public int GuardarConjunto(PostingRuleSet conjunto, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (conjunto.id == 0)
                {
                    conjunto.created_at = DateTime.UtcNow;
                    _context.PostingRuleSets.Add(conjunto);
                }
                else
                {
                    var actual = _context.PostingRuleSets.FirstOrDefault(c => c.id == conjunto.id);
                    if (actual == null)
                    {
                        mensaje = "El conjunto de reglas no existe.";
                        return 0;
                    }
                    actual.name = conjunto.name;
                    actual.valid_from = conjunto.valid_from;
                    actual.valid_to = conjunto.valid_to;
                    actual.status = conjunto.status;
                }

                _context.SaveChanges();
                mensaje = "Conjunto de reglas guardado.";
                return conjunto.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar el conjunto: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Crea el conjunto básico con sus tres reglas, en una transacción.
        /// </summary>
        /// <remarks>
        /// Son las mismas tres reglas que antes se metían por SQL. Ninguna nombra una
        /// cuenta concreta: dicen de DÓNDE sacarla, así que sirven igual en España que
        /// en cualquier otro país.
        /// </remarks>
        public int CrearConjuntoBasico(string[] tiposIngreso, string tipoGasto,
                                       string fuenteTesoreria, string fuenteConcepto,
                                       int? idUsuario, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var conjunto = new PostingRuleSet
                {
                    ID_iglesia = _context.IdIglesiaActual,
                    code = "BASE",
                    name = "Reglas básicas",
                    version = 1,
                    // Muy atrás a propósito: así cubre cualquier operación antigua que
                    // se quiera contabilizar, sin tener que pensar en la fecha.
                    valid_from = new DateTime(2020, 1, 1),
                    status = Activo,
                    created_at = DateTime.UtcNow,
                    created_by = idUsuario
                };
                _context.PostingRuleSets.Add(conjunto);
                _context.SaveChanges();

                foreach (var tipo in tiposIngreso)
                {
                    // Un ingreso entra en la caja (Debe) y lo explica el concepto (Haber)
                    _context.PostingRules.Add(new PostingRule
                    {
                        ID_iglesia = conjunto.ID_iglesia,
                        rule_set_id = conjunto.id,
                        priority = 100,
                        transaction_kind = tipo,
                        debit_account_source = fuenteTesoreria,
                        credit_account_source = fuenteConcepto,
                        status = Activo
                    });
                }

                // Un gasto es al revés: lo explica el concepto (Debe) y sale de la caja
                _context.PostingRules.Add(new PostingRule
                {
                    ID_iglesia = conjunto.ID_iglesia,
                    rule_set_id = conjunto.id,
                    priority = 100,
                    transaction_kind = tipoGasto,
                    debit_account_source = fuenteConcepto,
                    credit_account_source = fuenteTesoreria,
                    status = Activo
                });

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Reglas básicas creadas.";
                return conjunto.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al crear las reglas básicas: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        // --------------------------------------------------------------------
        // Reglas
        // --------------------------------------------------------------------

        /// <summary>Regla con los nombres resueltos, para el listado.</summary>
        public class ReglaDTO
        {
            public int id { get; set; }
            public int rule_set_id { get; set; }
            public int priority { get; set; }
            public string? transaction_kind { get; set; }
            public int? concept_id { get; set; }
            public string? concepto { get; set; }
            public int? site_id { get; set; }
            public string? sede { get; set; }
            public string? payment_method { get; set; }
            public string? debit_account_source { get; set; }
            public string? credit_account_source { get; set; }
            public int? fixed_debit_account_id { get; set; }
            public string? cuenta_debe { get; set; }
            public int? fixed_credit_account_id { get; set; }
            public string? cuenta_haber { get; set; }
            public string? status { get; set; }
        }

        public List<ReglaDTO> ListarReglas(int idConjunto)
        {
            var reglas = _context.PostingRules.AsNoTracking()
                .Where(r => r.rule_set_id == idConjunto)
                .OrderBy(r => r.priority).ThenBy(r => r.id)
                .ToList();

            var conceptos = _context.FinancialConcepts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");
            var sedes = _context.Sedes.AsNoTracking()
                .ToDictionary(s => s.ID, s => s.nombre_sede ?? "");
            var cuentas = _context.LedgerAccounts.AsNoTracking()
                .ToDictionary(c => c.id, c => (c.code ?? "") + " · " + (c.name ?? ""));

            return reglas.Select(r => new ReglaDTO
            {
                id = r.id,
                rule_set_id = r.rule_set_id,
                priority = r.priority,
                transaction_kind = r.transaction_kind,
                concept_id = r.concept_id,
                concepto = r.concept_id.HasValue && conceptos.ContainsKey(r.concept_id.Value)
                    ? conceptos[r.concept_id.Value] : "",
                site_id = r.site_id,
                sede = r.site_id.HasValue && sedes.ContainsKey(r.site_id.Value)
                    ? sedes[r.site_id.Value] : "",
                payment_method = r.payment_method,
                debit_account_source = r.debit_account_source,
                credit_account_source = r.credit_account_source,
                fixed_debit_account_id = r.fixed_debit_account_id,
                cuenta_debe = r.fixed_debit_account_id.HasValue && cuentas.ContainsKey(r.fixed_debit_account_id.Value)
                    ? cuentas[r.fixed_debit_account_id.Value] : "",
                fixed_credit_account_id = r.fixed_credit_account_id,
                cuenta_haber = r.fixed_credit_account_id.HasValue && cuentas.ContainsKey(r.fixed_credit_account_id.Value)
                    ? cuentas[r.fixed_credit_account_id.Value] : "",
                status = r.status
            }).ToList();
        }

        public PostingRule? ObtenerRegla(int id)
            => _context.PostingRules.AsNoTracking().FirstOrDefault(r => r.id == id);

        public bool GuardarRegla(PostingRule regla, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (regla.id == 0)
                {
                    _context.PostingRules.Add(regla);
                }
                else
                {
                    var actual = _context.PostingRules.FirstOrDefault(r => r.id == regla.id);
                    if (actual == null)
                    {
                        mensaje = "La regla no existe.";
                        return false;
                    }
                    actual.priority = regla.priority;
                    actual.transaction_kind = regla.transaction_kind;
                    actual.concept_id = regla.concept_id;
                    actual.site_id = regla.site_id;
                    actual.payment_method = regla.payment_method;
                    actual.debit_account_source = regla.debit_account_source;
                    actual.credit_account_source = regla.credit_account_source;
                    actual.fixed_debit_account_id = regla.fixed_debit_account_id;
                    actual.fixed_credit_account_id = regla.fixed_credit_account_id;
                    actual.status = regla.status;
                }

                _context.SaveChanges();
                mensaje = "Regla guardada.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar la regla: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool EliminarRegla(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var regla = _context.PostingRules.FirstOrDefault(r => r.id == id);
                if (regla == null)
                {
                    mensaje = "La regla no existe.";
                    return false;
                }
                _context.PostingRules.Remove(regla);
                _context.SaveChanges();
                mensaje = "Regla eliminada.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar la regla: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// <summary>
        /// ¿Se ha contabilizado ya algo con este conjunto? Si es así, sus reglas no se
        /// deberían tocar: los asientos ya hechos se explican con ellas.
        /// </summary>
        public bool TieneAsientos(int idConjunto)
            => _context.JournalEntries.AsNoTracking().Any(a => a.posting_rule_set_id == idConjunto);
    }
}
