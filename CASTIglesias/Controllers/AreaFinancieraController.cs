using CapaNegocio;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

            // Lo lee el _Layout para pintar el menú financiero en vez del habitual
            ViewBag.AreaFinanciera = true;
        }
    }
}
