using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Lectura del registro de auditoría financiera (tabla <c>audit_events</c>).
    /// </summary>
    /// <remarks>
    /// Aquí SOLO se lee. Quien escribe es CD_Asientos, dentro de la misma transacción
    /// que el hecho que registra, y eso no se toca desde esta clase: un registro de
    /// auditoría que se pueda modificar o borrar desde la aplicación no sirve para
    /// nada, porque lo primero que haría alguien que quisiera tapar algo es
    /// precisamente eso.
    ///
    /// Por eso esta clase no tiene Guardar, Modificar ni Eliminar, y no es un olvido.
    ///
    /// Las dos columnas cifradas (before_json_encrypted y after_json_encrypted) no se
    /// leen: hoy el motor no las escribe, y mientras estén vacías enseñar una columna
    /// permanentemente en blanco solo haría dudar de si falta algo.
    /// </remarks>
    public class CD_Auditoria
    {
        private readonly AppDbContext _context;

        public CD_Auditoria(AppDbContext context) => _context = context;

        /// <summary>Un hecho registrado, ya resuelto para enseñarlo.</summary>
        public class EventoDTO
        {
            public long id { get; set; }
            public DateTime occurred_at { get; set; }
            public string? event_type { get; set; }
            public string? entity_type { get; set; }
            public int? entity_id { get; set; }
            public string? action { get; set; }
            public string? usuario { get; set; }
            public string? sede { get; set; }
            public string? metadata_json { get; set; }
        }

        /// <summary>
        /// Eventos que cumplen los filtros, del más reciente al más antiguo.
        /// </summary>
        /// <param name="tope">
        /// Techo de filas. La tabla de auditoría solo crece, así que una pantalla sin
        /// tope acabaría tardando cada vez más hasta dejar de abrirse.
        /// </param>
        public List<EventoDTO> Listar(DateTime? desde, DateTime? hasta, string? tipoEvento,
                                      string? tipoEntidad, int? idUsuario, int? sedeID,
                                      int tope = 500)
        {
            var eventos = _context.AuditEvents.AsNoTracking().AsQueryable();

            if (desde.HasValue) eventos = eventos.Where(e => e.occurred_at >= desde.Value);
            // El "hasta" se lleva al final del día: si no, filtrar por hoy no
            // devolvería nada de hoy, porque la hora guardada es mayor que las 00:00.
            if (hasta.HasValue) eventos = eventos.Where(e => e.occurred_at < hasta.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(tipoEvento)) eventos = eventos.Where(e => e.event_type == tipoEvento);
            if (!string.IsNullOrWhiteSpace(tipoEntidad)) eventos = eventos.Where(e => e.entity_type == tipoEntidad);
            if (idUsuario.HasValue && idUsuario.Value > 0) eventos = eventos.Where(e => e.actor_user_id == idUsuario.Value);
            if (sedeID.HasValue && sedeID.Value > 0 && sedeID.Value != 1000)
                eventos = eventos.Where(e => e.site_id == sedeID.Value);

            var filas = eventos
                .OrderByDescending(e => e.occurred_at).ThenByDescending(e => e.id)
                .Take(tope)
                .ToList();

            // Los nombres se resuelven en memoria sobre las filas ya traídas: son como
            // mucho 'tope' y así no hay un JOIN por cada consulta de la pantalla.
            var usuarios = _context.Usuarios.AsNoTracking()
                .ToDictionary(u => u.ID_usuario, u => u.nombre_usuario ?? "");
            var sedes = _context.Sedes.AsNoTracking()
                .ToDictionary(s => s.ID, s => s.nombre_sede ?? "");

            return filas.Select(e => new EventoDTO
            {
                id = e.id,
                occurred_at = e.occurred_at,
                event_type = e.event_type,
                entity_type = e.entity_type,
                entity_id = e.entity_id,
                action = e.action,
                // Un usuario borrado deja su id huérfano. Se enseña el id en lugar de
                // dejar la celda vacía: el rastro sigue valiendo aunque falte el nombre.
                usuario = e.actor_user_id.HasValue
                    ? (usuarios.ContainsKey(e.actor_user_id.Value)
                        ? usuarios[e.actor_user_id.Value]
                        : "#" + e.actor_user_id.Value)
                    : null,
                sede = e.site_id.HasValue && sedes.ContainsKey(e.site_id.Value)
                    ? sedes[e.site_id.Value] : null,
                metadata_json = e.metadata_json
            }).ToList();
        }

        /// <summary>Tipos de evento que existen de verdad, para el desplegable.</summary>
        /// <remarks>
        /// Se sacan de los datos y no de una lista fija: así el desplegable nunca
        /// ofrece un filtro que no devuelve nada, ni se queda corto cuando el motor
        /// empiece a registrar hechos nuevos.
        /// </remarks>
        public List<string> TiposDeEvento()
        {
            return _context.AuditEvents.AsNoTracking()
                .Where(e => e.event_type != null)
                .Select(e => e.event_type!)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        /// <summary>Tipos de entidad que existen de verdad, para el desplegable.</summary>
        public List<string> TiposDeEntidad()
        {
            return _context.AuditEvents.AsNoTracking()
                .Where(e => e.entity_type != null)
                .Select(e => e.entity_type!)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        /// <summary>Cuántos eventos hay en total, para avisar de que la vista va topada.</summary>
        public int Contar(DateTime? desde, DateTime? hasta)
        {
            var eventos = _context.AuditEvents.AsNoTracking().AsQueryable();
            if (desde.HasValue) eventos = eventos.Where(e => e.occurred_at >= desde.Value);
            if (hasta.HasValue) eventos = eventos.Where(e => e.occurred_at < hasta.Value.Date.AddDays(1));
            return eventos.Count();
        }
    }
}
