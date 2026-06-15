using CapaEntidad;

namespace CapaDatos
{
    public class CD_Matrimonio
    {
        private readonly AppDbContext _context;

        public CD_Matrimonio(AppDbContext context)
        {
            _context = context;
        }

        public List<MatrimonioDTO> ListarMatrimonios(int sedeID)
        {
            try
            {
                var query = from m in _context.Matrimonios
                            join s in _context.Matrimonios on m.ID_pareja_seguidor equals s.ID_matrimonio into seg
                            from s in seg.DefaultIfEmpty()
                            where sedeID == 1000 || m.ID_sede == sedeID
                            select new MatrimonioDTO
                            {
                                ID_matrimonio = m.ID_matrimonio,
                                Nombres = m.Nombres,
                                Lideres = m.Lideres,
                                ID_pareja_seguidor = m.ID_pareja_seguidor,
                                Nombres_seguidor = s != null ? s.Nombres : null,
                                ID_sede = m.ID_sede
                            };
                return query.ToList();
            }
            catch
            {
                return new List<MatrimonioDTO>();
            }
        }

        public int RegistrarMatrimonio(Matrimonio obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.Matrimonios.Add(obj);
                _context.SaveChanges();
                mensaje = "Matrimonio registrado correctamente.";
                return obj.ID_matrimonio;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el matrimonio: " + ex.Message;
                return 0;
            }
        }

        public bool EditarMatrimonio(Matrimonio obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existente = _context.Matrimonios.FirstOrDefault(m => m.ID_matrimonio == obj.ID_matrimonio);
                if (existente == null)
                {
                    mensaje = "Matrimonio no encontrado.";
                    return false;
                }
                if (existente.ID_sede != obj.ID_sede)
                {
                    mensaje = "Acción denegada. El matrimonio no pertenece a tu sede.";
                    return false;
                }

                existente.Nombres = obj.Nombres;
                existente.Lideres = obj.Lideres;
                existente.ID_pareja_seguidor = obj.ID_pareja_seguidor == 0 ? null : obj.ID_pareja_seguidor;
                _context.SaveChanges();
                mensaje = "Matrimonio actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el matrimonio: " + ex.Message;
                return false;
            }
        }

        public bool EliminarMatrimonio(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var obj = _context.Matrimonios.FirstOrDefault(m => m.ID_matrimonio == id);
                if (obj == null)
                {
                    mensaje = "Matrimonio no encontrado.";
                    return false;
                }
                if (sedeID != 1000 && obj.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El matrimonio no pertenece a tu sede.";
                    return false;
                }

                _context.Matrimonios.Remove(obj);
                _context.SaveChanges();
                mensaje = "Matrimonio eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el matrimonio: " + ex.Message;
                return false;
            }
        }
    }
}
