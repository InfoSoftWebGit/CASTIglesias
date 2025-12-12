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

        // ✅ Se ejecuta antes de cada acción para preparar ViewBag
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (HttpContext.User.Identity.IsAuthenticated)
            {
                try
                {
                    // 1. Obtener la sede actual.
                    int sedeIDActual = ObtenerIdSedeUsuario();
                    string nombreSedeActual;
                    List<Sedes> listaSedes;

                    var esRolAltoGlobal = HttpContext.User.IsInRole("AdminGlobal") || HttpContext.User.IsInRole("PastorGeneral") || HttpContext.User.IsInRole("PastorSede");
                    var esMultiSede = esRolAltoGlobal || HttpContext.User.HasClaim("Multisede", "true");

                    var nombreClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "NombreSedeActual");

                    if (nombreClaim != null && sedeIDActual != 1000)
                    {
                        nombreSedeActual = nombreClaim.Value;
                    }
                    else
                    {
                        nombreSedeActual = ObtenerNombreSede(sedeIDActual);
                    }

                    if (esMultiSede)
                    {
                        listaSedes = _negocioSedes.ListarSedes() ?? new List<Sedes>();
                    }
                    else
                    {
                        listaSedes = new List<Sedes>();
                    }



                    Permisos permisosUsuario = null;

                    var idUsuarioClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    if (idUsuarioClaim != null && int.TryParse(idUsuarioClaim.Value, out int idUsuario))
                    {
                        permisosUsuario = _negocioPermisos.ObtenerPermisosPorUsuario(idUsuario);
                    }
                    ViewBag.SedeIDActual = sedeIDActual;
                    ViewBag.NombreSedeActual = nombreSedeActual;
                    ViewBag.ListaSedes = listaSedes;
                    ViewBag.PermisosMiembro = permisosUsuario ?? new Permisos();
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
        public async Task<IActionResult> CambiarSede(int nuevoSedeID, string returnUrl)
        {

            try
            {
                if (nuevoSedeID < 0)
                {
                    return Json(new { success = false, message = "ID de sede inválido." });
                }

                string nuevoNombreSede = nuevoSedeID == 1000
                    ? "Todas las Sedes"
                    : ObtenerNombreSede(nuevoSedeID);

                if (nuevoNombreSede == "Sede Desconocida")
                {
                    return Json(new { success = false, message = "Sede desconocida." });
                }

                var identity = HttpContext.User.Identity as ClaimsIdentity;
                if (identity == null)
                {
                    return Json(new { success = false, message = "Identidad no válida o sesión expirada." });
                }

                var claims = identity.Claims.ToList();

                // --- INICIO DE CAMBIO IMPORTANTE: Actualizar Claims ---
                claims.RemoveAll(c => c.Type == "IDsede" || c.Type == "NombreSedeActual");
                claims.Add(new Claim("IDsede", nuevoSedeID.ToString())); // Usamos IDsede como Claim Name (como está configurado)
                claims.Add(new Claim("NombreSedeActual", nuevoNombreSede));
                // --- FIN DE CAMBIO IMPORTANTE ---

                var newIdentity = new ClaimsIdentity(claims, identity.AuthenticationType);
                var newPrincipal = new ClaimsPrincipal(newIdentity);

                // Volver a firmar (esto debería actualizar la cookie de sesión)
                await HttpContext.SignInAsync(newPrincipal);

                // Devolver la URL de retorno para que el cliente recargue la página
                return Json(new { success = true, redirectUrl = returnUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
