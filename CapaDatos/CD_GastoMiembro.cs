using CapaEntidad;

namespace CapaDatos
{
    public class CD_GastoMiembro
    {
        private readonly AppDbContext _context;

        public CD_GastoMiembro(AppDbContext context)
        {
            _context = context;
        }

        public List<GastoDTO> ListarGastos(int sedeID)
        {
            var query = from g in _context.GastosMiembros
                        join m in _context.Miembros on g.id_miembro equals m.id_miembro
                        join z in _context.Zona on g.id_zona equals z.ID_zona into zonaGrp
                        from z in zonaGrp.DefaultIfEmpty()
                        where sedeID == 1000 || g.id_sede == sedeID
                        orderby g.fecha_gasto descending
                        select new GastoDTO
                        {
                            id_gasto = g.id_gasto,
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

        public int IngresarGasto(GastoMiembro obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.GastosMiembros.Add(obj);
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

        public bool EditarGasto(GastoMiembro obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existente = _context.GastosMiembros.FirstOrDefault(g => g.id_gasto == obj.id_gasto);
                if (existente == null) { mensaje = "Gasto no encontrado."; return false; }

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
                var gasto = _context.GastosMiembros.FirstOrDefault(g => g.id_gasto == id);
                if (gasto == null) { mensaje = "Gasto no encontrado."; return false; }
                if (sedeID != 1000 && gasto.id_sede != sedeID)
                {
                    mensaje = "Acción denegada. El gasto no pertenece a tu sede.";
                    return false;
                }
                _context.GastosMiembros.Remove(gasto);
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
    }
}
