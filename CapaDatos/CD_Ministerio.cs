using CapaEntidad;
using System.Linq; // Necesario para LINQ
using System.Collections.Generic;
using System; // Necesario para Exception y Console

namespace CapaDatos
{
    public class CD_Ministerio
    {
        private readonly AppDbContext _context;

        // Constructor que recibe el DbContext mediante DI
        public CD_Ministerio(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista las Zonas. Si sedeID es 1000 (Admin Global), devuelve todas. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para todos).</param>
        public List<Ministerio> ListarMinisterios(int sedeID)
        {
            try
            {
                var consultaBase = _context.Ministerios.AsQueryable();

                // Lógica condicional para manejar sedeID = 1000 (Admin Global)
                if (sedeID != 1000)
                {
                    // Si sedeID es diferente de 1000, aplica el filtro por ID_sede.
                    consultaBase = consultaBase.Where(z => z.ID_sede == sedeID);
                }
                // Si sedeID es 1000, no se aplica filtro.

                var ministerios = consultaBase.ToList();

                Console.WriteLine($"Ministerios cargados para la Sede {sedeID} (1000=Todas): {ministerios.Count}");
                return ministerios;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer zonas con EF Core: {ErrorHelper.Mensaje(ex)}");
                return new List<Ministerio>();
            }
        }

        /// Método de registrar zonas.
        public int RegistrarMinisterio(Ministerio obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                // La unicidad del nombre de la zona debe ser DENTRO DE LA MISMA SEDE.
                bool existezona = _context.Ministerios
                    .Any(f => f.Descripcion == obj.Descripcion && f.ID_sede == obj.ID_sede);

                if (existezona)
                {
                    mensaje = "El ministerio ya existe en esta sede.";
                    return 0;
                }

                _context.Ministerios.Add(obj);
                _context.SaveChanges();

                mensaje = "Se ha registrado correctamente el Ministerio.";
                return obj.ID;
            }
            catch (Exception ex)
            {
                mensaje = "Error al registrar el Ministerio: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// Método Editar Zona.
        /// <summary>
        /// Edita una zona.
        /// </summary>
        public bool EditarMinisterio(Ministerio objeto, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {

                var ministerio = _context.Ministerios.FirstOrDefault(f => f.ID == objeto.ID);

                if (ministerio == null)
                {
                    mensaje = "Ministerio no encontrado.";
                    return false;
                }

                if (ministerio.ID_sede != objeto.ID_sede)
                {
                    mensaje = "Acción denegada. El ministerio no pertenece a tu sede.";
                    return false;
                }

                // 4. Actualiza las propiedades.
                ministerio.Descripcion = objeto.Descripcion;
                ministerio.Lider = objeto.Lider;
                // zona.ID_sede no se toca.

                _context.SaveChanges();
                mensaje = "Ministerio editado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al editar el ministerio: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }

        /// Método de eliminar Zona.
        /// <summary>
        /// Elimina una zona. Si sedeID es 1000 (Admin Global), puede eliminar de cualquier sede. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para Admin Global).</param>
        public bool EliminarMinisterio(int id, int sedeID, out string mensaje)
        {
            mensaje = "";
            try
            {
                // 1. Buscar la zona solo por ID.
                var ministerio = _context.Ministerios.FirstOrDefault(x => x.ID == id);

                if (ministerio == null)
                {
                    mensaje = "Ministerio no encontrado.";
                    return false;
                }

                // 2. Control de Acceso Condicional
                // Si el usuario NO es AdminGlobal (sedeID != 1000) Y la zona no pertenece a su sede, denegamos.
                if (sedeID != 1000 && ministerio.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada.  no pertenece a tu sede.";
                    return false;
                }
                // Si sedeID es 1000, el AdminGlobal puede eliminar la zona de cualquier sede.

                _context.Ministerios.Remove(ministerio);
                _context.SaveChanges();
                mensaje = "Ministerio eliminado";
                return true;
            }
            catch (Exception ex) { mensaje = "Error: " + ErrorHelper.Mensaje(ex); return false; }
        }
        public List<Ministerio> BuscarMinisteriosPorNombre(int sedeID, string nombre)
        {
            nombre ??= string.Empty; // si es null, lo convertimos en vacío

            return _context.Ministerios
                .Where(m => m.ID_sede == sedeID &&
                            !string.IsNullOrEmpty(m.Descripcion) &&
                            m.Descripcion.ToLower().Contains(nombre.ToLower()))
                .ToList();
        }

    }
}
