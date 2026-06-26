using CapaNegocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Necesario para List<Claim>
using CapaEntidad; // Para el tipo Usuario

namespace CASTIglesias.Controllers
{
    public class AccesoController : Controller
    {
        private readonly CN_Usuarios _negocioUsuarios;
        private readonly CN_Permisos _negocioPermisos;

        public AccesoController(CN_Usuarios negocioUsuarios, CN_Permisos negocioPermisos)
        {
            _negocioUsuarios = negocioUsuarios;
            _negocioPermisos = negocioPermisos;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string correo, string clave)
        {
            // Buscar usuario
            var oUsuario = _negocioUsuarios.ListarTodosLosUsuariosParaLogin()
                .FirstOrDefault(u => u.correo_electronico == correo &&
                                     u.contrasenia == CN_Recursos.ConvertirSha256(clave));

            if (oUsuario == null)
            {
                ViewBag.Error = "Correo o contraseña incorrecta.";
                return View();
            }

            // Verificar si debe cambiar clave
            if (oUsuario.Es_primera_vez.GetValueOrDefault() || oUsuario.reestablecer.GetValueOrDefault())
            {
                TempData["ID_usuario"] = oUsuario.ID_usuario;
                return RedirectToAction("CambiarClave");
            }

            // Lógica de asignación de sede
            int sedeDelUsuarioLogueado = oUsuario.ID_sede;
            if (oUsuario.Rol == "AdminGlobal" || oUsuario.Rol == "PastorGeneral")
            {
                sedeDelUsuarioLogueado = 1000;
            }

            // 🔹 Obtener permisos desde la capa de negocio
            var permisosUsuario = _negocioPermisos.ObtenerPermisosPorUsuario(oUsuario.ID_usuario);

            // Crear Claims base
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, oUsuario.correo_electronico),
                new Claim(ClaimTypes.NameIdentifier, oUsuario.ID_usuario.ToString()),
                new Claim(ClaimTypes.Role, oUsuario.Rol ?? "Miembro"),
                new Claim("IDsede", sedeDelUsuarioLogueado.ToString()),
                new Claim("Multisede", (sedeDelUsuarioLogueado == 1000).ToString())
            };

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

            return RedirectToAction("Bienvenida", "Home");
        }

        public IActionResult ReestablecerClave() => View();
       
        [HttpPost]
        public IActionResult ReestablecerClave(string correo)
        {
            var oUsuario = _negocioUsuarios.ListarTodosLosUsuariosParaLogin().FirstOrDefault(u => u.correo_electronico == correo);

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

            var oUsuario = _negocioUsuarios.ListarTodosLosUsuariosParaLogin().FirstOrDefault(u => u.ID_usuario == id);

            if (oUsuario == null)
            {
                ViewBag.Error = "Usuario no encontrado.";
                return View();
            }

            if (!oUsuario.Es_primera_vez.GetValueOrDefault())
                return RedirectToAction("Index", "Home");

            if (oUsuario.contrasenia != CN_Recursos.ConvertirSha256(claveactual))
            {
                ViewBag.Error = "La contraseña actual no es correcta.";
                return View();
            }

            if (nuevaclave != confirmarclave)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View();
            }

            string nuevaClaveHash = CN_Recursos.ConvertirSha256(nuevaclave);

            bool respuesta = _negocioUsuarios.CambiarClave(id, nuevaClaveHash, oUsuario.ID_sede);

            if (!respuesta)
            {
                ViewBag.Error = "No se pudo actualizar la contraseña.";
                return View();
            }

            oUsuario.contrasenia = nuevaClaveHash;
            return RedirectToAction("Bienvenida", "Home");
        }

        public async Task<IActionResult> CerrarSesion()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
