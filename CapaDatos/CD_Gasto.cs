using CapaEntidad;

namespace CapaDatos
{
    public class CD_Gasto
    {
        private readonly AppDbContext _context;

        public CD_Gasto(AppDbContext context)
        {
            _context = context;
        }

        // ── GASTOS (cabecera) ──────────────────────────────────────────────

        public List<GastoDTO> ListarGastos(int sedeID)
        {
            var query = from g in _context.Gastos
                        join m in _context.Miembros on g.id_miembro equals m.id_miembro
                        join z in _context.Zona on g.id_zona equals z.ID_zona into zonaGrp
                        from z in zonaGrp.DefaultIfEmpty()
                        where sedeID == 1000 || g.id_sede == sedeID
                        orderby g.fecha_gasto descending
                        select new GastoDTO
                        {
                            id_gasto = g.id_gasto,
                            numero_pago = g.numero_pago,
                            cantidad = g.cantidad,
                            id_zona = g.id_zona,
                            nombre_zona = z != null ? z.nombre_zona : null,
                            razon = g.razon,
                            id_miembro = g.id_miembro,
                            nombre_miembro_completo = m.nombre_miembro + " " + m.apellidos_miembro,
                            tipo_cuenta = g.tipo_cuenta,
                            devuelto = g.devuelto,
                            fecha_gasto = g.fecha_gasto
                        };
            return query.ToList();
        }

        public int IngresarGasto(Gasto obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                // numero_pago lo asigna el trigger BEFORE INSERT de la BD
                _context.Gastos.Add(obj);
                _context.SaveChanges();

                // Recargar para obtener el numero_pago que asignó el trigger
                _context.Entry(obj).Reload();

                var detalle = new DetallePago
                {
                    numero_pago = obj.numero_pago!.Value,
                    id_sede     = obj.id_sede,
                    cantidad    = obj.cantidad,
                    razon       = obj.razon,
                    tipo_cuenta = obj.tipo_cuenta,
                    devuelto    = obj.devuelto,
                    fecha       = obj.fecha_gasto,
                    id_miembro  = obj.id_miembro
                };
                _context.DetallePagos.Add(detalle);
                _context.SaveChanges();

                mensaje = "Gasto registrado correctamente.";
                return obj.id_gasto;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el gasto: " + ex.Message;
                return 0;
            }
        }

        public bool EditarGasto(Gasto obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existente = _context.Gastos.FirstOrDefault(g => g.id_gasto == obj.id_gasto);
                if (existente == null) { mensaje = "Gasto no encontrado."; return false; }

                existente.numero_pago = obj.numero_pago;
                existente.cantidad = obj.cantidad;
                existente.id_zona = obj.id_zona;
                existente.razon = obj.razon;
                existente.id_miembro = obj.id_miembro;
                existente.tipo_cuenta = obj.tipo_cuenta;
                existente.devuelto = obj.devuelto;
                existente.fecha_gasto = obj.fecha_gasto;

                _context.SaveChanges();
                mensaje = "Gasto actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el gasto: " + ex.Message;
                return false;
            }
        }

        public bool EliminarGasto(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var gasto = _context.Gastos.FirstOrDefault(g => g.id_gasto == id);
                if (gasto == null) { mensaje = "Gasto no encontrado."; return false; }
                if (sedeID != 1000 && gasto.id_sede != sedeID)
                {
                    mensaje = "Acción denegada. El gasto no pertenece a tu sede.";
                    return false;
                }
                _context.Gastos.Remove(gasto);
                _context.SaveChanges();
                mensaje = "Gasto eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el gasto: " + ex.Message;
                return false;
            }
        }

        // ── DETALLE DE PAGO ────────────────────────────────────────────────

        public List<DetallePagoDTO> ListarDetallePagos(int sedeID)
        {
            var query = from d in _context.DetallePagos
                        join m in _context.Miembros on d.id_miembro equals m.id_miembro
                        where sedeID == 1000 || d.id_sede == sedeID
                        orderby d.fecha descending
                        select new DetallePagoDTO
                        {
                            id_detalle = d.id_detalle,
                            numero_pago = d.numero_pago,
                            cantidad = d.cantidad,
                            razon = d.razon,
                            tipo_cuenta = d.tipo_cuenta,
                            devuelto = d.devuelto,
                            fecha = d.fecha,
                            id_miembro = d.id_miembro,
                            nombre_miembro_completo = m.nombre_miembro + " " + m.apellidos_miembro
                        };
            return query.ToList();
        }

        public int IngresarDetallePago(DetallePago obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.DetallePagos.Add(obj);
                _context.SaveChanges();
                mensaje = "Detalle de pago registrado correctamente.";
                return obj.id_detalle;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el detalle: " + ex.Message;
                return 0;
            }
        }

        public bool EditarDetallePago(DetallePago obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existente = _context.DetallePagos.FirstOrDefault(d => d.id_detalle == obj.id_detalle);
                if (existente == null) { mensaje = "Detalle no encontrado."; return false; }

                existente.numero_pago = obj.numero_pago;
                existente.cantidad = obj.cantidad;
                existente.razon = obj.razon;
                existente.tipo_cuenta = obj.tipo_cuenta;
                existente.devuelto = obj.devuelto;
                existente.fecha = obj.fecha;
                existente.id_miembro = obj.id_miembro;

                _context.SaveChanges();
                mensaje = "Detalle de pago actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el detalle: " + ex.Message;
                return false;
            }
        }

        public bool EliminarDetallePago(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var detalle = _context.DetallePagos.FirstOrDefault(d => d.id_detalle == id);
                if (detalle == null) { mensaje = "Detalle no encontrado."; return false; }
                if (sedeID != 1000 && detalle.id_sede != sedeID)
                {
                    mensaje = "Acción denegada. El detalle no pertenece a tu sede.";
                    return false;
                }
                _context.DetallePagos.Remove(detalle);
                _context.SaveChanges();
                mensaje = "Detalle de pago eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el detalle: " + ex.Message;
                return false;
            }
        }
    }
}
