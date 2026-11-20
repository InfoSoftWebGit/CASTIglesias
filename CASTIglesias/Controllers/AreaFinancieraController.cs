using CapaNegocio;
using Permisos = CapaEntidad.Permisos;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Base de TODOS los controladores del área financiera.
    /// </summary>
    /// <remarks>
    /// Dos cosas se resuelven aquí y en ningún otro sitio:
    ///
    /// 1. El control de acceso. El área financiera se vende aparte, así que una
    ///    iglesia que no la tenga contratada no puede entrar ni escribiendo la URL
    ///    a mano. Al heredar de esta clase, cualquier pantalla financiera que se
    ///    añada en el futuro queda protegida sin que haya que acordarse de nada.
    ///
    /// 2. Qué menú lateral se pinta. El área NO se guarda en la sesión: se deduce
    ///    de en qué controlador estás. Así no puede quedarse "pegada" en un estado
    ///    que no corresponde, el botón atrás del navegador funciona y no hay nada
    ///    que sincronizar entre pestañas.
    /// </remarks>
    [Authorize]
    public abstract class AreaFinancieraController : BaseController
    {
        protected readonly CN_Plataforma _negocioPlataforma;

        protected AreaFinancieraController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                           CN_Plataforma negocioPlataforma)
            : base(negocioSedes, negocioPermisos)
        {
            _negocioPlataforma = negocioPlataforma;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // La base prepara sedes, permisos e iglesia activa. Si decidió redirigir
            // (sesión caducada, sede retirada), se respeta y no se sigue.
            base.OnActionExecuting(context);
            if (context.Result != null) return;

            if (!(HttpContext.User.Identity?.IsAuthenticated ?? false)) return;

            int idIglesia = SesionClaims.ObtenerIdIglesia(HttpContext.User);

            if (!_negocioPlataforma.TieneModuloFinanzas(idIglesia))
            {
                // Se vuelve al área de congregación con un aviso, en lugar de dar un
                // error: para el usuario esto no es un fallo, es que no lo ha contratado.
                TempData["MensajeAcceso"] = "Tu iglesia no tiene contratada el área financiera.";
                context.Result = RedirectToAction("Index", "Home");
                return;
            }

            // Permiso de entrada al área (punto 3.2). Se comprueba AQUÍ y no con
            // [RequierePermiso] en cada controlador por el mismo motivo que el módulo:
            // así cualquier pantalla financiera que se añada en el futuro queda
            // protegida sin que haya que acordarse de nada.
            //
            // Esto solo cierra la puerta de entrada. Lo que se puede HACER dentro
            // (contabilizar, revertir, aprobar, configurar, ver donantes) lleva su
            // propio permiso en cada acción, porque un tesorero puede necesitar
            // apuntar sin poder tocar el plan de cuentas.
            if (!TienePermiso(nameof(CapaEntidad.Permisos.FinanzasVer)))
            {
                context.Result = Filters.RequierePermisoAttribute
                    .RespuestaSinPermiso(HttpContext, this);
                return;
            }

            // Permiso de la pantalla concreta. Solo se mira en las acciones que pintan
            // una pantalla (las de PermisoPorPantalla): las llamadas AJAX que hacen
            // unas pantallas a controladores de otras (buscar conceptos, terceros,
            // cuentas...) siguen dependiendo solo de FinanzasVer y de los permisos de
            // cada acción, y no se rompen por no tener la otra pantalla.
            if (context.ActionDescriptor is ControllerActionDescriptor accion)
            {
                var permisoPantalla = ObtenerPermisoDePantalla(accion.ControllerName, accion.ActionName);
                if (permisoPantalla != null && !TienePermiso(permisoPantalla))
                {
                    context.Result = Filters.RequierePermisoAttribute
                        .RespuestaSinPermiso(HttpContext, this);
                    return;
                }
            }

            // Lo lee el _Layout para pintar el menú financiero en vez del habitual
            ViewBag.AreaFinanciera = true;
        }

        /// <summary>
        /// Pantallas del área financiera y el permiso que pide cada una. La clave es
        /// "Controlador/Acción" sin el sufijo Controller. Lo usa también el menú
        /// financiero, para no pintar enlaces que acabarían en "sin permiso".
        /// </summary>
        /// <remarks>
        /// Una pantalla nueva que no se añada aquí queda abierta a quien tenga
        /// FinanzasVer, como todas lo estaban antes de los permisos por pantalla.
        /// El panel (Finanzas/Index) no está a propósito: es la portada del área.
        /// </remarks>
        public static readonly IReadOnlyDictionary<string, string> PermisoPorPantalla =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Operaciones
                ["FinanzasOperaciones/Ingresos"] = nameof(Permisos.FinanzasVerIngresos),
                ["FinanzasOperaciones/Gastos"] = nameof(Permisos.FinanzasVerGastos),
                ["FinanzasTransferencias/Index"] = nameof(Permisos.FinanzasVerTransferencias),
                ["FinanzasPagos/Index"] = nameof(Permisos.FinanzasVerPagos),
                ["FinanzasArqueos/Index"] = nameof(Permisos.FinanzasVerArqueos),

                // Contabilidad
                ["FinanzasDiario/Index"] = nameof(Permisos.FinanzasVerDiario),
                ["FinanzasInformes/Mayor"] = nameof(Permisos.FinanzasVerMayor),
                ["FinanzasInformes/SumasYSaldos"] = nameof(Permisos.FinanzasVerSumasYSaldos),
                ["FinanzasInformes/Balance"] = nameof(Permisos.FinanzasVerBalance),
                ["FinanzasInformes/Resultados"] = nameof(Permisos.FinanzasVerResultados),
                ["FinanzasInformes/Cierre"] = nameof(Permisos.FinanzasVerCierre),

                // Gestión
                ["FinanzasPresupuestos/Index"] = nameof(Permisos.FinanzasVerPresupuestos),
                ["FinanzasPresupuestos/Seguimiento"] = nameof(Permisos.FinanzasVerPresupuestos),
                ["FinanzasCertificados/Index"] = nameof(Permisos.FinanzasVerCertificados),
                ["FinanzasCertificados/Certificado"] = nameof(Permisos.FinanzasVerCertificados),
                ["FinanzasAprobaciones/Index"] = nameof(Permisos.FinanzasVerAprobaciones),
                ["FinanzasConciliacion/Index"] = nameof(Permisos.FinanzasVerConciliacion),
                ["FinanzasAuditoria/Index"] = nameof(Permisos.FinanzasVerAuditoria),

                // Configuración
                ["FinanzasConceptos/Index"] = nameof(Permisos.FinanzasVerConceptos),
                ["FinanzasTesoreria/Index"] = nameof(Permisos.FinanzasVerTesoreria),
                ["FinanzasFondos/Index"] = nameof(Permisos.FinanzasVerFondos),
                ["FinanzasPlanCuentas/Index"] = nameof(Permisos.FinanzasVerPlanCuentas),
                ["FinanzasPlantillas/Index"] = nameof(Permisos.FinanzasVerPlantillas),
                ["FinanzasPlantillas/Cuentas"] = nameof(Permisos.FinanzasVerPlantillas),
                ["FinanzasEjercicios/Index"] = nameof(Permisos.FinanzasVerEjercicios),
                ["FinanzasTerceros/Index"] = nameof(Permisos.FinanzasVerTerceros),
                ["FinanzasProyectos/Index"] = nameof(Permisos.FinanzasVerProyectos),
                ["FinanzasReglas/Index"] = nameof(Permisos.FinanzasVerReglas),
                ["FinanzasNumeraciones/Index"] = nameof(Permisos.FinanzasVerNumeraciones),
                ["FinanzasApertura/Index"] = nameof(Permisos.FinanzasVerApertura),
            };

        /// <summary>Permiso que pide una pantalla financiera, o null si no tiene uno propio.</summary>
        public static string? ObtenerPermisoDePantalla(string controlador, string accion) =>
            PermisoPorPantalla.TryGetValue($"{controlador}/{accion}", out var permiso) ? permiso : null;
    }
}
