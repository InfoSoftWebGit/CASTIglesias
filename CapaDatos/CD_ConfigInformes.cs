using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Configuración de informes: quién los ve, objetivos anuales y envío mensual.
    /// </summary>
    public class CD_ConfigInformes
    {
        private readonly AppDbContext _context;

        public CD_ConfigInformes(AppDbContext context) => _context = context;

        public static readonly string[] RolesConAccesoTotal = { "AdminGlobal", "PastorGeneral", "PastorSede" };

        public sealed class UsuarioInformesDTO
        {
            public int id_usuario { get; set; }
            public string nombre { get; set; } = string.Empty;
            public string? rol { get; set; }
            public string? sede { get; set; }
            /// <summary>Pastores y AdminGlobal: lo ven siempre, no se puede quitar.</summary>
            public bool siempre { get; set; }
            public bool ver_informes { get; set; }
        }

        #region Quién ve los informes

        public List<UsuarioInformesDTO> ListarUsuarios()
        {
            var usuarios = (from u in _context.Usuarios.AsNoTracking()
                            join s in _context.Sedes.AsNoTracking() on u.ID_sede equals s.ID into ss
                            from s in ss.DefaultIfEmpty()
                            join p in _context.Permisos.AsNoTracking() on u.ID_usuario equals p.ID_usuario into ps
                            from p in ps.DefaultIfEmpty()
                            select new
                            {
                                u.ID_usuario, u.nombre_usuario, u.apellido_usuario, u.Rol, u.activo,
                                sede = s != null ? s.nombre_sede : null,
                                ver = p != null && p.VerInformes
                            })
                           .ToList();

            return usuarios
                .Select(u => new UsuarioInformesDTO
                {
                    id_usuario = u.ID_usuario,
                    nombre = $"{u.nombre_usuario} {u.apellido_usuario}".Trim(),
                    rol = u.Rol,
                    sede = u.sede,
                    siempre = RolesConAccesoTotal.Contains(u.Rol?.Trim() ?? "", StringComparer.OrdinalIgnoreCase),
                    ver_informes = u.ver
                })
                .OrderByDescending(u => u.siempre).ThenBy(u => u.nombre)
                .ToList();
        }

        /// <summary>
        /// Da o quita el permiso de ver informes. Si el usuario aún no tiene fila de
        /// permisos se le crea una con solo este permiso.
        /// </summary>
        public bool GuardarVerInformes(int idUsuario, bool valor, out string mensaje)
        {
            mensaje = string.Empty;
            var usuario = _context.Usuarios.FirstOrDefault(u => u.ID_usuario == idUsuario);
            if (usuario == null) { mensaje = "Usuario no encontrado."; return false; }
            if (RolesConAccesoTotal.Contains(usuario.Rol?.Trim() ?? "", StringComparer.OrdinalIgnoreCase))
            {
                mensaje = "Los pastores ven siempre los informes.";
                return false;
            }

            var permisos = _context.Permisos.FirstOrDefault(p => p.ID_usuario == idUsuario);
            if (permisos == null)
                _context.Permisos.Add(new Permisos { ID_usuario = idUsuario, ID_sede = usuario.ID_sede, VerInformes = valor });
            else
                permisos.VerInformes = valor;

            _context.SaveChanges();
            return true;
        }

        #endregion

        #region Objetivos anuales

        public Dictionary<string, decimal> ListarObjetivos(int anio, int sedeID) =>
            _context.ObjetivosAnuales.AsNoTracking()
                .Where(o => o.anio == anio && o.ID_sede == sedeID)
                .ToDictionary(o => o.indicador, o => o.valor);

        /// <summary>Guarda los objetivos de un año; un valor null o ≤ 0 borra el objetivo.</summary>
        public void GuardarObjetivos(int anio, int sedeID, Dictionary<string, decimal?> valores)
        {
            var actuales = _context.ObjetivosAnuales.Where(o => o.anio == anio && o.ID_sede == sedeID).ToList();
            foreach (var indicador in ObjetivoAnual.Indicadores)
            {
                valores.TryGetValue(indicador, out var valor);
                var fila = actuales.FirstOrDefault(o => o.indicador == indicador);
                if (valor is null or <= 0)
                {
                    if (fila != null) _context.ObjetivosAnuales.Remove(fila);
                }
                else if (fila == null)
                    _context.ObjetivosAnuales.Add(new ObjetivoAnual { anio = anio, ID_sede = sedeID, indicador = indicador, valor = valor.Value });
                else
                    fila.valor = valor.Value;
            }
            _context.SaveChanges();
        }

        #endregion

        #region Informe mensual

        public ConfigInformes ObtenerConfig() =>
            _context.ConfigInformes.AsNoTracking().FirstOrDefault() ?? new ConfigInformes();

        public void GuardarEnvioMensual(bool activo)
        {
            var cfg = _context.ConfigInformes.FirstOrDefault();
            // La iglesia se pone ya: es la clave de la fila y EF no deja cambiarla después
            if (cfg == null) _context.ConfigInformes.Add(new ConfigInformes { ID_iglesia = _context.IdIglesiaActual, envio_mensual = activo });
            else cfg.envio_mensual = activo;
            _context.SaveChanges();
        }

        /// <summary>Iglesias con el envío activo a las que aún no se ha mandado el mes indicado.</summary>
        public List<int> IglesiasPendientesDeEnvio(string mes) =>
            _context.ConfigInformes.IgnoreQueryFilters().AsNoTracking()
                .Where(c => c.envio_mensual && (c.ultimo_envio == null || c.ultimo_envio != mes))
                .Select(c => c.ID_iglesia)
                .ToList();

        public void MarcarEnviado(string mes)
        {
            var cfg = _context.ConfigInformes.FirstOrDefault();
            if (cfg == null) return;
            cfg.ultimo_envio = mes;
            _context.SaveChanges();
        }

        /// <summary>
        /// Pastores generales y AdminGlobal activos de la iglesia de la sesión, con
        /// correo. Son los que reciben el informe de toda la iglesia.
        /// </summary>
        public List<string> CorreosPastoresGenerales() =>
            _context.Usuarios.AsNoTracking()
                .Where(u => (u.Rol == "PastorGeneral" || u.Rol == "AdminGlobal") && u.activo == true
                            && u.correo_electronico != null && u.correo_electronico != "")
                .Select(u => u.correo_electronico!)
                .Distinct()
                .ToList();

        public string? CorreoDeUsuario(int idUsuario) =>
            _context.Usuarios.AsNoTracking().Where(u => u.ID_usuario == idUsuario).Select(u => u.correo_electronico).FirstOrDefault();

        public string? NombreIglesia(int idIglesia) =>
            _context.Iglesias.AsNoTracking().Where(i => i.ID == idIglesia).Select(i => i.nombre_iglesia).FirstOrDefault();

        #endregion
    }
}
