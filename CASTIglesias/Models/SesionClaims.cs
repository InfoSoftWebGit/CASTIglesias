using CapaDatos;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace CASTIglesias.Models
{
    /// <summary>
    /// Nombres de los claims de la sesión y utilidades para leerlos y reescribirlos.
    /// </summary>
    /// <remarks>
    /// La cookie de sesión va cifrada y firmada por ASP.NET, así que el navegador no puede
    /// alterar estos valores. Lo que sí puede hacer es PEDIR un cambio (de sede o de
    /// iglesia); por eso toda acción que reescribe claims valida antes contra la BBDD.
    /// </remarks>
    public static class SesionClaims
    {
        public const string IdIglesia = "IDiglesia";
        public const string NombreIglesia = "NombreIglesia";
        public const string IdSede = "IDsede";
        public const string NombreSede = "NombreSedeActual";
        // Valores siempre en minúsculas ("true"/"false"): HasClaim compara de forma exacta
        public const string Multisede = "Multisede";
        public const string AdminPlataforma = "AdminPlataforma";

        public static int ObtenerIdIglesia(ClaimsPrincipal usuario)
        {
            var valor = usuario.FindFirst(IdIglesia)?.Value;
            return int.TryParse(valor, out var id) ? id : 0;
        }

        public static int ObtenerIdUsuario(ClaimsPrincipal usuario)
        {
            var valor = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(valor, out var id) ? id : 0;
        }

        public static bool EsAdminPlataforma(ClaimsPrincipal usuario) => usuario.HasClaim(AdminPlataforma, "true");

        /// <summary>
        /// Quien puede ver "Todas las sedes" de su iglesia y elegir cualquiera de ellas.
        /// </summary>
        /// <remarks>
        /// PastorSede NO está aquí a propósito: es pastor de UNA sede.
        /// </remarks>
        public static bool TieneAccesoATodasLasSedes(ClaimsPrincipal usuario) =>
            usuario.IsInRole("AdminGlobal") ||
            usuario.IsInRole("PastorGeneral") ||
            usuario.HasClaim(Multisede, "true") ||
            EsAdminPlataforma(usuario);

        /// <summary>
        /// Sustituye los claims indicados y vuelve a emitir la cookie de sesión.
        /// </summary>
        public static async Task ReemplazarAsync(HttpContext http, IDictionary<string, string> nuevos)
        {
            if (http.User.Identity is not ClaimsIdentity identidad) return;

            var claims = identidad.Claims.Where(c => !nuevos.ContainsKey(c.Type)).ToList();
            claims.AddRange(nuevos.Select(n => new Claim(n.Key, n.Value)));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, identidad.AuthenticationType));
            await http.SignInAsync(principal);
        }
    }

    /// <summary>
    /// Implementación web de IContextoIglesia: la iglesia activa sale del claim de la cookie.
    /// </summary>
    public class ContextoIglesiaHttp : IContextoIglesia
    {
        private readonly IHttpContextAccessor _http;

        public ContextoIglesiaHttp(IHttpContextAccessor http) => _http = http;

        public int IdIglesia
        {
            get
            {
                var usuario = _http.HttpContext?.User;
                return usuario?.Identity?.IsAuthenticated == true
                    ? SesionClaims.ObtenerIdIglesia(usuario)
                    : 0;
            }
        }
    }
}
