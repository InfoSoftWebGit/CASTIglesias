using CapaEntidad;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    public class CD_Jovenes
    {
        private readonly AppDbContext _context;

        public CD_Jovenes(AppDbContext context)
        {
            _context = context;
        }

        public List<JovenDTO> ListarJovenes(int sedeID, int idZona)
        {
            var query = from mzgm in _context.Miembros_Zona_Grupo_Ministerio
                        join m in _context.Miembros on mzgm.ID_miembro equals m.id_miembro
                        join g in _context.Grupos on mzgm.ID_grupo equals g.ID_grupo into gGroup
                        from g in gGroup.DefaultIfEmpty()
                        where mzgm.ID_zona == idZona
                              && (sedeID == 1000 || mzgm.ID_sede == sedeID)
                        select new JovenDTO
                        {
                            id_miembro = m.id_miembro,
                            id_zgm = mzgm.ID,
                            nombre_miembro = m.nombre_miembro,
                            apellidos_miembro = m.apellidos_miembro,
                            telefono_movil = m.telefono_movil,
                            edad = m.edad,
                            nombre_grupo = g != null ? g.Descripcion : null,
                            id_grupo = mzgm.ID_grupo,
                            estado = m.estado
                        };
            return query.ToList();
        }

        public List<JovenDTO> ListarJovenesProximosSalir(int sedeID, int idZona, int edadMaxima)
        {
            int edadUmbral = edadMaxima - 1;
            var query = from mzgm in _context.Miembros_Zona_Grupo_Ministerio
                        join m in _context.Miembros on mzgm.ID_miembro equals m.id_miembro
                        join g in _context.Grupos on mzgm.ID_grupo equals g.ID_grupo into gGroup
                        from g in gGroup.DefaultIfEmpty()
                        where mzgm.ID_zona == idZona
                              && (sedeID == 1000 || mzgm.ID_sede == sedeID)
                              && m.edad != null
                              && m.edad >= edadUmbral
                              && m.edad <= edadMaxima
                        select new JovenDTO
                        {
                            id_miembro = m.id_miembro,
                            id_zgm = mzgm.ID,
                            nombre_miembro = m.nombre_miembro,
                            apellidos_miembro = m.apellidos_miembro,
                            telefono_movil = m.telefono_movil,
                            edad = m.edad,
                            nombre_grupo = g != null ? g.Descripcion : null,
                            id_grupo = mzgm.ID_grupo,
                            estado = m.estado
                        };
            return query.ToList();
        }

        public bool YaEsJoven(int idMiembro, int idZona)
        {
            return _context.Miembros_Zona_Grupo_Ministerio
                .Any(z => z.ID_miembro == idMiembro && z.ID_zona == idZona);
        }

        public int AgregarJoven(int idMiembro, int idZona, int idGrupo, int idSede, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (YaEsJoven(idMiembro, idZona))
                {
                    mensaje = "Este miembro ya pertenece a la zona de jóvenes.";
                    return 0;
                }

                var entry = new Miembro_zona_grupo_ministerio
                {
                    ID_miembro = idMiembro,
                    ID_zona = idZona,
                    ID_grupo = idGrupo,
                    ID_ministerio = 0,
                    ID_sede = idSede
                };

                _context.Miembros_Zona_Grupo_Ministerio.Add(entry);
                _context.SaveChanges();
                mensaje = "Joven añadido correctamente.";
                return entry.ID;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al añadir joven: {ex.Message}";
                return 0;
            }
        }

        public bool EliminarJoven(int idZgm, int idSede, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var entry = _context.Miembros_Zona_Grupo_Ministerio
                    .FirstOrDefault(z => z.ID == idZgm);

                if (entry == null)
                {
                    mensaje = "Registro no encontrado.";
                    return false;
                }

                if (idSede != 1000 && entry.ID_sede != idSede)
                {
                    mensaje = "Acción denegada.";
                    return false;
                }

                _context.Miembros_Zona_Grupo_Ministerio.Remove(entry);
                _context.SaveChanges();
                mensaje = "Joven eliminado de la zona correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al eliminar: {ex.Message}";
                return false;
            }
        }
    }
}
