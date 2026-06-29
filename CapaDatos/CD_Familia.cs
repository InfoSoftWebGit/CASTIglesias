using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq; // Necesario para usar .Where() y .Any()

namespace CapaDatos
{
    public class CD_Familia
    {
        private readonly AppDbContext _context;

        // Inyección de dependencias: el contexto se pasa desde fuera
        public CD_Familia(AppDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------
        // ✅ ListarFamilias: Filtro Condicional
        // ----------------------------------------------------
        /// <summary>
        /// Método de Listar Familias. Si sedeID es 1000 (Admin Global), devuelve todas. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para todas).</param>
        public List<Familia> ListarFamilias(int sedeID)
        {
            try
            {
                var consultaBase = _context.Familia.AsQueryable();

                if (sedeID != 1000)
                {
                    consultaBase = consultaBase.Where(f => f.ID_sede == sedeID);
                }
                // Si sedeID es 1000, no aplicamos el filtro y la consultaBase contiene todas las familias.

                var familias = consultaBase.ToList();

                System.Diagnostics.Debug.WriteLine($"Familias cargadas para la Sede {sedeID} (1000=Todas): {familias.Count}");
                return familias;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al leer las familias: {ErrorHelper.Mensaje(ex)}");
                return new List<Familia>();
            }
        }

        // ----------------------------------------------------
        // ✅ RegistrarFamilia
        // ----------------------------------------------------
        /// <summary>
        /// Método de registrar familia.
        /// </summary>
        public int RegistrarFamilia(Familia obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                _context.Familia.Add(obj);
                _context.SaveChanges();

                mensaje = "Se ha registrado correctamente a esta familia.";
                return obj.ID_familia;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar a la familia: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        // ----------------------------------------------------
        // ✅ EditarFamilia
        // ----------------------------------------------------
        /// <summary>
        /// Método Editar Familia.
        /// </summary>
        public bool EditarFamilia(Familia obj, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                var familiaExistente = _context.Familia.FirstOrDefault(f => f.ID_familia == obj.ID_familia);

                if (familiaExistente == null)
                {
                    mensaje = "Familia no encontrada.";
                    return false;
                }

                // ⚠️ VALIDACIÓN DE SEGURIDAD: Prevenir la edición cruzada de sedes.
                if (familiaExistente.ID_sede != obj.ID_sede)
                {
                    mensaje = "Acción denegada. La familia no pertenece a tu sede.";
                    return false;
                }

                // Actualizar los campos
                familiaExistente.Nombre_familia = obj.Nombre_familia;
                familiaExistente.Telefono_familia = obj.Telefono_familia;
                familiaExistente.Direccion = obj.Direccion;
                familiaExistente.Municipio = obj.Municipio;
                familiaExistente.Provincia = obj.Provincia;
                familiaExistente.CP = obj.CP;
                familiaExistente.Integrantes = obj.Integrantes;

                _context.SaveChanges();

                mensaje = "Familia actualizada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar los datos de la familia: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        // ----------------------------------------------------
        // ✅ EliminarFamilia: Control de Acceso Condicional
        // ----------------------------------------------------
        /// <summary>
        /// Método de eliminar familia. Si sedeID es 1000 (Admin Global), puede eliminar de cualquier sede. Si es != 1000, solo de su sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para Admin Global).</param>
        public bool EliminarFamilia(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                // Buscamos la familia por su ID principal.
                var familia = _context.Familia
                    .FirstOrDefault(f => f.ID_familia == id);

                if (familia == null)
                {
                    mensaje = "Familia no encontrada.";
                    return false;
                }

                // 🌟 CORRECCIÓN: Control de Acceso Condicional
                // Si el usuario NO es AdminGlobal (sedeID != 1000) Y la familia no pertenece a su sede, denegamos.
                if (sedeID != 1000 && familia.ID_sede != sedeID) // 👈 CAMBIO: Usamos 1000
                {
                    mensaje = "Acción denegada. La familia no pertenece a tu sede.";
                    return false;
                }
                // Si sedeID es 1000, el AdminGlobal puede eliminar la familia de cualquier sede.

                _context.Familia.Remove(familia);
                _context.SaveChanges();

                mensaje = "Familia eliminada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar la familia: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        // ----------------------------------------------------
        // ListarMiembrosDeFamilia
        // ----------------------------------------------------
        public List<MiembroFamiliaDTO> ListarMiembrosDeFamilia(int idFamilia, int sedeID)
        {
            try
            {
                var q = _context.Miembros.Where(m => m.id_familia == idFamilia);
                if (sedeID != 1000)
                    q = q.Where(m => m.id_sede == sedeID);

                return q.Select(m => new MiembroFamiliaDTO
                {
                    id_miembro           = m.id_miembro,
                    nombre_miembro       = m.nombre_miembro,
                    apellidos_miembro    = m.apellidos_miembro,
                    miembro_activo       = m.miembro_activo,
                    fallecido            = m.fallecido,
                    tipo_relacion_familiar = m.tipo_relacion_familiar
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ListarMiembrosDeFamilia: {ErrorHelper.Mensaje(ex)}");
                return new List<MiembroFamiliaDTO>();
            }
        }

        // ----------------------------------------------------
        // BuscarMiembrosParaAsignar
        // ----------------------------------------------------
        public List<MiembroFamiliaDTO> BuscarMiembrosParaAsignar(string query, int sedeID)
        {
            try
            {
                var q = _context.Miembros.AsQueryable();
                if (sedeID != 1000)
                    q = q.Where(m => m.id_sede == sedeID);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var lower = query.ToLower();
                    q = q.Where(m =>
                        (m.nombre_miembro != null && m.nombre_miembro.ToLower().Contains(lower)) ||
                        (m.apellidos_miembro != null && m.apellidos_miembro.ToLower().Contains(lower)));
                }

                return q.OrderBy(m => m.apellidos_miembro)
                        .Take(30)
                        .Select(m => new MiembroFamiliaDTO
                        {
                            id_miembro             = m.id_miembro,
                            nombre_miembro         = m.nombre_miembro,
                            apellidos_miembro      = m.apellidos_miembro,
                            miembro_activo         = m.miembro_activo,
                            fallecido              = m.fallecido,
                            tipo_relacion_familiar = m.tipo_relacion_familiar
                        }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error BuscarMiembrosParaAsignar: {ErrorHelper.Mensaje(ex)}");
                return new List<MiembroFamiliaDTO>();
            }
        }

        // ----------------------------------------------------
        // AsignarMiembroAFamilia
        // ----------------------------------------------------
        public bool AsignarMiembroAFamilia(int idMiembro, int idFamilia, string tipoRelacion, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var miembro = _context.Miembros.FirstOrDefault(m => m.id_miembro == idMiembro);
                if (miembro == null)  { mensaje = "Miembro no encontrado."; return false; }
                if (sedeID != 1000 && miembro.id_sede != sedeID) { mensaje = "Sin permiso sobre este miembro."; return false; }

                var familia = _context.Familia.FirstOrDefault(f => f.ID_familia == idFamilia);
                if (familia == null)  { mensaje = "Familia no encontrada."; return false; }
                if (sedeID != 1000 && familia.ID_sede != sedeID) { mensaje = "Sin permiso sobre esta familia."; return false; }

                if (string.IsNullOrWhiteSpace(tipoRelacion))
                {
                    mensaje = "Debe seleccionar un tipo de relación familiar.";
                    return false;
                }

                miembro.id_familia             = idFamilia;
                miembro.tipo_relacion_familiar = tipoRelacion;

                // Auto-sync: si grupo_familiar estaba vacío, lo rellenamos con el nombre de la familia
                if (string.IsNullOrWhiteSpace(miembro.grupo_familiar))
                    miembro.grupo_familiar = familia.Nombre_familia;

                _context.SaveChanges();
                mensaje = "Miembro asignado a la familia correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al asignar miembro: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        // ----------------------------------------------------
        // QuitarMiembroFamilia
        // ----------------------------------------------------
        public bool QuitarMiembroFamilia(int idMiembro, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var miembro = _context.Miembros.FirstOrDefault(m => m.id_miembro == idMiembro);
                if (miembro == null)  { mensaje = "Miembro no encontrado."; return false; }
                if (sedeID != 1000 && miembro.id_sede != sedeID) { mensaje = "Sin permiso sobre este miembro."; return false; }

                miembro.id_familia             = null;
                miembro.tipo_relacion_familiar = null;
                miembro.grupo_familiar         = null;

                _context.SaveChanges();
                mensaje = "Miembro desvinculado de la familia correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al quitar miembro de la familia: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}