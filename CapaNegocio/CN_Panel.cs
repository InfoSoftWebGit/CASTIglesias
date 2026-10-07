using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Arma el panel financiero: lo que hay que ver al entrar, sin tener que
    /// rellenar ningún filtro.
    /// </summary>
    /// <remarks>
    /// El panel responde a cuatro preguntas, en este orden:
    ///
    ///   1. ¿Hay algo que me impida trabajar? (preparación y avisos)
    ///   2. ¿Cuánto dinero tengo y dónde está? (cajas y fondos)
    ///   3. ¿Cómo va el ejercicio? (resultado contable)
    ///   4. ¿Me estoy pasando del presupuesto?
    ///
    /// Los importes salen de la CONTABILIDAD, no de las operaciones registradas. Es
    /// un cambio respecto al panel anterior y es deliberado: lo que no se ha
    /// contabilizado no es un dato contable, y mezclarlos daba un resultado que no
    /// coincidía con ningún informe. Lo pendiente de contabilizar se enseña aparte,
    /// como aviso, que es lo que es.
    /// </remarks>
    public class CN_Panel
    {
        private readonly CD_Panel _cdPanel;
        private readonly CD_Presupuestos _cdPresupuestos;
        private readonly CN_Informes _negocioInformes;

        public CN_Panel(CD_Panel cdPanel, CD_Presupuestos cdPresupuestos, CN_Informes negocioInformes)
        {
            _cdPanel = cdPanel;
            _cdPresupuestos = cdPresupuestos;
            _negocioInformes = negocioInformes;
        }

        /// <summary>Resumen del presupuesto activo, para la barra del panel.</summary>
        public class ResumenPresupuestoDTO
        {
            public int id { get; set; }
            public string? nombre { get; set; }
            public decimal presupuestado { get; set; }
            public decimal ejecutado { get; set; }

            /// <summary>Aprobado y aún sin contabilizar: reservado, ya no disponible.</summary>
            public decimal comprometido { get; set; }

            /// <summary>Líneas que ya se han pasado de lo previsto.</summary>
            public int lineas_superadas { get; set; }

            public decimal Disponible => presupuestado - ejecutado - comprometido;

            public decimal PorcentajeConsumido =>
                presupuestado == 0 ? 0 : Math.Round((ejecutado + comprometido) / presupuestado * 100, 1);

            public bool Superado => (ejecutado + comprometido) > presupuestado;
        }

        /// <summary>Todo lo que pinta el panel.</summary>
        public class PanelDTO
        {
            public FiscalYear? Ejercicio { get; set; }

            // ---- Preparación: qué falta para poder contabilizar ----
            public bool TieneCuentas { get; set; }
            public bool TieneCajas { get; set; }
            public bool TieneConceptos { get; set; }
            public bool TieneReglas { get; set; }

            /// <summary>true cuando se puede trabajar: las cuatro cosas están.</summary>
            public bool Preparado => TieneCuentas && TieneCajas && TieneConceptos && TieneReglas;

            // ---- Resultado del ejercicio, según la contabilidad ----
            public decimal Ingresos { get; set; }
            public decimal Gastos { get; set; }
            public decimal Resultado => Ingresos - Gastos;
            public bool EsExcedente => Resultado >= 0;

            /// <summary>Las cuentas de ingreso con más peso, para el gráfico de barras.</summary>
            public List<CD_Informes.SaldoCuentaDTO> IngresosPorCuenta { get; set; } = new();

            /// <summary>Las cuentas de gasto con más peso.</summary>
            public List<CD_Informes.SaldoCuentaDTO> GastosPorCuenta { get; set; } = new();

            /// <summary>Los doce meses del ejercicio, para el gráfico de evolución.</summary>
            public List<CD_Panel.MesDTO> Meses { get; set; } = new();

            /// <summary>Si hay algo que dibujar: un gráfico de doce ceros no dice nada.</summary>
            public bool HayEvolucion => Meses.Any(m => m.ingresos != 0 || m.gastos != 0);

            // ---- Dónde está el dinero ----
            public List<CD_Panel.SaldoCajaDTO> Cajas { get; set; } = new();
            public List<CD_Panel.SaldoFondoDTO> Fondos { get; set; } = new();

            public decimal TotalTesoreria => Cajas.Sum(c => c.saldo);
            public int TotalSinConciliar => Cajas.Sum(c => c.sin_conciliar);

            /// <summary>Cajas en descubierto. Puede ser legítimo, pero hay que verlo.</summary>
            public List<CD_Panel.SaldoCajaDTO> CajasEnNegativo =>
                Cajas.Where(c => c.saldo < 0).ToList();

            /// <summary>Fondos gastados por encima de lo que tienen.</summary>
            public List<CD_Panel.SaldoFondoDTO> FondosEnNegativo =>
                Fondos.Where(f => f.Disponible < 0).ToList();

            // ---- Avisos ----
            public int OperacionesSinContabilizar { get; set; }
            public decimal ImporteSinContabilizar { get; set; }
            public int PeriodosAbiertos { get; set; }

            // ---- Presupuesto ----
            public ResumenPresupuestoDTO? Presupuesto { get; set; }

            /// <summary>
            /// Si hay algo que reclame atención. Lo usa la vista para decidir si pinta
            /// la tarjeta de avisos o la calla.
            /// </summary>
            public bool TieneAvisos =>
                OperacionesSinContabilizar > 0
                || CajasEnNegativo.Count > 0
                || FondosEnNegativo.Count > 0
                || (Presupuesto?.Superado ?? false);
        }

        /// <summary>
        /// Prepara el panel de un ejercicio.
        /// </summary>
        /// <param name="sedeID">Sede activa, o 1000 para ver toda la iglesia.</param>
        public PanelDTO Armar(FiscalYear? ejercicio, int sedeID)
        {
            var panel = new PanelDTO { Ejercicio = ejercicio };

            // La preparación se mira siempre, incluso sin ejercicio: es justo cuando
            // más falta hace saber qué queda por configurar.
            var preparacion = _cdPanel.Preparacion();
            panel.TieneCuentas = preparacion.cuentas;
            panel.TieneCajas = preparacion.cajas;
            panel.TieneConceptos = preparacion.conceptos;
            panel.TieneReglas = preparacion.reglas;

            // Los saldos de caja y fondo no dependen del ejercicio: el dinero que hay
            // en la caja es el que hay, venga del ejercicio que venga.
            panel.Cajas = _cdPanel.SaldosPorCaja(sedeID);
            panel.Fondos = _cdPanel.SaldosPorFondo(sedeID);

            if (ejercicio == null) return panel;

            int? sedeFiltro = sedeID > 0 && sedeID != 1000 ? sedeID : null;

            var resultados = _negocioInformes.Resultados(ejercicio.start_date, ejercicio.end_date,
                                                         sedeFiltro, null);
            panel.Ingresos = resultados.Ingresos.Total;
            panel.Gastos = resultados.Gastos.Total;

            // Seis de cada una: son barras, y más de seis dejan de leerse de un vistazo.
            panel.IngresosPorCuenta = resultados.Ingresos.Cuentas
                .OrderByDescending(c => c.saldo).Take(6).ToList();
            panel.GastosPorCuenta = resultados.Gastos.Cuentas
                .OrderByDescending(c => c.saldo).Take(6).ToList();

            panel.Meses = _cdPanel.EvolucionMensual(ejercicio.start_date, ejercicio.end_date, sedeID);

            var pendiente = _cdPanel.SinContabilizar(ejercicio.id);
            panel.OperacionesSinContabilizar = pendiente.cuantas;
            panel.ImporteSinContabilizar = pendiente.importe;
            panel.PeriodosAbiertos = _cdPanel.PeriodosAbiertos(ejercicio.id);

            panel.Presupuesto = ResumirPresupuesto(ejercicio);

            return panel;
        }

        /// <summary>
        /// Presupuesto activo del ejercicio comparado con lo gastado.
        /// </summary>
        /// <remarks>
        /// Se reutiliza el seguimiento de la pantalla de presupuestos en vez de
        /// calcularlo otra vez: si algún día cambia cómo se mide lo ejecutado, cambia
        /// en un solo sitio y el panel no se queda diciendo otra cosa.
        /// </remarks>
        private ResumenPresupuestoDTO? ResumirPresupuesto(FiscalYear ejercicio)
        {
            var presupuesto = _cdPanel.PresupuestoActivo(ejercicio.id);
            if (presupuesto == null) return null;

            var lineas = _cdPresupuestos.Seguimiento(presupuesto.id,
                                                     ejercicio.start_date, ejercicio.end_date);

            return new ResumenPresupuestoDTO
            {
                id = presupuesto.id,
                nombre = presupuesto.name,
                presupuestado = lineas.Sum(l => l.presupuestado),
                ejecutado = lineas.Sum(l => l.ejecutado),
                comprometido = lineas.Sum(l => l.comprometido),
                lineas_superadas = lineas.Count(l => l.Superado)
            };
        }
    }
}
