using CapaEntidad;
using System.Linq; // Necesario para LINQ
using System.Collections.Generic;
using System; // Necesario para Exception y Console

namespace CapaDatos
{
    public class CD_Zona
    {
        private readonly AppDbContext _context;

        // Constructor que recibe el DbContext mediante DI
        public CD_Zona(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista las Zonas. Si sedeID es 1000 (Admin Global), devuelve todas. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para todos).</param>
        public List<Zona> ListarZonas(int sedeID)
        {
            try
            {
                var consultaBase = _context.Zona.AsQueryable();

                // Lógica condicional para manejar sedeID = 1000 (Admin Global)
                if (sedeID != 1000)
                {
                    // Si sedeID es diferente de 1000, aplica el filtro por ID_sede.
                    consultaBase = consultaBase.Where(z => z.ID_sede == sedeID);
                }
                // Si sedeID es 1000, no se aplica filtro.

                var zonas = consultaBase.ToList();

                Console.WriteLine($"Zonas cargadas para la Sede {sedeID} (1000=Todas): {zonas.Count}");
                return zonas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer zonas con EF Core: {ex.Message}");
                return new List<Zona>();
            }
        }

        /// Método de registrar zonas.
        public int RegistrarZona(Zona obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                // La unicidad del nombre de la zona debe ser DENTRO DE LA MISMA SEDE.
                bool existezona = _context.Zona
                    .Any(f => f.nombre_zona == obj.nombre_zona && f.ID_sede == obj.ID_sede);

                if (existezona)
                {
                    mensaje = "La Zona ya existe en esta sede.";
                    return 0;
                }

                _context.Zona.Add(obj);
                _context.SaveChanges();

                mensaje = "Se ha registrado correctamente la Zona.";
                return obj.ID_zona;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar la Zona: " + ex.Message;
                return 0;
            }
        }

        /// Método Editar Zona.
        /// <summary>
        /// Edita una zona.
        /// </summary>
        public bool EditarZona(Zona objeto, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                // 1. Busca si ya existe otra zona con el mismo nombre de líder, excluyendo la zona actual y filtrando por sede.
                bool liderDuplicado = _context.Zona
                    .Any(f => f.nombre_lider == objeto.nombre_lider &&
                              f.ID_zona != objeto.ID_zona &&
                              f.ID_sede == objeto.ID_sede); // Filtro por Sede

                if (liderDuplicado)
                {
                    mensaje = "El nombre del líder ya existe en otra zona en esta sede.";
                    return false;
                }

                // 2. Busca la zona que se va a actualizar.
                var zona = _context.Zona.FirstOrDefault(f => f.ID_zona == objeto.ID_zona);

                if (zona == null)
                {
                    mensaje = "Zona no encontrada.";
                    return false;
                }

                // 3. VALIDACIÓN DE SEGURIDAD: Prevenir la edición cruzada de sedes.
                // Asegurar que la zona a editar pertenece a la misma sede del usuario logueado (objeto.ID_sede).
                if (zona.ID_sede != objeto.ID_sede)
                {
                    mensaje = "Acción denegada. La zona no pertenece a tu sede.";
                    return false;
                }

                // 4. Actualiza las propiedades.
                zona.nombre_zona = objeto.nombre_zona;
                zona.nombre_lider = objeto.nombre_lider;
                zona.descripcion = objeto.descripcion;
                // zona.ID_sede no se toca.

                _context.SaveChanges();
                mensaje = "Zona editada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al editar la zona: " + ex.Message;
                return false;
            }
        }

        /// Método de eliminar Zona.
        /// <summary>
        /// Elimina una zona. Si sedeID es 1000 (Admin Global), puede eliminar de cualquier sede. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para Admin Global).</param>
        public bool EliminarZona(int id, int sedeID, out string mensaje)
        {
            mensaje = "";
            try
            {
                // 1. Buscar la zona solo por ID.
                var z = _context.Zona.FirstOrDefault(x => x.ID_zona == id);

                if (z == null)
                {
                    mensaje = "Zona no encontrada.";
                    return false;
                }

                // 2. Control de Acceso Condicional
                // Si el usuario NO es AdminGlobal (sedeID != 1000) Y la zona no pertenece a su sede, denegamos.
                if (sedeID != 1000 && z.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. La zona no pertenece a tu sede.";
                    return false;
                }
                // Si sedeID es 1000, el AdminGlobal puede eliminar la zona de cualquier sede.

                _context.Zona.Remove(z);
                _context.SaveChanges();
                mensaje = "Zona eliminada";
                return true;
            }
            catch (Exception ex) { mensaje = "Error: " + ex.Message; return false; }
        }
        public List<Zona> BuscarZonasPorNombre(int sedeID, string nombre)
        {
            return _context.Zona
                .Where(z => z.ID_sede == sedeID &&
                            z.nombre_zona.ToLower().Contains(nombre.ToLower()))
                .ToList();
        }

    }
}