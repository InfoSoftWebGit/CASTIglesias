using CapaEntidad;
using CapaNegocio;
using CASTIglesias.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace CASTIglesias.Filters
{
    /// <summary>
    /// Impide ejecutar la acción si el usuario no tiene al menos uno de los permisos indicados.
    /// </summary>
    /// <remarks>
    /// El menú lateral y las tarjetas del dashboard solo OCULTAN los enlaces: sin este filtro,
    /// cualquiera que escribiera la URL (o llamara a la acción por AJAX) podía usarla.
    /// Los nombres son las propiedades bool de <see cref="Permisos"/>: X (ver),
    /// XCrearEditar y XEliminar. Se pasan con nameof(Permisos.X) para que un renombrado
    /// rompa la compilación y no la seguridad.
    /// Los roles con acceso total y el administrador de plataforma no necesitan nada especial:
    /// ObtenerPermisosDeSesion ya les devuelve todos los permisos a true.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RequierePermisoAttribute : ActionFilterAttribute
    {
        // BaseController guarda aquí los permisos que ya ha leído para el menú, y así el
        // filtro no repite la consulta a la BBDD en la misma petición.
        public const string ClaveItems = "PermisosSesion";

        public const string MensajeSinPermiso = "No tienes permiso para realizar esta acción.";
        private const string MensajeSinAcceso = "No tienes permiso para acceder a esta sección.";

        private readonly string[] _permisos;

        public RequierePermisoAttribute(params string[] permisos)
        {
            if (permisos == null || permisos.Length == 0)
                throw new ArgumentException("Indica al menos un permiso.", nameof(permisos));

            // Se valida al crear el atributo: un nombre mal escrito dejaría la acción
            // cerrada para todos sin avisar.
            foreach (var nombre in permisos)
                ObtenerPropiedad(nombre);

            _permisos = permisos;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;

            // Sin sesión decide [Authorize]; si otro filtro ya ha respondido (por ejemplo,
            // BaseController mandando a cerrar sesión), no se hace nada más.
            if (http.User.Identity?.IsAuthenticated != true || context.Result != null)
                return;

            if (TieneAlguno(ObtenerPermisos(http), _permisos))
                return;

            context.Result = RespuestaSinPermiso(context.HttpContext, context.Controller);
        }

        /// <summary>
        /// Respuesta estándar cuando falta un permiso.
        /// </summary>
        /// <remarks>
        /// En AJAX se devuelve un 200 con resultado/success a false y el mensaje, en lugar
        /// de un 403: todas las pantallas ya muestran "mensaje" cuando el guardado o el
        /// borrado fallan, mientras que un 403 acabaría en errores genéricos distintos en
        /// cada vista. Las páginas normales vuelven al dashboard con un aviso.
        /// </remarks>
        public static IActionResult RespuestaSinPermiso(HttpContext http, object? controlador)
        {
            if (http.Request.Headers.XRequestedWith == "XMLHttpRequest" || !HttpMethods.IsGet(http.Request.Method))
                return JsonSinPermiso();

            if (controlador is Controller c)
                c.TempData["MensajeAcceso"] = MensajeSinAcceso;

            return new RedirectToActionResult("Index", "Home", null);
        }

        /// <summary>
        /// JSON de "sin permiso". Lleva los nombres de campo que usan las distintas
        /// pantallas (resultado/success, mensaje/message) para que todas lo muestren.
        /// </summary>
        public static JsonResult JsonSinPermiso() => new(new
        {
            resultado = false,
            success = false,
            error = true,
            sinPermiso = true,
            mensaje = MensajeSinPermiso,
            message = MensajeSinPermiso
        });

        /// <summary>Permisos de la sesión, reutilizando los que ya cargó BaseController.</summary>
        public static Permisos ObtenerPermisos(HttpContext http)
        {
            if (http.Items[ClaveItems] is Permisos permisos)
                return permisos;

            // Solo pasa si BaseController no pudo cargarlos (error de BBDD, sede inválida...)
            var negocioPermisos = http.RequestServices.GetRequiredService<CN_Permisos>();
            permisos = negocioPermisos.ObtenerPermisosDeSesion(SesionClaims.ObtenerIdUsuario(http.User));
            http.Items[ClaveItems] = permisos;
            return permisos;
        }

        /// <summary>true si al menos uno de los permisos indicados está activo.</summary>
        public static bool TieneAlguno(Permisos permisos, params string[] nombres) =>
            nombres.Any(n => (bool)ObtenerPropiedad(n).GetValue(permisos)!);

        private static PropertyInfo ObtenerPropiedad(string nombre)
        {
            var prop = typeof(Permisos).GetProperty(nombre);
            if (prop == null || prop.PropertyType != typeof(bool))
                throw new ArgumentException($"'{nombre}' no es un permiso de la clase Permisos.", nameof(nombre));
            return prop;
        }
    }
}
