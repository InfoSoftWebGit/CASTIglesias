using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Plantillas del plan contable: crearlas, rellenarlas y aplicarlas.
    /// </summary>
    /// <remarks>
    /// Pedido explícitamente: la plantilla la crea cada quien como quiera, igual que
    /// en Business Central. Por eso aquí no se valida que la plantilla "parezca" un
    /// plan español, ni se exige ningún código concreto: solo que cada cuenta tenga
    /// lo imprescindible para que el motor contable pueda usarla.
    ///
    /// Lo imprescindible son tres cosas, y no son un capricho:
    ///   - el tipo (activo, pasivo, patrimonio, ingreso, gasto), porque es lo único
    ///     que mira el Balance y la Cuenta de Resultados para colocar la cuenta;
    ///   - el saldo normal (deudora o acreedora), porque es lo que decide el signo;
    ///   - el código, porque es la identidad de la cuenta y lo que une padre e hija.
    /// </remarks>
    public class CN_PlantillasPlan
    {
        private readonly CD_PlantillasPlan _cdPlantillas;

        public CN_PlantillasPlan(CD_PlantillasPlan cdPlantillas) => _cdPlantillas = cdPlantillas;

        /// <summary>Tipos de cuenta válidos. Mismos valores que el plan de cuentas.</summary>
        public static readonly string[] TiposCuenta =
            { "asset", "liability", "equity", "income", "expense", "memorandum" };

        /// <summary>Saldos normales válidos.</summary>
        public static readonly string[] SaldosNormales = { "debit", "credit" };

        public List<CD_PlantillasPlan.PlantillaDTO> Listar() => _cdPlantillas.Listar();
        public AccountingTemplate? Obtener(int id) => _cdPlantillas.Obtener(id);
        public List<AccountingTemplateAccount> CuentasDe(int id) => _cdPlantillas.CuentasDe(id);

        public int GuardarPlantilla(AccountingTemplate plantilla, out string mensaje)
        {
            mensaje = string.Empty;

            plantilla.name = plantilla.name?.Trim();
            if (string.IsNullOrWhiteSpace(plantilla.name))
            {
                mensaje = "La plantilla necesita un nombre.";
                return 0;
            }

            if (_cdPlantillas.ExisteNombre(plantilla.name, plantilla.id))
            {
                mensaje = "Ya tienes otra plantilla con ese nombre.";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(plantilla.status))
                plantilla.status = CD_PlantillasPlan.Borrador;

            return _cdPlantillas.GuardarPlantilla(plantilla, out mensaje);
        }

        public bool EliminarPlantilla(int id, out string mensaje)
            => _cdPlantillas.EliminarPlantilla(id, out mensaje);

        /// <summary>Añade o modifica una cuenta de la plantilla.</summary>
        public bool GuardarCuenta(AccountingTemplateAccount cuenta, out string mensaje)
        {
            mensaje = string.Empty;

            cuenta.code = cuenta.code?.Trim();
            cuenta.name = cuenta.name?.Trim();
            cuenta.parent_code = string.IsNullOrWhiteSpace(cuenta.parent_code)
                ? null : cuenta.parent_code.Trim();

            if (string.IsNullOrWhiteSpace(cuenta.code))
            {
                mensaje = "La cuenta necesita un código.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(cuenta.name))
            {
                mensaje = "La cuenta necesita un nombre.";
                return false;
            }

            if (!TiposCuenta.Contains(cuenta.account_type))
            {
                mensaje = "El tipo de cuenta no es válido.";
                return false;
            }

            if (!SaldosNormales.Contains(cuenta.normal_balance))
            {
                mensaje = "Hay que indicar si la cuenta es deudora o acreedora.";
                return false;
            }

            if (_cdPlantillas.ExisteCodigoEnPlantilla(cuenta.template_id, cuenta.code, cuenta.id))
            {
                mensaje = "Ya hay otra cuenta con ese código en esta plantilla.";
                return false;
            }

            // Una cuenta no puede ser su propia madre: el árbol no tendría raíz y
            // aplicar la plantilla dejaría la cuenta sin colocar.
            if (cuenta.parent_code == cuenta.code)
            {
                mensaje = "Una cuenta no puede tener como padre a sí misma.";
                return false;
            }

            return _cdPlantillas.GuardarCuenta(cuenta, out mensaje);
        }

        public bool EliminarCuenta(int id, out string mensaje)
            => _cdPlantillas.EliminarCuenta(id, out mensaje);

        /// <summary>Guarda el plan de cuentas actual como plantilla.</summary>
        public int CrearDesdeMiPlan(string nombre, string? pais, string? regimen, out string mensaje)
        {
            mensaje = string.Empty;

            nombre = nombre?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(nombre))
            {
                mensaje = "La plantilla necesita un nombre.";
                return 0;
            }

            if (_cdPlantillas.ExisteNombre(nombre))
            {
                mensaje = "Ya tienes otra plantilla con ese nombre.";
                return 0;
            }

            return _cdPlantillas.CrearDesdeMiPlan(nombre, pais, regimen, out mensaje);
        }

        public int Duplicar(int id, string nombreNuevo, out string mensaje)
        {
            mensaje = string.Empty;

            nombreNuevo = nombreNuevo?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(nombreNuevo))
            {
                mensaje = "La copia necesita un nombre.";
                return 0;
            }

            if (_cdPlantillas.ExisteNombre(nombreNuevo))
            {
                mensaje = "Ya tienes otra plantilla con ese nombre.";
                return 0;
            }

            return _cdPlantillas.Duplicar(id, nombreNuevo, out mensaje);
        }

        /// <summary>
        /// Comprueba la plantilla antes de aplicarla y avisa de lo que no cuadra.
        /// </summary>
        /// <remarks>
        /// Se avisa, no se bloquea, salvo en lo que de verdad rompería el plan. Una
        /// cuenta cuyo padre no está en la plantilla se puede aplicar igual: quedará
        /// como cuenta de primer nivel, que es raro pero no roto, y el usuario decide.
        /// </remarks>
        public List<string> Revisar(int idPlantilla)
        {
            var avisos = new List<string>();
            var cuentas = _cdPlantillas.CuentasDe(idPlantilla);

            if (cuentas.Count == 0)
            {
                avisos.Add("La plantilla no tiene ninguna cuenta.");
                return avisos;
            }

            var codigos = cuentas.Select(c => c.code).Where(c => c != null).ToHashSet();

            foreach (var c in cuentas.Where(c => !string.IsNullOrWhiteSpace(c.parent_code)
                                              && !codigos.Contains(c.parent_code)))
            {
                avisos.Add($"La cuenta {c.code} dice tener como padre a {c.parent_code}, " +
                           "que no está en la plantilla. Se creará como cuenta de primer nivel.");
            }

            if (!cuentas.Any(c => c.is_postable))
            {
                avisos.Add("Ninguna cuenta de la plantilla admite apuntes, así que no se " +
                           "podría contabilizar nada con ella.");
            }

            return avisos;
        }

        /// <summary>Crea en el plan las cuentas de la plantilla que falten.</summary>
        public CD_PlantillasPlan.ResultadoAplicarDTO AplicarAMiPlan(int idPlantilla, int? idUsuario)
            => _cdPlantillas.AplicarAMiPlan(idPlantilla, idUsuario);
    }
}
