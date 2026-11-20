using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Lee el historial de estados para los informes evolutivos de congregantes.
    /// </summary>
    /// <remarks>
    /// Solo lee: todo el cálculo (totales por periodo, embudo, tiempos) se hace en
    /// CN_AnaliticaCongregantes, en memoria. Una iglesia tiene como mucho unos miles
    /// de eventos, y calcular "en qué estado estaba cada persona el día X" en SQL
    /// sería mucho más difícil de leer y de mantener que en C#.
    /// </remarks>
    public class CD_AnaliticaCongregantes
    {
        private readonly AppDbContext _context;

        public CD_AnaliticaCongregantes(AppDbContext context) => _context = context;

        /// <summary>
        /// Todos los eventos de la sede (o de todas si es 1000), incluidos los que se
        /// deducen para quien no tiene historial.
        /// </summary>
        /// <remarks>
        /// Un congregante puede no tener historial si se creó entre el despliegue de
        /// la aplicación y la ejecución del script 0002, o si falló el segundo paso del
        /// guardado. Para que no desaparezca de los totales se le deduce un alta con su
        /// estado actual (y su baja, si la tiene), igual que hace la carga inicial.
        /// </remarks>
        public List<EventoEstadoDTO> ListarEventos(int sedeID)
        {
            var historial = _context.HistorialEstadosMiembro.AsNoTracking();
            var miembros = _context.Miembros.AsNoTracking();
            if (sedeID != Sedes.TodasLasSedes)
            {
                historial = historial.Where(h => h.ID_sede == sedeID);
                miembros = miembros.Where(m => m.id_sede == sedeID);
            }

            var eventos = historial
                .Select(h => new EventoEstadoDTO
                {
                    id_miembro = h.id_miembro,
                    tipo_evento = h.tipo_evento,
                    estado_anterior = h.estado_anterior,
                    estado_nuevo = h.estado_nuevo,
                    fecha = h.fecha_cambio,
                    aproximado = h.origen == HistorialEstadoMiembro.OrigenCargaInicial
                })
                .ToList();

            // El historial de un congregante puede estar en otra sede si se le cambió
            // de sede: se mira en toda la iglesia para no deducirle un alta duplicada.
            var conHistorial = _context.HistorialEstadosMiembro.AsNoTracking()
                .Select(h => h.id_miembro).Distinct().ToHashSet();

            var sinHistorial = miembros
                .Select(m => new
                {
                    m.id_miembro,
                    m.estado,
                    m.miembro_activo,
                    llegada = m.fecha_llegada_iglesia ?? m.fecha_bautismo,
                    baja = m.fecha_baja ?? m.fecha_fallecido
                })
                .ToList()
                .Where(m => !conHistorial.Contains(m.id_miembro));

            foreach (var m in sinHistorial)
            {
                eventos.Add(new EventoEstadoDTO
                {
                    id_miembro = m.id_miembro,
                    tipo_evento = HistorialEstadoMiembro.TipoAlta,
                    estado_nuevo = m.estado,
                    fecha = m.llegada,
                    aproximado = true
                });
                if (m.miembro_activo == "No")
                    eventos.Add(new EventoEstadoDTO
                    {
                        id_miembro = m.id_miembro,
                        tipo_evento = HistorialEstadoMiembro.TipoBaja,
                        estado_anterior = m.estado,
                        estado_nuevo = m.estado,
                        fecha = m.baja,
                        aproximado = true
                    });
            }

            return eventos;
        }
    }
}
