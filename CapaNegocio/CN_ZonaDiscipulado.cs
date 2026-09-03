using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Lógica de las zonas de discipulado por defecto (Hombres, Mujeres, Niños).
    /// </summary>
    public class CN_ZonaDiscipulado
    {
        private readonly CD_ZonaDiscipulado _capaDatos;

        public CN_ZonaDiscipulado(CD_ZonaDiscipulado capaDatos)
        {
            _capaDatos = capaDatos;
        }

        public bool EsTipoValido(string tipo) => CD_ZonaDiscipulado.EsTipoValido(tipo);

        /// <summary>
        /// Garantiza que la sede tenga las seis zonas por defecto de la aplicación.
        /// </summary>
        public void AsegurarZonasPorDefecto(int sedeID)
        {
            _capaDatos.AsegurarZonasPorDefecto(sedeID);
        }

        /// <summary>
        /// Devuelve la zona por defecto del tipo para la sede, creándola si no existe.
        /// Devuelve null para la vista global (sede 1000).
        /// </summary>
        public Zona? ObtenerZona(int sedeID, string tipo)
        {
            return _capaDatos.ObtenerOCrearZonaPorTipo(sedeID, tipo);
        }

        public List<MiembroZonaDTO> ListarMiembrosZona(int sedeID, string tipo)
        {
            if (!EsTipoValido(tipo))
                return new List<MiembroZonaDTO>();

            return _capaDatos.ListarMiembrosZona(sedeID, tipo);
        }

        public int ContadorMiembrosZona(int sedeID, string tipo)
        {
            if (!EsTipoValido(tipo))
                return 0;

            return _capaDatos.ContadorMiembrosZona(sedeID, tipo);
        }

        public int AgregarMiembroZona(int idMiembro, int idGrupo, int sedeID, string tipo, out string mensaje)
        {
            if (!EsTipoValido(tipo))
            {
                mensaje = "Tipo de zona no válido.";
                return 0;
            }

            if (sedeID == 1000)
            {
                mensaje = "Selecciona una sede específica para añadir miembros.";
                return 0;
            }

            var zona = _capaDatos.ObtenerOCrearZonaPorTipo(sedeID, tipo);
            if (zona == null)
            {
                mensaje = "No se pudo obtener la zona de la sede.";
                return 0;
            }

            return _capaDatos.AgregarMiembroZona(idMiembro, zona.ID_zona, idGrupo, sedeID, out mensaje);
        }

        public bool EliminarMiembroZona(int idZgm, int sedeID, out string mensaje)
        {
            return _capaDatos.EliminarMiembroZona(idZgm, sedeID, out mensaje);
        }

        public bool EditarGrupoMiembroZona(int idZgm, int idGrupo, int sedeID, out string mensaje)
        {
            return _capaDatos.EditarGrupoMiembroZona(idZgm, idGrupo, sedeID, out mensaje);
        }
    }
}
