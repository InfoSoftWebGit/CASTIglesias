using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Presupuestos por fondo, ministerio y sede, con seguimiento del gasto real.
    /// </summary>
    /// <remarks>
    /// Decisión D7: cuando se supera un presupuesto la aplicación AVISA, no bloquea.
    /// "La aplicación informa, la decisión sigue siendo humana." Por eso aquí no
    /// hay ninguna validación que impida registrar un gasto: el aviso vive en la
    /// pantalla de seguimiento, no en el camino del gasto.
    /// </remarks>
    public class CN_Presupuestos
    {
        private readonly CD_Presupuestos _cdPresupuestos;
        private readonly CD_Ejercicios _cdEjercicios;
        private readonly CD_PlanCuentas _cdPlanCuentas;

        public CN_Presupuestos(CD_Presupuestos cdPresupuestos, CD_Ejercicios cdEjercicios,
                               CD_PlanCuentas cdPlanCuentas)
        {
            _cdPresupuestos = cdPresupuestos;
            _cdEjercicios = cdEjercicios;
            _cdPlanCuentas = cdPlanCuentas;
        }

        public List<Budget> Listar() => _cdPresupuestos.Listar();
        public Budget? Obtener(int id) => _cdPresupuestos.Obtener(id);
        public List<BudgetLine> LineasDe(int id) => _cdPresupuestos.LineasDe(id);

        public int Guardar(Budget presupuesto, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(presupuesto.code))
            {
                mensaje = "El código del presupuesto es obligatorio.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(presupuesto.name))
            {
                mensaje = "El nombre del presupuesto es obligatorio.";
                return 0;
            }
            if (presupuesto.fiscal_year_id <= 0)
            {
                mensaje = "Hay que decir a qué ejercicio pertenece.";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(presupuesto.status))
                presupuesto.status = CD_Presupuestos.Borrador;
            if (string.IsNullOrWhiteSpace(presupuesto.scope_type))
                presupuesto.scope_type = "corporate";

            return _cdPresupuestos.GuardarPresupuesto(presupuesto, out mensaje);
        }

        public bool GuardarLinea(BudgetLine linea, out string mensaje)
        {
            mensaje = string.Empty;

            if (linea.budget_id <= 0)
            {
                mensaje = "La línea tiene que pertenecer a un presupuesto.";
                return false;
            }
            if (linea.amount <= 0)
            {
                mensaje = "El importe presupuestado tiene que ser mayor que cero.";
                return false;
            }

            // Una línea que no concreta nada presupuestaría "todo el gasto de la
            // iglesia", que no sirve para decidir nada.
            if (linea.ledger_account_id == null && linea.fund_id == null
                && linea.site_id == null && linea.ministry_id == null)
            {
                mensaje = "Indica al menos una cuenta, un fondo, una sede o un ministerio: "
                        + "si no, la línea no se puede comparar con ningún gasto concreto.";
                return false;
            }

            if (linea.fund_id == 0) linea.fund_id = null;
            if (linea.site_id == 0) linea.site_id = null;
            if (linea.ministry_id == 0) linea.ministry_id = null;
            if (linea.ledger_account_id == 0) linea.ledger_account_id = null;

            // El seguimiento solo mira cuentas de gasto, porque un presupuesto mide lo
            // que se gasta. Una línea contra una cuenta de ingresos o de caja daría
            // siempre cero ejecutado, y eso parecería "no se ha gastado nada" en vez
            // de un error de configuración. Mejor avisar aquí.
            if (linea.ledger_account_id.HasValue)
            {
                var cuenta = _cdPlanCuentas.Obtener(linea.ledger_account_id.Value);
                if (cuenta == null)
                {
                    mensaje = "La cuenta indicada no existe.";
                    return false;
                }
                if (cuenta.account_type != "expense")
                {
                    mensaje = $"La cuenta {cuenta.code} no es de gastos. Un presupuesto mide "
                            + "el gasto, así que solo admite cuentas de ese tipo.";
                    return false;
                }
            }

            return _cdPresupuestos.GuardarLinea(linea, out mensaje);
        }

        public bool EliminarLinea(int id, out string mensaje)
            => _cdPresupuestos.EliminarLinea(id, out mensaje);

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;

            var presupuesto = _cdPresupuestos.Obtener(id);
            if (presupuesto == null)
            {
                mensaje = "El presupuesto no existe.";
                return false;
            }

            // Un presupuesto aprobado es un acuerdo, normalmente de una asamblea.
            // Se cierra, no se borra.
            if (presupuesto.status == CD_Presupuestos.Activo)
            {
                mensaje = "Un presupuesto activo no se borra. Ciérralo si ya no está en vigor.";
                return false;
            }

            return _cdPresupuestos.EliminarPresupuesto(id, out mensaje);
        }

        /// <summary>Resumen de un presupuesto frente a lo realmente gastado.</summary>
        public class SeguimientoDTO
        {
            public Budget? Presupuesto { get; set; }
            public List<CD_Presupuestos.SeguimientoLineaDTO> Lineas { get; set; } = new();

            public decimal TotalPresupuestado => Lineas.Sum(l => l.presupuestado);
            public decimal TotalEjecutado => Lineas.Sum(l => l.ejecutado);
            public decimal TotalDisponible => TotalPresupuestado - TotalEjecutado;

            public decimal PorcentajeGlobal => TotalPresupuestado == 0
                ? 0
                : Math.Round(TotalEjecutado / TotalPresupuestado * 100, 1);

            /// <summary>Las líneas que se han pasado, que son las que hay que mirar.</summary>
            public List<CD_Presupuestos.SeguimientoLineaDTO> Superadas =>
                Lineas.Where(l => l.Superado).ToList();
        }

        public SeguimientoDTO Seguimiento(int idPresupuesto)
        {
            var presupuesto = _cdPresupuestos.Obtener(idPresupuesto);
            if (presupuesto == null) return new SeguimientoDTO();

            var ejercicio = _cdEjercicios.Obtener(presupuesto.fiscal_year_id);
            DateTime desde = ejercicio?.start_date ?? new DateTime(DateTime.Today.Year, 1, 1);
            DateTime hasta = ejercicio?.end_date ?? new DateTime(DateTime.Today.Year, 12, 31);

            return new SeguimientoDTO
            {
                Presupuesto = presupuesto,
                Lineas = _cdPresupuestos.Seguimiento(idPresupuesto, desde, hasta)
            };
        }
    }
}
