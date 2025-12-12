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
                System.Diagnostics.Debug.WriteLine($"Error al leer las familias: {ex.Message}");
                return new List<Familia>();
            }
        }

        // ----------------------------------------------------
        // ✅ RegistrarFamilia (No requiere cambio en el ID_sede ya que valida contra obj.ID_sede, que es el ID real)
        // ----------------------------------------------------
        /// <summary>
        /// Método de registrar familia.
        /// </summary>
        public int RegistrarFamilia(Familia obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                // 🌟 FILTRO CLAVE: Verificar la existencia del correo SOLO en la sede actual (obj.ID_sede).
                // obj.ID_sede debe contener el ID real de la sede (NUNCA 1000/0).
                bool existeCorreo = _context.Familia
                    .Any(f => f.ID_sede == obj.ID_sede);

                if (existeCorreo)
                {
                    mensaje = "El correo familiar ya existe en esta sede.";
                    return 0;
                }

                _context.Familia.Add(obj);
                _context.SaveChanges();

                mensaje = "Se ha registrado correctamente a esta familia.";
                return obj.ID_familia;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar a la familia: " + ex.Message;
                return 0;
            }
        }

        // ----------------------------------------------------
        // ✅ EditarFamilia (No requiere cambio en el ID_sede ya que valida contra obj.ID_sede, que es el ID real)
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

                // 🌟 FILTRO CLAVE: Verificar que el nuevo correo no exista en otra familia DENTRO DE LA MISMA SEDE.
                bool existeOtroCorreo = _context.Familia
                    .Any(f => f.ID_familia != obj.ID_familia &&
                              f.ID_sede == obj.ID_sede); // Aplicamos el filtro de sede

                if (existeOtroCorreo)
                {
                    mensaje = "El correo ya está en uso por otra familia en esta sede.";
                    return false;
                }

                // Actualizar los campos
                familiaExistente.Nombre_familia = obj.Nombre_familia;
                familiaExistente.Telefono_familia = obj.Telefono_familia;
                familiaExistente.Direccion = obj.Direccion;
                familiaExistente.Municipio = obj.Municipio;

                _context.SaveChanges();

                mensaje = "Familia actualizada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar los datos de la familia: " + ex.Message;
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
                mensaje = "Error al eliminar la familia: " + ex.Message;
                return false;
            }
        }
    }
}