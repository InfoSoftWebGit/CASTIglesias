using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Arqueos de caja: contar el efectivo y justificar la diferencia.
    /// </summary>
    /// <remarks>
    /// Solo se arquean las cajas de EFECTIVO. Arquear un banco no significa nada:
    /// el dinero no se cuenta a mano, se concilia con el extracto, que es otra
    /// pantalla y otro procedimiento.
    /// </remarks>
    public class CN_Arqueos
    {
        private readonly CD_Arqueos _cdArqueos;
        private readonly CD_Tesoreria _cdTesoreria;

        public CN_Arqueos(CD_Arqueos cdArqueos, CD_Tesoreria cdTesoreria)
        {
            _cdArqueos = cdArqueos;
            _cdTesoreria = cdTesoreria;
        }

        /// <summary>Denominaciones del euro, de mayor a menor.</summary>
        /// <remarks>
        /// Están aquí y no en la vista porque son un dato del dominio, no de la
        /// presentación: el día que haya que contar otra moneda, se cambia en un sitio.
        /// Ver la memoria de expansión internacional.
        /// </remarks>
        public static readonly decimal[] Denominaciones =
            { 500m, 200m, 100m, 50m, 20m, 10m, 5m, 2m, 1m, 0.50m, 0.20m, 0.10m, 0.05m, 0.02m, 0.01m };

        public List<CD_Arqueos.ArqueoDTO> Listar(int? sedeID, string? estado)
            => _cdArqueos.Listar(sedeID, estado);

        public CashSession? Obtener(int id) => _cdArqueos.Obtener(id);
        public List<CashCountLine> LineasDe(int id) => _cdArqueos.LineasDe(id);
        public CashSession? AbiertaDe(int idCaja) => _cdArqueos.AbiertaDe(idCaja);
        public decimal SaldoContable(int idCaja) => _cdArqueos.SaldoContable(idCaja);

        /// <summary>Cajas que se pueden arquear: las de efectivo y activas.</summary>
        public List<TreasuryAccount> CajasArqueables()
            => _cdTesoreria.ListarActivas().Where(c => c.account_type == "cash").ToList();

        public int Abrir(int idCaja, int idSede, int idUsuario, out string mensaje)
        {
            mensaje = string.Empty;

            var caja = _cdTesoreria.Obtener(idCaja);
            if (caja == null)
            {
                mensaje = "La caja no existe.";
                return 0;
            }
            if (caja.account_type != "cash")
            {
                mensaje = "Solo se arquean las cajas de efectivo. Un banco se concilia con su extracto.";
                return 0;
            }
            if (idSede <= 0 || idSede == CapaEntidad.Sedes.TodasLasSedes)
            {
                mensaje = "Hay que trabajar en una sede concreta para arquear una caja.";
                return 0;
            }

            return _cdArqueos.Abrir(idCaja, idSede, idUsuario, out mensaje);
        }

        /// <summary>Cierra el arqueo con su recuento.</summary>
        public bool Cerrar(int idSesion, Dictionary<decimal, int> recuento, int idUsuario,
                           int? idValidador, string? notas, out string mensaje)
        {
            mensaje = string.Empty;

            var sesion = _cdArqueos.Obtener(idSesion);
            if (sesion == null)
            {
                mensaje = "El arqueo no existe.";
                return false;
            }

            // El validador no puede ser quien cierra: es la razón de ser del segundo par
            // de ojos. Se comprueba aquí y también al validar.
            if (idValidador.HasValue && idValidador.Value == idUsuario)
            {
                mensaje = "El segundo validador tiene que ser otra persona.";
                return false;
            }

            var lineas = recuento
                .Where(r => r.Value > 0)
                .Select(r => new CashCountLine { denomination = r.Key, quantity = r.Value })
                .ToList();

            decimal contado = lineas.Sum(l => l.denomination * l.quantity);
            decimal esperado = _cdArqueos.SaldoContable(sesion.treasury_account_id);

            // Si hay diferencia hay que explicarla. Es la regla que impide que un
            // descuadre se cierre en silencio y aparezca meses después sin contexto.
            if (contado != esperado && string.IsNullOrWhiteSpace(notas))
            {
                mensaje = $"La caja no cuadra: contado {contado:N2} frente a {esperado:N2} esperados. " +
                          "Hay que explicar la diferencia antes de cerrar.";
                return false;
            }

            return _cdArqueos.Cerrar(idSesion, lineas, idUsuario, idValidador, notas, out mensaje);
        }

        public bool Validar(int idSesion, int idUsuario, out string mensaje)
            => _cdArqueos.Validar(idSesion, idUsuario, out mensaje);
    }
}
