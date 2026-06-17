using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Gasto
    {
        private readonly CD_Gasto _cd;

        public CN_Gasto(CD_Gasto cd) { _cd = cd; }

        // ── GASTOS (cabecera) ──────────────────────────────────────────────

        public List<GastoDTO> ListarGastos(int sedeID) => _cd.ListarGastos(sedeID);

        public int IngresarGasto(Gasto obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            if (obj.cantidad <= 0) { mensaje = "La cantidad debe ser mayor que cero."; return 0; }
            if (string.IsNullOrWhiteSpace(obj.razon)) { mensaje = "La razón del gasto no puede estar vacía."; return 0; }
            if (obj.id_miembro <= 0) { mensaje = "Debes seleccionar el miembro que registra el gasto."; return 0; }
            if (obj.fecha_gasto == null) { mensaje = "La fecha del gasto es obligatoria."; return 0; }

            obj.id_sede = sedeID;
            return _cd.IngresarGasto(obj, out mensaje);
        }

        public bool EditarGasto(Gasto obj, int sedeID, out string mensaje)
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

        // ── DETALLE DE PAGO ────────────────────────────────────────────────

        public List<DetallePagoDTO> ListarDetallePagos(int sedeID) => _cd.ListarDetallePagos(sedeID);

        public int IngresarDetallePago(DetallePago obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            if (obj.numero_pago <= 0) { mensaje = "El número de pago es obligatorio."; return 0; }
            if (obj.cantidad <= 0) { mensaje = "La cantidad debe ser mayor que cero."; return 0; }
            if (string.IsNullOrWhiteSpace(obj.razon)) { mensaje = "La razón no puede estar vacía."; return 0; }
            if (obj.id_miembro <= 0) { mensaje = "Debes seleccionar el miembro."; return 0; }
            if (obj.fecha == null) { mensaje = "La fecha es obligatoria."; return 0; }

            obj.id_sede = sedeID;
            return _cd.IngresarDetallePago(obj, out mensaje);
        }

        public bool EditarDetallePago(DetallePago obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            if (obj.numero_pago <= 0) { mensaje = "El número de pago es obligatorio."; return false; }
            if (obj.cantidad <= 0) { mensaje = "La cantidad debe ser mayor que cero."; return false; }
            if (string.IsNullOrWhiteSpace(obj.razon)) { mensaje = "La razón no puede estar vacía."; return false; }
            if (obj.id_miembro <= 0) { mensaje = "Debes seleccionar el miembro."; return false; }

            obj.id_sede = sedeID;
            return _cd.EditarDetallePago(obj, out mensaje);
        }

        public bool EliminarDetallePago(int id, int sedeID, out string mensaje) =>
            _cd.EliminarDetallePago(id, sedeID, out mensaje);
    }
}
