using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Reserva y libera presupuesto cuando un gasto se aprueba o se contabiliza.
    /// </summary>
    /// <remarks>
    /// Ver CD_Compromisos para el problema que esto resuelve.
    ///
    /// Todo lo de aquí es BLANDO: si no hay presupuesto activo, o el gasto no encaja en
    /// ninguna línea, no se reserva nada y la operación sigue su curso. El presupuesto
    /// avisa, no bloquea (decisión D7), así que tampoco puede impedir aprobar.
    /// </remarks>
    public class CN_Compromisos
    {
        private readonly CD_Compromisos _cdCompromisos;
        private readonly CD_Panel _cdPanel;
        private readonly CD_ConceptosFinancieros _cdConceptos;

        public CN_Compromisos(CD_Compromisos cdCompromisos, CD_Panel cdPanel,
                              CD_ConceptosFinancieros cdConceptos)
        {
            _cdCompromisos = cdCompromisos;
            _cdPanel = cdPanel;
            _cdConceptos = cdConceptos;
        }

        public Dictionary<int, decimal> ComprometidoPorLinea(int idPresupuesto)
            => _cdCompromisos.ComprometidoPorLinea(idPresupuesto);

        public decimal TotalComprometido(int idPresupuesto)
            => _cdCompromisos.TotalComprometido(idPresupuesto);

        /// <summary>
        /// Reserva el importe de una operación aprobada contra su línea de presupuesto.
        /// </summary>
        /// <returns>true si se llegó a reservar algo.</returns>
        public bool ReservarPorOperacion(FinancialTransaction operacion)
        {
            try
            {
                // Solo los gastos consumen presupuesto. Un ingreso no reserva nada.
                if (!CN_Operaciones.TiposGasto.Contains(operacion.transaction_kind))
                    return false;

                var presupuesto = _cdPanel.PresupuestoActivo(operacion.fiscal_year_id);
                if (presupuesto == null) return false;

                var concepto = _cdConceptos.Obtener(operacion.concept_id);
                var linea = _cdCompromisos.LineaQueAplica(presupuesto.id, operacion,
                                                           concepto?.default_expense_account_id);
                if (linea == null) return false;

                return _cdCompromisos.Reservar(presupuesto.id, linea.id, operacion.id,
                                                operacion.total_amount, out _) > 0;
            }
            catch (Exception)
            {
                // Reservar es una ayuda, no un requisito: que falle no puede impedir
                // aprobar un gasto que alguien ya ha decidido.
                return false;
            }
        }

        /// <summary>Libera lo reservado por una operación.</summary>
        public void LiberarPorOperacion(int idOperacion)
            => _cdCompromisos.Liberar(idOperacion);
    }
}
