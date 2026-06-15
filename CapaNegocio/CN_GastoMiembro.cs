using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_GastoMiembro
    {
        private readonly CD_GastoMiembro _cd;

        public CN_GastoMiembro(CD_GastoMiembro cd) { _cd = cd; }

        public List<GastoDTO> ListarGastos(int sedeID) => _cd.ListarGastos(sedeID);

        public int IngresarGasto(GastoMiembro obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            if (obj.cantidad <= 0) { mensaje = "La cantidad debe ser mayor que cero."; return 0; }
            if (string.IsNullOrWhiteSpace(obj.razon)) { mensaje = "La razón del gasto no puede estar vacía."; return 0; }
            if (obj.id_miembro <= 0) { mensaje = "Debes seleccionar el miembro que registra el gasto."; return 0; }
            if (obj.fecha_gasto == null) { mensaje = "La fecha del gasto es obligatoria."; return 0; }

            obj.id_sede = sedeID;
            return _cd.IngresarGasto(obj, out mensaje);
        }

        public bool EditarGasto(GastoMiembro obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            if (obj.cantidad <= 0) { mensaje = "La cantidad debe ser mayor que cero."; return false; }
            if (string.IsNullOrWhiteSpace(obj.razon)) { mensaje = "La razón del gasto no puede estar vacía."; return false; }
            if (obj.id_miembro <= 0) { mensaje = "Debes seleccionar el miembro que registra el gasto."; return false; }

            obj.id_sede = sedeID;
            return _cd.EditarGasto(obj, out mensaje);
        }

        public bool EliminarGasto(int id, int sedeID, out string mensaje) =>
            _cd.EliminarGasto(id, sedeID, out mensaje);
    }
}
