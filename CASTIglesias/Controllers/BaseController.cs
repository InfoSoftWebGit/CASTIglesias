using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using CapaNegocio;
using CapaEntidad;
using System.Collections.Generic;
using CASTIglesias.Models; // SesionClaims

namespace CASTIglesias.Controllers
{
    public class BaseController : Controller
    {
        protected readonly CN_Sedes _negocioSedes;
        protected readonly CN_Permisos _negocioPermisos;

        public BaseController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos)
        {
            _negocioSedes = negocioSedes;
            _negocioPermisos = negocioPermisos;
        }

        // EL TIPO DE RETORNO CAMBIA DE 'int?' a 'int'
        protected int ObtenerIdSedeUsuario()
        {
            var sedeClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IDsede");

            // Usamos 'sedeID' como nombre de parámetro
            if (sedeClaim != null && int.TryParse(sedeClaim.Value, out int sedeID))
            {
                // Si el Claim es 0 (antiguo Admin Global), lo convertimos al nuevo estándar 1000.
                if (sedeID == 0) return 1000;

                // Si es un ID de sede válido (positivo), lo devolvemos.
                if (sedeID > 0) return sedeID;
            }

            // Si el usuario es Admin Global o Pastor General pero el claim no está configurado (debería ser '0' en el claim)
            if (HttpContext.User.IsInRole("AdminGlobal") || HttpContext.User.IsInRole("PastorGeneral"))
            {
                return 1000;
            }

            // Si el usuario no está autenticado o no tiene Claim válido y no es global.
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("ID de sede del usuario no encontrado, inválido o no configurado en la sesión.");
            }
            throw new UnauthorizedAccessException("El usuario no está autenticado.");
        }

        // El parámetro cambia de 'int?' a 'int'
        protected string ObtenerNombreSede(int sedeID)
        {
            if (sedeID == 1000)
            {
                return "Todas las Sedes";
            }

            // Buscamos la sede por su ID.
            var sede = _negocioSedes.ListarSedes().FirstOrDefault(s => s.ID == sedeID);

            return sede?.nombre_sede ?? "Sede Desconocida";
        }

        /// <summary>
        /// Sedes a las que puede entrar el usuario en esta petición.
        /// </summary>
        /// <remarks>
        /// Lo rellena OnActionExecuting; si algo falla se queda en "ninguna", de modo que
        /// ante la duda se deniega (por ejemplo, en CambiarSede).
        /// </remarks>
        protected AccesoSedes AccesoSedesActual { get; private set; } = AccesoSedes.Ninguna;

        /// <summary>
        /// Comprueba permisos dentro de la acción, cuando dependen del registro (por
        /// ejemplo, del estado de un congregante) y no bastan con [RequierePermiso].
        /// </summary>
        /// <returns>true si tiene al menos uno de los permisos indicados.</returns>
        protected bool TienePermiso(params string[] permisos) =>
            Filters.RequierePermisoAttribute.TieneAlguno(
                Filters.RequierePermisoAttribute.ObtenerPermisos(HttpContext), permisos);

        /// <summary>Respuesta JSON estándar de "sin permiso" (ver RequierePermisoAttribute).</summary>
        protected JsonResult SinPermiso() => Filters.RequierePermisoAttribute.JsonSinPermiso();

        private AccesoSedes CalcularAccesoSedes()
        {
            // Roles globales y administrador de plataforma: sin consulta. Para este último
            // es obligatorio, porque su fila de usuario es de la iglesia interna de Congrega
            // y no tiene nada que ver con la iglesia en la que está trabajando.
            // El claim Multisede NO se usa aquí: viene de usuario_sedes y, si se retira,
            // debe dejar de valer sin esperar a que el usuario vuelva a entrar.
            var usuario = HttpContext.User;
            if (SesionClaims.EsAdminPlataforma(usuario) || usuario.IsInRole("AdminGlobal") || usuario.IsInRole("PastorGeneral"))
                return AccesoSedes.Todas(ObtenerIdSedeUsuario());

            // Se pide al contenedor para no cambiar el constructor de todos los controladores
            var negocioUsuarioSedes = HttpContext.RequestServices.GetRequiredService<CN_UsuarioSedes>();
            return negocioUsuarioSedes.ObtenerAcceso(SesionClaims.ObtenerIdUsuario(HttpContext.User));
        }

        // ✅ Se ejecuta antes de cada acción para preparar ViewBag
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (HttpContext.User.Identity.IsAuthenticated)
            {
                // Sesiones iniciadas antes de existir el claim de iglesia: sin él, el filtro
                // global no devuelve nada y la app parecería vacía. Se obliga a entrar de nuevo.
                if (SesionClaims.ObtenerIdIglesia(HttpContext.User) == 0)
                {
                    context.Result = RedirectToAction("CerrarSesion", "Acceso");
                    return;
                }

                try
                {
                    // 1. Obtener la sede actual.
                    int sedeIDActual = ObtenerIdSedeUsuario();
                    string nombreSedeActual;
                    List<Sedes> listaSedes;

                    // Sedes permitidas en esta petición. Se recalculan siempre para que un
                    // cambio en usuario_sedes se aplique sin tener que volver a entrar.
                    AccesoSedesActual = CalcularAccesoSedes();

                    // Si le han quitado la sede en la que está trabajando, se cierra la
                    // sesión: al volver a entrar recibe su sede principal.
                    if (!AccesoSedesActual.Permite(sedeIDActual))
                    {
                        context.Result = RedirectToAction("CerrarSesion", "Acceso");
                        return;
                    }

                    var nombreClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "NombreSedeActual");

                    if (nombreClaim != null && sedeIDActual != 1000)
                    {
                        nombreSedeActual = nombreClaim.Value;
                    }
                    else
                    {
                        nombreSedeActual = ObtenerNombreSede(sedeIDActual);
                    }

                    if (AccesoSedesActual.TodasLasSedes)
                    {
                        // ListarSedes ya viene limitado a la iglesia activa y sin la fila 1000;
                        // la opción "Todas las Sedes" se añade aquí para que siempre signifique
                        // "todas las de esta iglesia".
                        listaSedes = new List<Sedes>
                        {
                            new Sedes { ID = Sedes.TodasLasSedes, nombre_sede = "Todas las Sedes" }
                        };
                        listaSedes.AddRange(_negocioSedes.ListarSedes() ?? new List<Sedes>());
                    }
                    else if (AccesoSedesActual.Sedes.Count > 1)
                    {
                        // Varias sedes concretas: sin "Todas las Sedes", porque las consultas
                        // solo saben filtrar por una sede o por todas, no por un grupo.
                        listaSedes = (_negocioSedes.ListarSedes() ?? new List<Sedes>())
                            .Where(s => AccesoSedesActual.Sedes.Contains(s.ID))
                            .ToList();
                    }
                    else
                    {
                        listaSedes = new List<Sedes>();
                    }



                    Permisos permisosUsuario = null;

                    var idUsuarioClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    if (idUsuarioClaim != null && int.TryParse(idUsuarioClaim.Value, out int idUsuario))
                    {
                        permisosUsuario = _negocioPermisos.ObtenerPermisosDeSesion(idUsuario);
                    }
                    ViewBag.NombreIglesiaActual = HttpContext.User.FindFirst(SesionClaims.NombreIglesia)?.Value;
                    ViewBag.EsAdminPlataforma = SesionClaims.EsAdminPlataforma(HttpContext.User);
                    ViewBag.SedeIDActual = sedeIDActual;
                    ViewBag.NombreSedeActual = nombreSedeActual;
                    ViewBag.ListaSedes = listaSedes;
                    ViewBag.MostrarSelectorSedes = listaSedes.Count > 1;
                    ViewBag.PermisosMiembro = permisosUsuario ?? new Permisos();
                    // Lo reutiliza [RequierePermiso], que se ejecuta después de este método
                    HttpContext.Items[Filters.RequierePermisoAttribute.ClaveItems] = ViewBag.PermisosMiembro;

                    // El botón "Área financiera" solo se pinta si la iglesia la tiene
                    // contratada. Es presentación: quien de verdad corta el paso es
                    // AreaFinancieraController, que lo comprueba en cada petición.
                    // Se pide al contenedor para no cambiar el constructor de todos
                    // los controladores, igual que CN_UsuarioSedes.
                    var negocioPlataforma = HttpContext.RequestServices.GetRequiredService<CN_Plataforma>();
                    ViewBag.TieneModuloFinanzas =
                        negocioPlataforma.TieneModuloFinanzas(SesionClaims.ObtenerIdIglesia(HttpContext.User));
                }
                catch (UnauthorizedAccessException)
                {
                    ViewBag.ListaSedes = new List<Sedes>();
                    ViewBag.SedeIDActual = null;
                    ViewBag.NombreSedeActual = "****";
                    ViewBag.PermisosMiembro = new Permisos();
                }
                catch (Exception)
                {
                    ViewBag.ListaSedes = new List<Sedes>();
                    ViewBag.SedeIDActual = null;
                    ViewBag.NombreSedeActual = "Error DB";
                    ViewBag.PermisosMiembro = new Permisos();
                }
            }
        }

        // ✅ Acción para cambiar de sede dinámicamente
        [HttpPost]
        [ValidateAntiForgeryToken] // el token lo envía el ajaxPrefilter de _Layout
        public async Task<IActionResult> CambiarSede(int nuevoSedeID, string returnUrl)
        {

            try
            {
                if (!HttpContext.User.Identity?.IsAuthenticated ?? true)
                {
                    return Json(new { success = false, message = "Identidad no válida o sesión expirada." });
                }

                // El ID de sede llega del navegador: antes se guardaba en la cookie sin
                // comprobar nada y cualquiera podía ponerse una sede de otra iglesia o 1000.
                // OnActionExecuting ya ha calculado AccesoSedesActual para esta petición.
                if (nuevoSedeID == Sedes.TodasLasSedes)
                {
                    if (!AccesoSedesActual.TodasLasSedes)
                        return Json(new { success = false, message = "No tienes acceso a todas las sedes." });
                }
                else
                {
                    // El filtro global hace que una sede de otra iglesia "no exista"
                    if (!_negocioSedes.ExisteSedeEnIglesiaActual(nuevoSedeID))
                        return Json(new { success = false, message = "Sede desconocida." });

                    // Sin acceso total, solo sus sedes: la principal o las de usuario_sedes
                    if (!AccesoSedesActual.Permite(nuevoSedeID))
                        return Json(new { success = false, message = "No tienes acceso a esa sede." });
                }

                string nuevoNombreSede = nuevoSedeID == Sedes.TodasLasSedes
                    ? "Todas las Sedes"
                    : ObtenerNombreSede(nuevoSedeID);

                await SesionClaims.ReemplazarAsync(HttpContext, new Dictionary<string, string>
                {
                    [SesionClaims.IdSede] = nuevoSedeID.ToString(),
                    [SesionClaims.NombreSede] = nuevoNombreSede
                });

                // Url.IsLocalUrl evita usar este endpoint para redirigir a otro dominio
                var destino = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
                return Json(new { success = true, redirectUrl = destino });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ErrorHelper.Mensaje(ex) });
            }
        }
    }
}
