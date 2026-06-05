using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaDatos
{
    public class CD_Lideres
    {
        private readonly AppDbContext _context;

        public CD_Lideres(AppDbContext context)
        {
            _context = context;
        }

        public List<object> ListarLideres(int sedeID)
        {
            try
            {
                var consulta = _context.Lideres.AsQueryable();
                if (sedeID != 1000)
                    consulta = consulta.Where(l => l.ID_sede == sedeID);

                var lideres = consulta.OrderByDescending(l => l.ID).ToList();

                var zonas = _context.Zona.ToDictionary(z => z.ID_zona, z => z.nombre_zona ?? "");
                var grupos = _context.Grupos.ToDictionary(g => g.ID_grupo, g => g.Descripcion ?? "");
                var ministerios = _context.Ministerios.ToDictionary(m => m.ID, m => m.Descripcion ?? "");

                return lideres.Select(l => (object)new
                {
                    id = l.ID,
                    id_miembro = l.ID_miembro,
                    nombre_miembro = l.Nombre_miembro,
                    apellidos_miembro = l.Apellidos_miembro,
                    id_zona = l.ID_zona,
                    nombre_zona = l.ID_zona > 0 && zonas.ContainsKey(l.ID_zona) ? zonas[l.ID_zona] : "—",
                    id_grupo = l.ID_grupo,
                    descripcion_grupo = l.ID_grupo > 0 && grupos.ContainsKey(l.ID_grupo) ? grupos[l.ID_grupo] : "—",
                    id_ministerio = l.ID_ministerio,
                    descripcion_ministerio = l.ID_ministerio > 0 && ministerios.ContainsKey(l.ID_ministerio) ? ministerios[l.ID_ministerio] : "—",
                    tiene_usuario = l.Tiene_usuario,
                    id_sede = l.ID_sede
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al listar líderes: {ex.Message}");
                return new List<object>();
            }
        }

        public int RegistrarLider(Lider obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                bool duplicado = _context.Lideres
                    .Any(l => l.ID_miembro == obj.ID_miembro && l.ID_sede == obj.ID_sede);
                if (duplicado)
                {
                    mensaje = "Este miembro ya está registrado como líder en esta sede.";
                    return 0;
                }

                _context.Lideres.Add(obj);
                _context.SaveChanges();
                mensaje = "Líder registrado correctamente.";
                return obj.ID;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el líder: " + ex.Message;
                return 0;
            }
        }

        public bool EditarLider(Lider obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existing = _context.Lideres.FirstOrDefault(l => l.ID == obj.ID);
                if (existing == null)
                {
                    mensaje = "Líder no encontrado.";
                    return false;
                }
                if (sedeID != 1000 && existing.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El registro no pertenece a tu sede.";
                    return false;
                }

                bool duplicado = _context.Lideres
                    .Any(l => l.ID_miembro == obj.ID_miembro && l.ID_sede == existing.ID_sede && l.ID != obj.ID);
                if (duplicado)
                {
                    mensaje = "Este miembro ya está registrado como líder en esta sede.";
                    return false;
                }

                existing.ID_miembro = obj.ID_miembro;
                existing.Nombre_miembro = obj.Nombre_miembro;
                existing.Apellidos_miembro = obj.Apellidos_miembro;
                existing.ID_zona = obj.ID_zona;
                existing.ID_grupo = obj.ID_grupo;
                existing.ID_ministerio = obj.ID_ministerio;
                existing.Tiene_usuario = obj.Tiene_usuario;
                _context.SaveChanges();

                mensaje = "Líder actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar el líder: " + ex.Message;
                return false;
            }
        }

        public bool EliminarLider(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var obj = _context.Lideres.FirstOrDefault(l => l.ID == id);
                if (obj == null)
                {
                    mensaje = "Líder no encontrado.";
                    return false;
                }
                if (sedeID != 1000 && obj.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El registro no pertenece a tu sede.";
                    return false;
                }

                _context.Lideres.Remove(obj);
                _context.SaveChanges();
                mensaje = "Líder eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el líder: " + ex.Message;
                return false;
            }
        }
    }
}
