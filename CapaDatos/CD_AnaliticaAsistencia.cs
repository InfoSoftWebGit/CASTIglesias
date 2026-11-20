using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Lee los registros de asistencia para el panel de evolución. Solo lee; el
    /// cálculo está en CN_AnaliticaAsistencia.
    /// </summary>
    public class CD_AnaliticaAsistencia
    {
        private readonly AppDbContext _context;

        public CD_AnaliticaAsistencia(AppDbContext context) => _context = context;

        /// <summary>Cultos de la sede (o de todas si es 1000) entre dos fechas, ambas incluidas.</summary>
        public List<CultoAsistenciaDTO> ListarCultos(int sedeID, DateTime desde, DateTime hasta)
        {
            var consulta = _context.Asistencia_Culto.AsNoTracking()
                .Where(a => a.fecha_asistencia_culto >= desde && a.fecha_asistencia_culto <= hasta);
            if (sedeID != Sedes.TodasLasSedes)
                consulta = consulta.Where(a => a.ID_sede == sedeID);

            return consulta
                .Select(a => new CultoAsistenciaDTO
                {
                    fecha = a.fecha_asistencia_culto,
                    turno = a.turno_culto,
                    adultos = a.adulto_asistencia_culto,
                    ninos = a.niños_asistencia_culto,
                    nuevos = a.nuevos_asistencia_culto,
                    invitados = a.invi_visit_asistencia_culto
                })
                .ToList();
        }
    }
}
