using CapaEntidad;

namespace CapaDatos
{
    public class CD_Culto
    {
        private readonly AppDbContext _context;

        public CD_Culto(AppDbContext context) => _context = context;

        public List<Culto> Listar(int sedeID)
        {
            try
            {
                if (sedeID == 1000)
                    return _context.Cultos.OrderBy(c => c.dia_semana).ThenBy(c => c.nombre).ToList();
                return _context.Cultos
                    .Where(c => c.id_sede == sedeID)
                    .OrderBy(c => c.dia_semana).ThenBy(c => c.nombre)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_Culto.Listar: {ErrorHelper.Mensaje(ex)} | Inner: {ex.InnerException?.Message}");
                return new List<Culto>();
            }
        }

        // Guarda culto + sus bloques en una transacción
        public bool GuardarConBloques(CultoConBloquesDTO dto, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            using var tx = _context.Database.BeginTransaction();
            try
            {
                Culto culto;
                if (dto.id_culto == 0)
                {
                    culto = new Culto { nombre = dto.nombre, dia_semana = dto.dia_semana, id_sede = sedeID };
                    _context.Cultos.Add(culto);
                    _context.SaveChanges();
                }
                else
                {
                    culto = _context.Cultos.Find(dto.id_culto)!;
                    if (culto == null || (culto.id_sede != sedeID && sedeID != 1000))
                    {
                        mensaje = "Culto no encontrado o sin permiso.";
                        return false;
                    }
                    culto.nombre = dto.nombre;
                    culto.dia_semana = dto.dia_semana;
                    _context.SaveChanges();
                }

                // Sincronizar bloques: comparar IDs existentes vs enviados
                var bloquesExistentes = _context.BloquesCulto.Where(b => b.id_culto == culto.id_culto).ToList();
                var idsEnviados = dto.bloques.Where(b => b.id_bloque > 0).Select(b => b.id_bloque).ToHashSet();

                // Eliminar bloques que ya no están (y sus requerimientos)
                foreach (var b in bloquesExistentes.Where(b => !idsEnviados.Contains(b.id_bloque)))
                {
                    var reqs = _context.RequerimientosCulto.Where(r => r.id_bloque == b.id_bloque).ToList();
                    _context.RequerimientosCulto.RemoveRange(reqs);
                    _context.BloquesCulto.Remove(b);
                }
                _context.SaveChanges();

                // Añadir / actualizar bloques enviados
                for (int i = 0; i < dto.bloques.Count; i++)
                {
                    var bDto = dto.bloques[i];
                    bDto.orden = i + 1;
                    if (bDto.id_bloque == 0)
                    {
                        bDto.id_culto = culto.id_culto;
                        bDto.id_sede = sedeID;
                        _context.BloquesCulto.Add(bDto);
                    }
                    else
                    {
                        var existente = bloquesExistentes.First(b => b.id_bloque == bDto.id_bloque);
                        existente.hora_inicio = bDto.hora_inicio;
                        existente.hora_fin = bDto.hora_fin;
                        existente.orden = bDto.orden;
                    }
                }
                _context.SaveChanges();
                tx.Commit();
                mensaje = dto.id_culto == 0 ? "Culto registrado correctamente." : "Culto actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Console.WriteLine($"CD_Culto.GuardarConBloques: {ErrorHelper.Mensaje(ex)}");
                mensaje = $"Error al guardar: {ErrorHelper.Mensaje(ex)}";
                return false;
            }
        }

        public bool Eliminar(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var obj = _context.Cultos.Find(id);
                if (obj == null || (obj.id_sede != sedeID && sedeID != 1000))
                {
                    mensaje = "Culto no encontrado o sin permiso.";
                    return false;
                }
                var reqs = _context.RequerimientosCulto.Where(r => r.id_culto == id).ToList();
                _context.RequerimientosCulto.RemoveRange(reqs);
                _context.SaveChanges();

                var bloques = _context.BloquesCulto.Where(b => b.id_culto == id).ToList();
                _context.BloquesCulto.RemoveRange(bloques);
                _context.SaveChanges();

                _context.Cultos.Remove(obj);
                _context.SaveChanges();
                mensaje = "Culto eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CD_Culto.Eliminar: {ErrorHelper.Mensaje(ex)} | Inner: {ex.InnerException?.Message}");
                mensaje = $"Error al eliminar: {ErrorHelper.Mensaje(ex)}";
                return false;
            }
        }
    }
}
