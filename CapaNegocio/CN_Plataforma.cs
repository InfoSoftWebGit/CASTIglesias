using CapaDatos;
using CapaEntidad;
using System.Security.Cryptography;
using System.Text;

namespace CapaNegocio
{
    /// <summary>
    /// Reglas del acceso del administrador de plataforma a las iglesias cliente.
    /// </summary>
    public class CN_Plataforma
    {
        private readonly CD_Plataforma _cdPlataforma;

        public CN_Plataforma(CD_Plataforma cdPlataforma) => _cdPlataforma = cdPlataforma;

        public bool EsAdminPlataforma(int idUsuario) => _cdPlataforma.EsAdminPlataforma(idUsuario);

        public List<Iglesia> ListarIglesias(string? buscar) => _cdPlataforma.ListarIglesias(buscar);

        /// <summary>
        /// Valida y registra la entrada en una iglesia. Devuelve la iglesia si todo es correcto.
        /// </summary>
        /// <remarks>
        /// El motivo es obligatorio porque es lo que justifica el acceso a datos de
        /// categoría especial (RGPD). La iglesia se busca en BBDD: el ID llega del navegador.
        /// </remarks>
        public Iglesia? EntrarEnIglesia(int idUsuario, int idIglesia, string? motivo, string? ip, out string mensaje)
        {
            mensaje = string.Empty;

            if (!_cdPlataforma.EsAdminPlataforma(idUsuario))
            {
                mensaje = "No tienes permiso de administrador de plataforma.";
                return null;
            }

            if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5)
            {
                mensaje = "Indica el motivo del acceso.";
                return null;
            }

            var iglesia = _cdPlataforma.ObtenerIglesia(idIglesia);
            if (iglesia == null)
            {
                mensaje = "La iglesia no existe.";
                return null;
            }

            // La columna admite 300 caracteres; se recorta en vez de fallar
            var motivoLimpio = motivo.Trim();
            if (motivoLimpio.Length > 300) motivoLimpio = motivoLimpio[..300];

            _cdPlataforma.RegistrarAcceso(idUsuario, idIglesia, motivoLimpio, CalcularHash(ip));
            return iglesia;
        }

        public void SalirDeIglesia(int idUsuario) => _cdPlataforma.CerrarAccesosAbiertos(idUsuario);

        public Iglesia? ObtenerIglesia(int idIglesia) => _cdPlataforma.ObtenerIglesia(idIglesia);

        private static string? CalcularHash(string? valor)
        {
            if (string.IsNullOrEmpty(valor)) return null;
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(valor));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
