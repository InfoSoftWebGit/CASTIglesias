using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Sala
    {
        private readonly CD_Sala _cd;
        public CN_Sala(CD_Sala cd) => _cd = cd;

        public List<Sala> Listar(int sedeId) => _cd.Listar(sedeId);

        public bool Guardar(Sala sala, out string mensaje)
        {
            if (string.IsNullOrWhiteSpace(sala.nombre_sala))
            {
                mensaje = "El nombre de la sala es obligatorio.";
                return false;
            }
            sala.nombre_sala = sala.nombre_sala.Trim();
            if (sala.reservado != "Si") { sala.id_zona_reserva = null; sala.fecha_reserva = null; }
            return _cd.Guardar(sala, out mensaje);
        }

        public bool Eliminar(int idSala, int sedeId, out string mensaje) =>
            _cd.Eliminar(idSala, sedeId, out mensaje);
    }
}
