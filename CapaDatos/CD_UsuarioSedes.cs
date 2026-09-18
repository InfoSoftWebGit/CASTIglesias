using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Acceso a la tabla usuario_sedes: en qué sedes puede trabajar cada usuario.
    /// </summary>
    public class CD_UsuarioSedes
    {
        private readonly AppDbContext _context;

        public CD_UsuarioSedes(AppDbContext context) => _context = context;

        /// <summary>
        /// Calcula las sedes a las que puede entrar un usuario.
        /// </summary>
        /// <remarks>
        /// Ignora el filtro por iglesia porque se usa también en el login, cuando aún no
        /// hay iglesia en la sesión; por eso filtra a mano por la iglesia de la fila del
        /// usuario. El ID llega siempre del login o del claim firmado, nunca del navegador.
        ///
        /// Reglas, en este orden:
        /// 1. Administrador de plataforma, AdminGlobal, PastorGeneral o sede 1000: todas.
        /// 2. Una fila con ID_sede NULL en usuario_sedes: todas.
        /// 3. Si no: su sede principal más las filas concretas de usuario_sedes. Así un
        ///    usuario antiguo sin filas sigue entrando en su sede como hasta ahora.
        /// </remarks>
        public AccesoSedes ObtenerAcceso(int idUsuario)
        {
            var usuario = _context.Usuarios.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.ID_usuario == idUsuario)
                .Select(u => new { u.ID_iglesia, u.ID_sede, u.Rol, u.es_admin_plataforma })
                .FirstOrDefault();

            if (usuario == null) return AccesoSedes.Ninguna;

            var rol = usuario.Rol?.Trim() ?? string.Empty;
            if (usuario.es_admin_plataforma ||
                rol.Equals("AdminGlobal", StringComparison.OrdinalIgnoreCase) ||
                rol.Equals("PastorGeneral", StringComparison.OrdinalIgnoreCase) ||
                usuario.ID_sede == Sedes.TodasLasSedes)
            {
                return AccesoSedes.Todas(usuario.ID_sede);
            }

            var filas = _context.UsuarioSedes.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(us => us.ID_usuario == idUsuario && us.ID_iglesia == usuario.ID_iglesia)
                .Select(us => us.ID_sede)
                .ToList();

            if (filas.Any(s => s == null))
                return AccesoSedes.Todas(usuario.ID_sede);

            var sedes = filas.Select(s => s!.Value)
                .Append(usuario.ID_sede)
                .Where(s => s != Sedes.TodasLasSedes)
                .ToHashSet();

            return new AccesoSedes { SedePrincipal = usuario.ID_sede, Sedes = sedes };
        }

        /// <summary>
        /// Sedes guardadas de un usuario de la iglesia activa, para el formulario de edición.
        /// </summary>
        /// <returns>Lista de IDs; null dentro de la lista significa "todas".</returns>
        public List<int?> ListarSedesDeUsuario(int idUsuario)
        {
            // Aquí sí se mantiene el filtro: quien edita solo ve usuarios de su iglesia
            return _context.UsuarioSedes
                .AsNoTracking()
                .Where(us => us.ID_usuario == idUsuario)
                .Select(us => us.ID_sede)
                .ToList();
        }

        /// <summary>
        /// Deja en usuario_sedes exactamente las sedes pedidas, dentro de lo que puede
        /// tocar quien edita.
        /// </summary>
        /// <param name="idUsuario">Usuario editado (de la iglesia activa).</param>
        /// <param name="todas">true = una única fila NULL (todas las sedes).</param>
        /// <param name="sedes">Sedes concretas deseadas, ya validadas por la capa de negocio.</param>
        /// <param name="ambitoEditor">Sedes que puede gestionar quien edita. Las filas
        /// fuera de su ámbito no se tocan, para que un pastor de sede no borre, sin
        /// saberlo, el acceso a una sede que él no ve.</param>
        /// <param name="creadoPor">Usuario que hace el cambio (auditoría).</param>
        public void Sincronizar(int idUsuario, bool todas, IReadOnlyCollection<int> sedes,
                                AccesoSedes ambitoEditor, int? creadoPor)
        {
            // El filtro global limita a la iglesia activa: un ID de otra iglesia no aparece
            if (!_context.Usuarios.Any(u => u.ID_usuario == idUsuario))
                throw new InvalidOperationException("Usuario no encontrado en esta iglesia.");

            var actuales = _context.UsuarioSedes
                .Where(us => us.ID_usuario == idUsuario)
                .ToList();

            bool EnAmbito(int? sede) =>
                sede == null ? ambitoEditor.TodasLasSedes : ambitoEditor.Permite(sede.Value);

            // Qué filas deben quedar (null = todas)
            var deseadas = todas
                ? new HashSet<int?> { null }
                : sedes.Select(s => (int?)s).ToHashSet();

            foreach (var fila in actuales)
            {
                if (EnAmbito(fila.ID_sede) && !deseadas.Contains(fila.ID_sede))
                    _context.UsuarioSedes.Remove(fila);
            }

            foreach (var sede in deseadas)
            {
                if (!EnAmbito(sede) || actuales.Any(f => f.ID_sede == sede)) continue;

                _context.UsuarioSedes.Add(new UsuarioSede
                {
                    ID_usuario = idUsuario,
                    ID_sede = sede,
                    nivel = UsuarioSede.NivelPorDefecto,
                    creado_por = creadoPor
                });
            }

            _context.SaveChanges();
        }

        /// <summary>
        /// Borra las filas de un usuario antes de eliminarlo y suelta las referencias en
        /// las que figura como creador, para que las FK no bloqueen el borrado.
        /// </summary>
        public void LimpiarAntesDeEliminarUsuario(int idUsuario)
        {
            _context.UsuarioSedes
                .Where(us => us.ID_usuario == idUsuario)
                .ExecuteDelete();

            // creado_por puede apuntar a filas de otras iglesias (p. ej. un administrador
            // de plataforma); solo se pone a NULL, no se borra nada ajeno.
            _context.UsuarioSedes.IgnoreQueryFilters()
                .Where(us => us.creado_por == idUsuario)
                .ExecuteUpdate(s => s.SetProperty(us => us.creado_por, (int?)null));
        }
    }
}
