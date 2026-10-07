using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Plantillas del plan contable: un plan de cuentas guardado para poder aplicarlo.
    /// </summary>
    /// <remarks>
    /// Funciona como en Business Central: la iglesia se arma la plantilla que quiera
    /// (a mano, o copiando el plan que ya tiene), y luego la aplica. Aquí NO hay
    /// ninguna cuenta escrita en el código, igual que en el resto del módulo: lo que
    /// la plantilla contiene es lo que el usuario haya metido.
    ///
    /// Las dos tablas llevan organization_id, así que el filtro global por iglesia lo
    /// pone EF y no hace falta repetirlo en cada consulta de aquí.
    /// </remarks>
    public class CD_PlantillasPlan
    {
        private readonly AppDbContext _context;

        public CD_PlantillasPlan(AppDbContext context) => _context = context;

        public const string Borrador = "draft";
        public const string Publicada = "published";

        /// <summary>Una plantilla con el recuento de sus cuentas.</summary>
        public class PlantillaDTO
        {
            public int id { get; set; }
            public string? name { get; set; }
            public string? country_code { get; set; }
            public string? regime_code { get; set; }
            public int version { get; set; }
            public string? status { get; set; }
            public int cuentas { get; set; }
        }

        /// <summary>Resultado de aplicar una plantilla al plan de cuentas.</summary>
        public class ResultadoAplicarDTO
        {
            public int creadas { get; set; }
            /// <summary>Cuentas de la plantilla cuyo código ya existía en el plan.</summary>
            public List<string> omitidas { get; set; } = new();
            public string mensaje { get; set; } = "";
            public bool correcto { get; set; }
        }

        public List<PlantillaDTO> Listar()
        {
            var plantillas = _context.AccountingTemplates.AsNoTracking()
                .OrderBy(p => p.name).ToList();

            var recuentos = _context.AccountingTemplateAccounts.AsNoTracking()
                .GroupBy(c => c.template_id)
                .Select(g => new { template_id = g.Key, cuantas = g.Count() })
                .ToList()
                .ToDictionary(r => r.template_id, r => r.cuantas);

            return plantillas.Select(p => new PlantillaDTO
            {
                id = p.id,
                name = p.name,
                country_code = p.country_code,
                regime_code = p.regime_code,
                version = p.version,
                status = p.status,
                cuentas = recuentos.ContainsKey(p.id) ? recuentos[p.id] : 0
            }).ToList();
        }

        public AccountingTemplate? Obtener(int id)
            => _context.AccountingTemplates.AsNoTracking().FirstOrDefault(p => p.id == id);

        public List<AccountingTemplateAccount> CuentasDe(int idPlantilla)
            => _context.AccountingTemplateAccounts.AsNoTracking()
                .Where(c => c.template_id == idPlantilla)
                .OrderBy(c => c.code)
                .ToList();

        public bool ExisteNombre(string nombre, int idExcluir = 0)
            => _context.AccountingTemplates.Any(p => p.name == nombre && p.id != idExcluir);

        public int GuardarPlantilla(AccountingTemplate plantilla, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (plantilla.id == 0)
                {
                    if (plantilla.version <= 0) plantilla.version = 1;
                    _context.AccountingTemplates.Add(plantilla);
                }
                else
                {
                    var actual = _context.AccountingTemplates.FirstOrDefault(p => p.id == plantilla.id);
                    if (actual == null)
                    {
                        mensaje = "La plantilla no existe.";
                        return 0;
                    }
                    actual.name = plantilla.name;
                    actual.country_code = plantilla.country_code;
                    actual.regime_code = plantilla.regime_code;
                    actual.version = plantilla.version;
                    actual.status = plantilla.status;
                }

                _context.SaveChanges();
                mensaje = "Plantilla guardada.";
                return plantilla.id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar la plantilla: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool EliminarPlantilla(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var plantilla = _context.AccountingTemplates.FirstOrDefault(p => p.id == id);
                if (plantilla == null)
                {
                    mensaje = "La plantilla no existe.";
                    return false;
                }

                // Las líneas se borran con la plantilla: sin ella no significan nada, y
                // dejarlas huérfanas haría que el recuento de otra plantilla con el
                // mismo id reutilizado saliera mal.
                var cuentas = _context.AccountingTemplateAccounts
                    .Where(c => c.template_id == id).ToList();
                _context.AccountingTemplateAccounts.RemoveRange(cuentas);
                _context.AccountingTemplates.Remove(plantilla);

                _context.SaveChanges();
                mensaje = "Plantilla eliminada.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar la plantilla: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool ExisteCodigoEnPlantilla(int idPlantilla, string codigo, int idExcluir = 0)
            => _context.AccountingTemplateAccounts
                .Any(c => c.template_id == idPlantilla && c.code == codigo && c.id != idExcluir);

        public bool GuardarCuenta(AccountingTemplateAccount cuenta, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (cuenta.id == 0)
                {
                    _context.AccountingTemplateAccounts.Add(cuenta);
                }
                else
                {
                    var actual = _context.AccountingTemplateAccounts.FirstOrDefault(c => c.id == cuenta.id);
                    if (actual == null)
                    {
                        mensaje = "La cuenta de la plantilla no existe.";
                        return false;
                    }
                    actual.code = cuenta.code;
                    actual.name = cuenta.name;
                    actual.account_type = cuenta.account_type;
                    actual.parent_code = cuenta.parent_code;
                    actual.is_postable = cuenta.is_postable;
                    actual.normal_balance = cuenta.normal_balance;
                }

                _context.SaveChanges();
                mensaje = "Cuenta guardada.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar la cuenta: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        public bool EliminarCuenta(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var cuenta = _context.AccountingTemplateAccounts.FirstOrDefault(c => c.id == id);
                if (cuenta == null)
                {
                    mensaje = "La cuenta no existe.";
                    return false;
                }
                _context.AccountingTemplateAccounts.Remove(cuenta);
                _context.SaveChanges();
                mensaje = "Cuenta eliminada.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar la cuenta: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        // --------------------------------------------------------------------
        // Copiar el plan actual a una plantilla
        // --------------------------------------------------------------------

        /// <summary>
        /// Crea una plantilla con una copia del plan de cuentas de la iglesia.
        /// </summary>
        /// <remarks>
        /// Es el camino que de verdad se usa: nadie teclea 80 cuentas en una plantilla,
        /// pero sí quiere guardar el plan que ya tiene montado para reaprovecharlo en
        /// otra iglesia o conservarlo antes de tocarlo.
        ///
        /// El padre se guarda por CÓDIGO, no por id: los identificadores de la iglesia
        /// de origen no valen nada en la de destino.
        /// </remarks>
        public int CrearDesdeMiPlan(string nombre, string? pais, string? regimen, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var cuentas = _context.LedgerAccounts.AsNoTracking().OrderBy(c => c.code).ToList();
                if (cuentas.Count == 0)
                {
                    mensaje = "No hay ninguna cuenta en el plan, así que no hay nada que copiar.";
                    return 0;
                }

                var plantilla = new AccountingTemplate
                {
                    name = nombre,
                    country_code = pais,
                    regime_code = regimen,
                    version = 1,
                    status = Borrador
                };
                _context.AccountingTemplates.Add(plantilla);
                _context.SaveChanges();   // hace falta el id para las líneas

                // Diccionario id -> código, para traducir parent_account_id a parent_code
                var codigoPorId = cuentas.ToDictionary(c => c.id, c => c.code);

                foreach (var cuenta in cuentas)
                {
                    _context.AccountingTemplateAccounts.Add(new AccountingTemplateAccount
                    {
                        template_id = plantilla.id,
                        code = cuenta.code,
                        name = cuenta.name,
                        account_type = cuenta.account_type,
                        // Si el padre no está en el plan (no debería pasar, pero los datos
                        // mandan), la cuenta se guarda sin padre en vez de perderse.
                        parent_code = cuenta.parent_account_id.HasValue
                                      && codigoPorId.ContainsKey(cuenta.parent_account_id.Value)
                            ? codigoPorId[cuenta.parent_account_id.Value]
                            : null,
                        is_postable = cuenta.is_postable,
                        normal_balance = cuenta.normal_balance
                    });
                }

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = $"Plantilla creada con {cuentas.Count} cuentas copiadas del plan actual.";
                return plantilla.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al crear la plantilla: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Duplica una plantilla. Útil para partir de una y cambiar cuatro cosas.
        /// </summary>
        public int Duplicar(int idPlantilla, string nombreNuevo, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var origen = _context.AccountingTemplates.AsNoTracking()
                    .FirstOrDefault(p => p.id == idPlantilla);
                if (origen == null)
                {
                    mensaje = "La plantilla que quieres duplicar no existe.";
                    return 0;
                }

                var copia = new AccountingTemplate
                {
                    name = nombreNuevo,
                    country_code = origen.country_code,
                    regime_code = origen.regime_code,
                    version = 1,
                    status = Borrador
                };
                _context.AccountingTemplates.Add(copia);
                _context.SaveChanges();

                foreach (var c in CuentasDe(idPlantilla))
                {
                    _context.AccountingTemplateAccounts.Add(new AccountingTemplateAccount
                    {
                        template_id = copia.id,
                        code = c.code,
                        name = c.name,
                        account_type = c.account_type,
                        parent_code = c.parent_code,
                        is_postable = c.is_postable,
                        normal_balance = c.normal_balance
                    });
                }

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Plantilla duplicada.";
                return copia.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al duplicar la plantilla: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        // --------------------------------------------------------------------
        // Aplicar una plantilla al plan de cuentas
        // --------------------------------------------------------------------

        /// <summary>
        /// Crea en el plan de cuentas las cuentas de una plantilla que todavía no existan.
        /// </summary>
        /// <remarks>
        /// Tres decisiones que importan:
        ///
        /// 1. Lo que ya existe NO se toca. Si el código ya está en el plan, se omite y
        ///    se informa. Sobrescribir podría cambiar el tipo o el saldo normal de una
        ///    cuenta que ya tiene asientos, y eso daría la vuelta a informes ya emitidos.
        ///
        /// 2. Se aplica en orden de código, de corto a largo, para que el padre exista
        ///    siempre antes que la hija. Ordenar solo alfabéticamente no lo garantiza.
        ///
        /// 3. Todo dentro de una transacción: media plantilla aplicada es peor que
        ///    ninguna, porque deja un árbol con padres que faltan.
        /// </remarks>
        public ResultadoAplicarDTO AplicarAMiPlan(int idPlantilla, int? idUsuario)
        {
            var resultado = new ResultadoAplicarDTO();

            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                var deLaPlantilla = CuentasDe(idPlantilla);
                if (deLaPlantilla.Count == 0)
                {
                    resultado.mensaje = "La plantilla no tiene ninguna cuenta.";
                    return resultado;
                }

                // Lo que ya hay en el plan, por código
                var existentes = _context.LedgerAccounts
                    .ToDictionary(c => c.code ?? "", c => c);

                // Padre antes que hija: primero las de código más corto.
                var enOrden = deLaPlantilla
                    .OrderBy(c => (c.code ?? "").Length)
                    .ThenBy(c => c.code)
                    .ToList();

                foreach (var plantilla in enOrden)
                {
                    string codigo = plantilla.code ?? "";
                    if (string.IsNullOrWhiteSpace(codigo)) continue;

                    if (existentes.ContainsKey(codigo))
                    {
                        resultado.omitidas.Add(codigo);
                        continue;
                    }

                    // El padre se busca por código entre lo que ya hay (incluidas las
                    // cuentas creadas en esta misma pasada, que están en el diccionario).
                    int? idPadre = null;
                    sbyte nivel = 1;
                    if (!string.IsNullOrWhiteSpace(plantilla.parent_code)
                        && existentes.TryGetValue(plantilla.parent_code!, out var padre))
                    {
                        idPadre = padre.id;
                        // El nivel se deduce del padre y no se copia de la plantilla: así
                        // sale coherente aunque la plantilla viniera con niveles mal puestos.
                        nivel = (sbyte)Math.Min(padre.level + 1, sbyte.MaxValue);
                    }

                    var nueva = new LedgerAccount
                    {
                        code = codigo,
                        name = plantilla.name,
                        account_type = plantilla.account_type,
                        parent_account_id = idPadre,
                        level = nivel,
                        is_postable = plantilla.is_postable,
                        normal_balance = plantilla.normal_balance,
                        status = CD_PlanCuentas.Activa,
                        created_at = DateTime.UtcNow,
                        created_by = idUsuario,
                        row_version = 1
                    };

                    _context.LedgerAccounts.Add(nueva);
                    // Hace falta guardar aquí para tener el id disponible como padre de
                    // las cuentas que vengan después en esta misma pasada.
                    _context.SaveChanges();

                    existentes[codigo] = nueva;
                    resultado.creadas++;
                }

                transaccion.Commit();

                resultado.correcto = true;
                resultado.mensaje = resultado.omitidas.Count == 0
                    ? $"Se han creado {resultado.creadas} cuentas."
                    : $"Se han creado {resultado.creadas} cuentas. " +
                      $"{resultado.omitidas.Count} ya existían y no se han tocado.";
                return resultado;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                resultado.correcto = false;
                resultado.creadas = 0;
                resultado.mensaje = "Error al aplicar la plantilla, no se ha creado ninguna cuenta: "
                                    + ErrorHelper.Mensaje(ex);
                return resultado;
            }
        }
    }
}
