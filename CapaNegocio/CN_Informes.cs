using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Libro Mayor, Balance de Situación y Cuenta de Resultados.
    /// </summary>
    /// <remarks>
    /// Decisión D3: la aplicación CALCULA y PROPONE, pero no cierra el ejercicio ni
    /// genera asientos de cierre. Estos informes son el "calcula"; la propuesta de
    /// cierre se apoya en ellos.
    ///
    /// Igual que el motor, aquí no hay ninguna cuenta escrita en el código: lo único
    /// que se mira de cada cuenta es su TIPO (activo, pasivo, patrimonio, ingreso o
    /// gasto), que es lo que la iglesia declara al crearla en su plan. Por eso esto
    /// funciona con el plan español y con cualquier otro.
    /// </remarks>
    public class CN_Informes
    {
        private readonly CD_Informes _cdInformes;

        public CN_Informes(CD_Informes cdInformes) => _cdInformes = cdInformes;

        /// <summary>Un bloque del Balance o de la Cuenta de Resultados.</summary>
        public class BloqueInforme
        {
            public string Titulo { get; set; } = "";
            public List<CD_Informes.SaldoCuentaDTO> Cuentas { get; set; } = new();
            public decimal Total => Cuentas.Sum(c => c.saldo);
        }

        /// <summary>Balance de Situación a una fecha.</summary>
        public class BalanceDTO
        {
            public BloqueInforme Activo { get; set; } = new();
            public BloqueInforme Pasivo { get; set; } = new();
            public BloqueInforme Patrimonio { get; set; } = new();

            /// <summary>
            /// Resultado del ejercicio que todavía no está en ninguna cuenta de
            /// patrimonio, porque no se ha hecho el asiento de cierre (D3).
            /// </summary>
            public decimal ResultadoDelEjercicio { get; set; }

            public decimal TotalActivo => Activo.Total;
            public decimal TotalPasivoYPatrimonio =>
                Pasivo.Total + Patrimonio.Total + ResultadoDelEjercicio;

            /// <summary>
            /// El Activo tiene que ser igual al Pasivo más el Patrimonio. Si no lo es,
            /// hay un problema y el informe lo dice en vez de disimularlo.
            /// </summary>
            public bool Cuadra => Math.Abs(TotalActivo - TotalPasivoYPatrimonio) < 0.01m;
            public decimal Descuadre => TotalActivo - TotalPasivoYPatrimonio;
        }

        /// <summary>Cuenta de Resultados de un periodo.</summary>
        public class ResultadosDTO
        {
            public BloqueInforme Ingresos { get; set; } = new();
            public BloqueInforme Gastos { get; set; } = new();

            /// <summary>Positivo es excedente; negativo, déficit.</summary>
            public decimal Resultado => Ingresos.Total - Gastos.Total;
            public bool EsExcedente => Resultado >= 0;
        }

        // --------------------------------------------------------------------
        // Libro Mayor
        // --------------------------------------------------------------------

        public List<LedgerAccount> CuentasConMovimiento() => _cdInformes.CuentasConMovimiento();

        /// <summary>
        /// Movimientos de una cuenta con su saldo de arranque.
        /// </summary>
        public (decimal saldoInicial, List<CD_Informes.MovimientoMayorDTO> movimientos)
            Mayor(int cuentaID, DateTime desde, DateTime hasta, int? sedeID, int? fondoID)
        {
            decimal saldoInicial = _cdInformes.SaldoAnterior(cuentaID, desde, sedeID, fondoID);
            var movimientos = _cdInformes.MovimientosDeCuenta(cuentaID, desde, hasta,
                                                              sedeID, fondoID, saldoInicial);
            return (saldoInicial, movimientos);
        }

        // --------------------------------------------------------------------
        // Balance de Situación
        // --------------------------------------------------------------------

        /// <summary>
        /// Foto del patrimonio a una fecha: qué tiene la iglesia y con qué lo ha
        /// financiado.
        /// </summary>
        /// <remarks>
        /// El resultado del ejercicio se calcula aparte y se suma al patrimonio. Es
        /// necesario porque, sin asiento de cierre (D3), las cuentas de ingresos y
        /// gastos siguen vivas y su diferencia no está en ninguna cuenta de
        /// patrimonio. Sin esto, el balance no cuadraría nunca.
        /// </remarks>
        public BalanceDTO Balance(DateTime hasta, int? sedeID, int? fondoID, DateTime inicioEjercicio)
        {
            var saldos = _cdInformes.SaldosPorCuenta(null, hasta, sedeID, fondoID);

            var balance = new BalanceDTO
            {
                Activo = new BloqueInforme
                {
                    Titulo = "Activo",
                    Cuentas = saldos.Where(c => c.tipo == CD_Informes.Activo).ToList()
                },
                Pasivo = new BloqueInforme
                {
                    Titulo = "Pasivo",
                    Cuentas = saldos.Where(c => c.tipo == CD_Informes.Pasivo).ToList()
                },
                Patrimonio = new BloqueInforme
                {
                    Titulo = "Patrimonio neto",
                    Cuentas = saldos.Where(c => c.tipo == CD_Informes.Patrimonio).ToList()
                }
            };

            var resultados = Resultados(inicioEjercicio, hasta, sedeID, fondoID);
            balance.ResultadoDelEjercicio = resultados.Resultado;

            return balance;
        }

        // --------------------------------------------------------------------
        // Cuenta de Resultados
        // --------------------------------------------------------------------

        /// <summary>
        /// Qué ha entrado y qué ha salido en un periodo, y si sobra o falta dinero.
        /// </summary>
        public ResultadosDTO Resultados(DateTime desde, DateTime hasta, int? sedeID, int? fondoID)
        {
            var saldos = _cdInformes.SaldosPorCuenta(desde, hasta, sedeID, fondoID);

            return new ResultadosDTO
            {
                Ingresos = new BloqueInforme
                {
                    Titulo = "Ingresos",
                    Cuentas = saldos.Where(c => c.tipo == CD_Informes.Ingreso).ToList()
                },
                Gastos = new BloqueInforme
                {
                    Titulo = "Gastos",
                    Cuentas = saldos.Where(c => c.tipo == CD_Informes.Gasto).ToList()
                }
            };
        }

        // --------------------------------------------------------------------
        // Propuesta de cierre (D3)
        // --------------------------------------------------------------------

        /// <summary>Lo que hay que revisar antes de cerrar un ejercicio.</summary>
        public class PropuestaCierreDTO
        {
            public FiscalYear? Ejercicio { get; set; }
            public ResultadosDTO Resultados { get; set; } = new();
            public BalanceDTO Balance { get; set; } = new();

            /// <summary>Operaciones registradas que nunca llegaron a contabilizarse.</summary>
            public List<FinancialTransaction> SinContabilizar { get; set; } = new();

            /// <summary>Periodos que siguen abiertos dentro del ejercicio.</summary>
            public List<AccountingPeriod> PeriodosAbiertos { get; set; } = new();

            public decimal Resultado => Resultados.Resultado;
            public bool EsExcedente => Resultados.EsExcedente;

            /// <summary>
            /// Se puede cerrar cuando no queda nada sin contabilizar, todos los
            /// periodos están cerrados y el balance cuadra. Es una recomendación, no
            /// un candado: quien cierra es una persona.
            /// </summary>
            public bool ListoParaCerrar =>
                SinContabilizar.Count == 0 && PeriodosAbiertos.Count == 0 && Balance.Cuadra;
        }

        /// <summary>
        /// Prepara la propuesta de cierre de un ejercicio.
        /// </summary>
        /// <remarks>
        /// Decisión D3: la aplicación PROPONE y la asesoría valida. Aquí se calcula el
        /// resultado, el balance y lo que falta por hacer, pero NO se genera ningún
        /// asiento de cierre ni de apertura del siguiente ejercicio. Eso queda fuera
        /// de la fase 1 a propósito: qué cuentas intervienen en la regularización
        /// depende del país y de la asesoría.
        /// </remarks>
        public PropuestaCierreDTO PropuestaCierre(FiscalYear ejercicio,
                                                  List<AccountingPeriod> periodos,
                                                  int? sedeID)
        {
            return new PropuestaCierreDTO
            {
                Ejercicio = ejercicio,
                Resultados = Resultados(ejercicio.start_date, ejercicio.end_date, sedeID, null),
                Balance = Balance(ejercicio.end_date, sedeID, null, ejercicio.start_date),
                SinContabilizar = _cdInformes.OperacionesSinContabilizar(ejercicio.id),
                PeriodosAbiertos = periodos.Where(p => p.status == "open").ToList()
            };
        }

        // --------------------------------------------------------------------
        // Sumas y saldos (la comprobación de que todo cuadra)
        // --------------------------------------------------------------------

        /// <summary>
        /// Todas las cuentas con movimiento y sus totales. Es el informe que usa una
        /// asesoría para comprobar de un vistazo que la contabilidad cuadra: el total
        /// del Debe tiene que ser igual al del Haber.
        /// </summary>
        public List<CD_Informes.SaldoCuentaDTO> SumasYSaldos(DateTime desde, DateTime hasta,
                                                             int? sedeID, int? fondoID)
            => _cdInformes.SaldosPorCuenta(desde, hasta, sedeID, fondoID);
    }
}
