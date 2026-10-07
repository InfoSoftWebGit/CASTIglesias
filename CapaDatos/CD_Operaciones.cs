using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Operaciones financieras: ingresos, aportaciones y gastos.
    /// </summary>
    /// <remarks>
    /// De momento las operaciones se quedan en borrador o aprobadas: NO generan
    /// asientos. Eso lo hará el motor contable, que necesita antes las decisiones
    /// del plan de cuentas. Aquí ya se guarda todo lo que el motor necesitará.
    /// </remarks>
    public class CD_Operaciones
    {
        private readonly AppDbContext _context;

        public CD_Operaciones(AppDbContext context) => _context = context;

        // Estados de financial_transactions que se usan por ahora
        public const string Borrador = "draft";
        public const string Aprobada = "approved";
        public const string PendienteAprobacion = "pending_approval";

        /// <summary>Operación con los nombres ya resueltos, para el listado.</summary>
        public class OperacionDTO
        {
            public int id { get; set; }
            public string? transaction_number { get; set; }
            public string? transaction_kind { get; set; }
            public DateTime operation_date { get; set; }
            public int concept_id { get; set; }
            public string? concepto { get; set; }
            public int? party_id { get; set; }
            public string? tercero { get; set; }
            public bool is_anonymous { get; set; }
            public int? fund_id { get; set; }
            public string? fondo { get; set; }
            public int? treasury_account_id { get; set; }
            public string? caja { get; set; }
            public decimal total_amount { get; set; }
            public string? description { get; set; }
            public string? status { get; set; }
            public int site_id { get; set; }

            // Estado contable: la vista los usa para decidir si ofrece contabilizar,
            // revertir, o solo mirar.
            public string? posting_status { get; set; }
            public int? journal_entry_id { get; set; }
            public string? asiento { get; set; }
        }

        /// <summary>
        /// Lista las operaciones de unos tipos concretos.
        /// </summary>
        /// <param name="sedeID">
        /// Sede activa. Si es la 1000 ("todas"), no se filtra por sede: el filtro
        /// por iglesia lo pone EF de todos modos.
        /// </param>
        public List<OperacionDTO> Listar(string[] tipos, int sedeID, DateTime? desde, DateTime? hasta,
                                         bool incluirNombresDonante)
        {
            var consulta = _context.FinancialTransactions.AsNoTracking()
                .Where(t => tipos.Contains(t.transaction_kind));

            if (sedeID != Sedes.TodasLasSedes)
                consulta = consulta.Where(t => t.site_id == sedeID);

            if (desde.HasValue)
                consulta = consulta.Where(t => t.operation_date >= desde.Value);
            if (hasta.HasValue)
                consulta = consulta.Where(t => t.operation_date <= hasta.Value);

            var operaciones = consulta
                .OrderByDescending(t => t.operation_date)
                .ThenByDescending(t => t.id)
                .Take(1000)
                .ToList();

            // Los nombres se resuelven en memoria: son catálogos pequeños y evita
            // una unión por cada columna.
            var conceptos = _context.FinancialConcepts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");
            var fondos = _context.Funds.AsNoTracking()
                .ToDictionary(f => f.id, f => f.name ?? "");
            var cajas = _context.TreasuryAccounts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");
            var terceros = incluirNombresDonante
                ? _context.Parties.AsNoTracking().ToDictionary(p => p.id, p => p.display_name ?? "")
                : new Dictionary<int, string>();

            // Número del asiento de las que ya están contabilizadas, para poder
            // enlazar desde el listado al Diario. Solo se piden los que hacen falta.
            var idsAsiento = operaciones.Where(t => t.journal_entry_id.HasValue)
                                        .Select(t => t.journal_entry_id!.Value)
                                        .Distinct().ToList();
            var asientos = idsAsiento.Count == 0
                ? new Dictionary<int, string>()
                : _context.JournalEntries.AsNoTracking()
                    .Where(a => idsAsiento.Contains(a.id))
                    .ToDictionary(a => a.id, a => a.entry_number ?? "");

            return operaciones.Select(t => new OperacionDTO
            {
                id = t.id,
                transaction_number = t.transaction_number,
                transaction_kind = t.transaction_kind,
                operation_date = t.operation_date,
                concept_id = t.concept_id,
                concepto = conceptos.ContainsKey(t.concept_id) ? conceptos[t.concept_id] : "",
                party_id = t.party_id,
                tercero = t.party_id.HasValue && terceros.ContainsKey(t.party_id.Value)
                    ? terceros[t.party_id.Value] : "",
                is_anonymous = t.is_anonymous,
                fund_id = t.fund_id,
                fondo = t.fund_id.HasValue && fondos.ContainsKey(t.fund_id.Value) ? fondos[t.fund_id.Value] : "",
                treasury_account_id = t.treasury_account_id,
                caja = t.treasury_account_id.HasValue && cajas.ContainsKey(t.treasury_account_id.Value)
                    ? cajas[t.treasury_account_id.Value] : "",
                total_amount = t.total_amount,
                description = t.description,
                status = t.status,
                site_id = t.site_id,
                posting_status = t.posting_status,
                journal_entry_id = t.journal_entry_id,
                asiento = t.journal_entry_id.HasValue && asientos.ContainsKey(t.journal_entry_id.Value)
                    ? asientos[t.journal_entry_id.Value] : ""
            }).ToList();
        }

        public FinancialTransaction? Obtener(int id)
        {
            return _context.FinancialTransactions.AsNoTracking().FirstOrDefault(t => t.id == id);
        }

        /// <summary>
        /// Guarda una operación nueva y le asigna su número, todo en una transacción.
        /// </summary>
        /// <remarks>
        /// El número sale de document_sequences con un bloqueo pesimista breve
        /// (SELECT ... FOR UPDATE): si dos personas registran a la vez, una espera
        /// a la otra y no se repite ni se salta ningún número, que es lo que pide
        /// la especificación.
        /// </remarks>
        public int Registrar(FinancialTransaction operacion, string tipoDocumento,
                             string prefijoSede, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                // La iglesia hay que ponerla AQUÍ, antes de numerar. El formulario no la
                // envía (ninguna vista lo hace) y normalmente la rellena AsignarIglesia
                // al guardar, pero eso ocurre demasiado tarde: la numeración de abajo
                // necesita saber de qué iglesia es el contador.
                // Con ID_iglesia a 0 se buscaba el contador de la iglesia 0, no se
                // encontraba, se intentaba crear uno nuevo, y al guardarlo AsignarIglesia
                // le ponía la iglesia real y chocaba con uk_document_sequences.
                if (operacion.ID_iglesia == 0)
                    operacion.ID_iglesia = _context.IdIglesiaActual;

                operacion.transaction_number = SiguienteNumero(
                    operacion.ID_iglesia, operacion.site_id, tipoDocumento,
                    operacion.fiscal_year_id, prefijoSede);

                operacion.created_at = DateTime.UtcNow;
                _context.FinancialTransactions.Add(operacion);
                _context.SaveChanges();

                transaccion.Commit();
                mensaje = "Operación registrada con el número " + operacion.transaction_number + ".";
                return operacion.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al registrar la operación: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Reserva el siguiente número de una secuencia. Debe llamarse DENTRO de
        /// una transacción ya abierta.
        /// </summary>
        private string SiguienteNumero(int idIglesia, int idSede, string tipoDocumento,
                                       int idEjercicio, string prefijoSede)
        {
            // El bloqueo y la lectura van SEPARADOS, y no es un capricho.
            //
            // Antes esto era un solo FromSqlRaw con el FOR UPDATE dentro. El problema
            // es que el filtro global por iglesia se compone sobre esa consulta, y al
            // componer, EF la envuelve en una subconsulta. La fila existente dejaba de
            // encontrarse, el código de abajo creía que era la primera operación del
            // ejercicio e intentaba CREAR la secuencia otra vez, chocando con
            // uk_document_sequences ("Ya existe un registro con ese valor"). Y solo
            // fallaba a partir de la SEGUNDA operación, porque la primera vez la
            // secuencia de verdad no existe.
            //
            // Así el SQL crudo no pasa por el traductor de EF (es un comando suelto) y
            // la lectura es LINQ normal, que EF sabe traducir y filtrar bien.
            // El bloqueo se mantiene hasta el commit de la transacción, que es lo que
            // impide que dos peticiones simultáneas se lleven el mismo número.
            _context.Database.ExecuteSqlRaw(
                @"SELECT id FROM document_sequences
                  WHERE organization_id = {0} AND site_id = {1}
                    AND document_type = {2} AND fiscal_year_id = {3}
                  FOR UPDATE",
                idIglesia, idSede, tipoDocumento, idEjercicio);

            var secuencia = _context.DocumentSequences
                .FirstOrDefault(s => s.ID_iglesia == idIglesia
                                  && s.site_id == idSede
                                  && s.document_type == tipoDocumento
                                  && s.fiscal_year_id == idEjercicio);

            if (secuencia == null)
            {
                // Primera operación de este tipo en este ejercicio y sede
                secuencia = new DocumentSequence
                {
                    ID_iglesia = idIglesia,
                    site_id = idSede,
                    document_type = tipoDocumento,
                    fiscal_year_id = idEjercicio,
                    prefix = prefijoSede,
                    next_number = 1,
                    padding_length = 6,
                    row_version = 1
                };
                _context.DocumentSequences.Add(secuencia);
                _context.SaveChanges();
            }

            int numero = secuencia.next_number;
            secuencia.next_number = numero + 1;
            secuencia.row_version++;
            _context.SaveChanges();

            string relleno = numero.ToString().PadLeft(Math.Max(secuencia.padding_length, (sbyte)1), '0');
            return string.IsNullOrWhiteSpace(secuencia.prefix)
                ? relleno
                : secuencia.prefix + "-" + relleno;
        }

        public bool Editar(FinancialTransaction operacion, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var actual = _context.FinancialTransactions.FirstOrDefault(t => t.id == operacion.id);
                if (actual == null)
                {
                    mensaje = "Operación no encontrada.";
                    return false;
                }

                // El número y las fechas contables no se tocan al editar: el número
                // ya está reservado y cambiar el periodo movería la operación de mes.
                actual.operation_date = operacion.operation_date;
                actual.concept_id = operacion.concept_id;
                actual.party_id = operacion.party_id;
                actual.is_anonymous = operacion.is_anonymous;
                actual.fund_id = operacion.fund_id;
                actual.treasury_account_id = operacion.treasury_account_id;
                actual.total_amount = operacion.total_amount;
                actual.description = operacion.description;
                actual.external_reference = operacion.external_reference;
                actual.payment_method = operacion.payment_method;
                actual.status = operacion.status;
                actual.updated_at = DateTime.UtcNow;
                actual.row_version++;

                _context.SaveChanges();
                mensaje = "Operación actualizada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar la operación: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var operacion = _context.FinancialTransactions.FirstOrDefault(t => t.id == id);
                if (operacion == null)
                {
                    mensaje = "Operación no encontrada.";
                    return false;
                }

                _context.FinancialTransactions.Remove(operacion);
                _context.SaveChanges();
                mensaje = "Operación eliminada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar la operación: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        #region Totales para el panel

        /// <summary>Suma por tipo de operación en un rango de fechas.</summary>
        public decimal SumarPorTipos(string[] tipos, int sedeID, DateTime desde, DateTime hasta)
        {
            var consulta = _context.FinancialTransactions.AsNoTracking()
                .Where(t => tipos.Contains(t.transaction_kind)
                         && t.operation_date >= desde && t.operation_date <= hasta);

            if (sedeID != Sedes.TodasLasSedes)
                consulta = consulta.Where(t => t.site_id == sedeID);

            return consulta.Sum(t => (decimal?)t.total_amount) ?? 0m;
        }

        /// <summary>Totales agrupados por concepto, para el panel.</summary>
        public List<(string concepto, decimal total)> TotalesPorConcepto(
            string[] tipos, int sedeID, DateTime desde, DateTime hasta)
        {
            var consulta = _context.FinancialTransactions.AsNoTracking()
                .Where(t => tipos.Contains(t.transaction_kind)
                         && t.operation_date >= desde && t.operation_date <= hasta);

            if (sedeID != Sedes.TodasLasSedes)
                consulta = consulta.Where(t => t.site_id == sedeID);

            var totales = consulta
                .GroupBy(t => t.concept_id)
                .Select(g => new { IdConcepto = g.Key, Total = g.Sum(x => x.total_amount) })
                .ToList();

            var nombres = _context.FinancialConcepts.AsNoTracking()
                .ToDictionary(c => c.id, c => c.name ?? "");

            return totales
                .Select(t => (nombres.ContainsKey(t.IdConcepto) ? nombres[t.IdConcepto] : "", t.Total))
                .OrderByDescending(t => t.Item2)
                .ToList();
        }

        #endregion
    }
}
