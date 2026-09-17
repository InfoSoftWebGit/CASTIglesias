using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Datos del administrador de plataforma: catálogo de iglesias y registro de accesos.
    /// </summary>
    /// <remarks>
    /// Todas las comprobaciones de "¿es administrador de plataforma?" se hacen contra la
    /// BBDD y no solo contra el claim, para que retirar el permiso por SQL tenga efecto
    /// inmediato aunque la persona tenga la sesión abierta.
    /// </remarks>
    public class CD_Plataforma
    {
        private readonly AppDbContext _context;

        public CD_Plataforma(AppDbContext context) => _context = context;

        public bool EsAdminPlataforma(int idUsuario)
        {
            // Sin filtro: la fila del administrador está en la iglesia interna de Congrega,
            // no en la iglesia en la que esté trabajando en ese momento.
            return _context.Usuarios.IgnoreQueryFilters()
                .Any(u => u.ID_usuario == idUsuario && u.es_admin_plataforma);
        }

        /// <summary>
        /// Lista las iglesias con su número de sedes reales, con búsqueda por nombre, CIF o correo.
        /// </summary>
        /// <remarks>
        /// Se limita el número de filas porque con miles de iglesias no tiene sentido
        /// pintarlas todas: se busca y se entra.
        /// </remarks>
        public List<Iglesia> ListarIglesias(string? buscar, int maximo = 200)
        {
            var consulta = _context.Iglesias.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();
                consulta = consulta.Where(i =>
                    (i.nombre_iglesia != null && i.nombre_iglesia.Contains(buscar)) ||
                    (i.cif != null && i.cif.Contains(buscar)) ||
                    (i.email_contacto != null && i.email_contacto.Contains(buscar)));
            }

            // Primero las recién pagadas pendientes de configurar: son las que requieren acción
            var iglesias = consulta
                .OrderBy(i => i.estado == "pendiente_configuracion" ? 0 : 1)
                .ThenBy(i => i.nombre_iglesia)
                .Take(maximo)
                .ToList();

            var ids = iglesias.Select(i => i.ID).ToList();
            var sedesPorIglesia = _context.Sedes.IgnoreQueryFilters()
                .Where(s => ids.Contains(s.ID_iglesia) && s.ID != Sedes.TodasLasSedes)
                .GroupBy(s => s.ID_iglesia)
                .Select(g => new { IdIglesia = g.Key, Total = g.Count() })
                .ToDictionary(x => x.IdIglesia, x => x.Total);

            foreach (var iglesia in iglesias)
                iglesia.num_sedes = sedesPorIglesia.TryGetValue(iglesia.ID, out var total) ? total : 0;

            return iglesias;
        }

        public Iglesia? ObtenerIglesia(int idIglesia)
        {
            return _context.Iglesias.AsNoTracking().FirstOrDefault(i => i.ID == idIglesia);
        }

        /// <summary>
        /// Cierra los accesos que el administrador tenga abiertos y abre uno nuevo.
        /// </summary>
        /// <remarks>
        /// Van en una sola transacción para que nunca queden dos accesos abiertos a la vez
        /// ni un acceso nuevo sin haber cerrado el anterior.
        /// </remarks>
        public void RegistrarAcceso(int idUsuario, int idIglesia, string motivo, string? ipHash)
        {
            using var transaccion = _context.Database.BeginTransaction();

            CerrarAccesosAbiertos(idUsuario);

            _context.AccesosPlataforma.Add(new AccesoPlataforma
            {
                ID_usuario = idUsuario,
                ID_iglesia = idIglesia,
                motivo = motivo,
                inicio = DateTime.Now,
                ip_hash = ipHash
            });
            _context.SaveChanges();

            transaccion.Commit();
        }

        public void CerrarAccesosAbiertos(int idUsuario)
        {
            var abiertos = _context.AccesosPlataforma
                .Where(a => a.ID_usuario == idUsuario && a.fin == null)
                .ToList();

            foreach (var acceso in abiertos)
                acceso.fin = DateTime.Now;

            _context.SaveChanges();
        }
    }
}
