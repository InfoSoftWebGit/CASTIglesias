using CapaNegocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Necesario para List<Claim>
using CapaEntidad; // Para el tipo Usuario
using CASTIglesias.Models; // Para IdiomasSoportados

namespace CASTIglesias.Controllers
{
    public class AccesoController : Controller
    {
        private readonly CN_Usuarios _negocioUsuarios;
        private readonly CN_Permisos _negocioPermisos;
        private readonly CN_Plataforma _negocioPlataforma;
        private readonly CN_UsuarioSedes _negocioUsuarioSedes;

        public AccesoController(CN_Usuarios negocioUsuarios, CN_Permisos negocioPermisos,
                                CN_Plataforma negocioPlataforma, CN_UsuarioSedes negocioUsuarioSedes)
        {
            _negocioUsuarios = negocioUsuarios;
            _negocioPermisos = negocioPermisos;
            _negocioPlataforma = negocioPlataforma;
            _negocioUsuarioSedes = negocioUsuarioSedes;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string correo, string clave)
        {
            // La iglesia y la sede del usuario salen de SU fila en la BBDD, nunca de lo
            // que envíe el formulario: el correo identifica a la persona y su fila dice
            // a qué iglesia pertenece.
            var oUsuario = _negocioUsuarios.ValidarCredenciales(correo, clave);

            if (oUsuario == null)
            {
                ViewBag.Error = "Correo o contraseña incorrecta.";
                return View();
            }

            // "O tiene iglesia o nada": una fila sin iglesia válida no entra.
            var iglesia = _negocioPlataforma.ObtenerIglesia(oUsuario.ID_iglesia);
            if (iglesia == null)
            {
                ViewBag.Error = "El usuario no está asociado a ninguna iglesia. Contacta con soporte.";
                return View();
            }

            // Una iglesia pendiente de configurar, suspendida o cerrada no puede entrar.
            // El administrador de plataforma sí, porque su iglesia es la interna de Congrega.
            if (!oUsuario.es_admin_plataforma && !Iglesia.EstadosConAcceso.Contains(iglesia.estado))
            {
                ViewBag.Error = iglesia.estado == "pendiente_configuracion"
                    ? "Tu iglesia todavía se está configurando. Nos pondremos en contacto contigo."
                    : "El acceso de tu iglesia está suspendido. Contacta con soporte@congrega.es.";
                return View();
            }

            // Verificar si debe cambiar clave
            if (oUsuario.Es_primera_vez.GetValueOrDefault() || oUsuario.reestablecer.GetValueOrDefault())
            {
                TempData["ID_usuario"] = oUsuario.ID_usuario;
                return RedirectToAction("CambiarClave");
            }

            // Sedes permitidas (rol, sede 1000 o filas de usuario_sedes). Quien tiene todas
            // entra en "Todas las Sedes"; el resto entra en su sede principal y, si tiene
            // más, las elige después en el selector.
            var accesoSedes = _negocioUsuarioSedes.ObtenerAcceso(oUsuario.ID_usuario);
            int sedeDelUsuarioLogueado = accesoSedes.TodasLasSedes ? Sedes.TodasLasSedes : oUsuario.ID_sede;

            // 🔹 Obtener permisos desde la capa de negocio
            var permisosUsuario = _negocioPermisos.ObtenerPermisosDeSesion(oUsuario.ID_usuario);

            // Crear Claims base
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, oUsuario.correo_electronico ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, oUsuario.ID_usuario.ToString()),
                new Claim(ClaimTypes.Role, oUsuario.Rol ?? "Miembro"),
                // Iglesia activa: la lee AppDbContext para filtrar TODAS las consultas
                new Claim(SesionClaims.IdIglesia, oUsuario.ID_iglesia.ToString()),
                new Claim(SesionClaims.NombreIglesia, iglesia.nombre_iglesia ?? string.Empty),
                new Claim(SesionClaims.IdSede, sedeDelUsuarioLogueado.ToString()),
                // En minúsculas: el layout compara con HasClaim("Multisede", "true")
                new Claim(SesionClaims.Multisede, accesoSedes.TodasLasSedes ? "true" : "false")
            };

            if (oUsuario.es_admin_plataforma)
            {
                claims.Add(new Claim(SesionClaims.AdminPlataforma, "true"));
            }

            // 🔹 Agregar permisos como Claims personalizados
            claims.Add(new Claim("Permiso_Usuarios", permisosUsuario.Usuarios.ToString()));
            claims.Add(new Claim("Permiso_Miembros", permisosUsuario.Miembros.ToString()));
            claims.Add(new Claim("Permiso_Familias", permisosUsuario.Familias.ToString()));
            claims.Add(new Claim("Permiso_Grupos", permisosUsuario.Grupos.ToString()));
            claims.Add(new Claim("Permiso_Zonas", permisosUsuario.Zonas.ToString()));
            claims.Add(new Claim("Permiso_Diezmos", permisosUsuario.Diezmos.ToString()));
            claims.Add(new Claim("Permiso_Conceptos", permisosUsuario.Conceptos.ToString()));

            // Crear identidad y firmar
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // El administrador de plataforma empieza eligiendo en qué iglesia trabajar
            if (oUsuario.es_admin_plataforma)
            {
                return RedirectToAction("Index", "Plataforma");
            }

            return RedirectToAction("Bienvenida", "Home");
        }

        public IActionResult ReestablecerClave() => View();

        [HttpPost]
        public IActionResult ReestablecerClave(string correo)
        {
            var oUsuario = string.IsNullOrWhiteSpace(correo) ? null : _negocioUsuarios.ObtenerUsuarioPorCorreo(correo.Trim());

            if (oUsuario == null)
            {
                ViewBag.Error = "No se encontró un usuario relacionado a ese correo";
                return View();
            }

            bool respuesta = _negocioUsuarios.ReestablecerClave(oUsuario.ID_usuario, correo, oUsuario.ID_sede);

            if (respuesta)
                return RedirectToAction("Login");

            ViewBag.Error = "No se pudo reestablecer la contraseña.";
            return View();
        }


        public IActionResult CambiarClave()
        {
            var idUsuario = TempData["ID_usuario"];
            if (idUsuario == null)
            {
                ViewBag.Error = "No se pudo identificar al usuario.";
                return RedirectToAction("Login");
            }
            ViewBag.ID_usuario = idUsuario.ToString();
            TempData["ID_usuario"] = idUsuario; // Mantener por POST
            return View();
        }
        [HttpPost]
        public IActionResult CambiarClave(string idusuario, string claveactual, string nuevaclave, string confirmarclave)
        {
            ViewBag.ID_usuario = idusuario;
            int id;

            if (!int.TryParse(idusuario, out id))
            {
                ViewBag.Error = "ID de usuario inválido.";
                return View();
            }

            var oUsuario = _negocioUsuarios.ObtenerUsuarioSinFiltro(id);

            if (oUsuario == null)
            {
                ViewBag.Error = "Usuario no encontrado.";
                return View();
            }

            if (!oUsuario.Es_primera_vez.GetValueOrDefault())
                return RedirectToAction("Index", "Home");

            // Validaciones y hash nuevo (PBKDF2) en la capa de negocio
            var error = _negocioUsuarios.CambiarClavePropia(id, claveactual, nuevaclave, confirmarclave);
            if (error != null)
            {
                ViewBag.Error = error;
                return View();
            }

            // Tras cambiarla vuelve al login para entrar con la clave nueva: esta acción no
            // abre sesión, y Bienvenida la exige.
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> CerrarSesion()
        {
            // Si era el administrador de plataforma, se cierra su acceso abierto a la iglesia
            // para que el registro de auditoría refleje cuándo terminó.
            if (SesionClaims.EsAdminPlataforma(User))
            {
                _negocioPlataforma.SalirDeIglesia(SesionClaims.ObtenerIdUsuario(User));
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Cambia el idioma de la interfaz y devuelve al usuario a la página desde
        /// la que lo solicitó. La preferencia se guarda en una cookie que lee el
        /// CookieRequestCultureProvider configurado en Program.cs.
        /// </summary>
        /// <param name="lang">Código del idioma solicitado ("es" o "en").</param>
        /// <param name="returnUrl">Ruta local a la que volver tras el cambio.</param>
        /// <remarks>
        /// Es un GET a propósito, para poder engancharlo como enlace normal en el
        /// desplegable del menú (igual que CerrarSesion). No usa POST porque no
        /// modifica datos: solo ajusta una preferencia de visualización, de modo
        /// que un CSRF aquí no tendría más efecto que ver la web en otro idioma.
        ///
        /// La preferencia es por navegador, no por usuario. Cuando se añada la
        /// columna de idioma en la tabla de usuarios, bastará con leerla al hacer
        /// login y escribir esta misma cookie; el resto no cambia.
        /// </remarks>
        public IActionResult CambiarIdioma(string lang, string? returnUrl = null)
        {
            // Lista blanca: el valor llega desde la URL y acaba construyendo un
            // CultureInfo, así que no se acepta nada fuera de los idiomas declarados.
            if (!IdiomasSoportados.EsValido(lang))
            {
                lang = IdiomasSoportados.PorDefecto;
            }

            // Se escriben las dos culturas por separado, igual que en Program.cs:
            // la de formato queda invariante para no alterar el enlace de decimales,
            // y solo cambia la de interfaz.
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(
                        culture:   CultureInfo.InvariantCulture,
                        uiCulture: new CultureInfo(lang))),
                new CookieOptions
                {
                    Expires  = DateTimeOffset.UtcNow.AddYears(1),

                    // Cookie funcional: se marca como esencial para que no la
                    // elimine una futura política de consentimiento de cookies.
                    IsEssential = true,

                    // Solo la lee el servidor; ningún script necesita acceder a ella.
                    HttpOnly = true,

                    SameSite = SameSiteMode.Lax
                });

            // Url.IsLocalUrl evita el open redirect: returnUrl viene de la query
            // string y sin esta comprobación se podría enlazar el cambio de idioma
            // a un salto hacia un dominio externo.
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
