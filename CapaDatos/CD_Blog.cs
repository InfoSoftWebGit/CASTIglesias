using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaDatos
{
    /// <summary>
    /// Acceso a datos de los artículos del blog público.
    /// </summary>
    public class CD_Blog
    {
        private readonly AppDbContext _context;

        public CD_Blog(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Artículos visibles en la web: publicados y con fecha ya alcanzada.</summary>
        public List<BlogArticulo> ListarPublicados(DateTime ahora)
        {
            return _context.BlogArticulos
                .Where(a => a.publicado && a.fecha_publicacion != null && a.fecha_publicacion <= ahora)
                .OrderByDescending(a => a.fecha_publicacion)
                .ToList();
        }

        /// <summary>Todos los artículos, también los borradores (panel de administración).</summary>
        public List<BlogArticulo> ListarTodos()
        {
            return _context.BlogArticulos
                .OrderByDescending(a => a.fecha_modificacion ?? a.fecha_alta)
                .ToList();
        }

        public BlogArticulo? BuscarPorId(int id) =>
            _context.BlogArticulos.FirstOrDefault(a => a.ID_articulo == id);

        public BlogArticulo? BuscarPorSlug(string slug) =>
            _context.BlogArticulos.FirstOrDefault(a => a.slug == slug);

        /// <summary>Indica si otra entrada distinta de <paramref name="idExcluir"/> usa ya esa dirección.</summary>
        public bool ExisteSlug(string slug, int idExcluir) =>
            _context.BlogArticulos.Any(a => a.slug == slug && a.ID_articulo != idExcluir);

        /// <returns>El ID del artículo, o 0 si no se pudo guardar.</returns>
        public int Guardar(BlogArticulo obj, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                if (obj.ID_articulo == 0)
                    _context.BlogArticulos.Add(obj);
                else
                    _context.BlogArticulos.Update(obj);

                _context.SaveChanges();
                return obj.ID_articulo;
            }
            catch (Exception ex)
            {
                mensaje = "Error al guardar el artículo: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var articulo = BuscarPorId(id);
                if (articulo == null)
                {
                    mensaje = "El artículo no existe.";
                    return false;
                }

                _context.BlogArticulos.Remove(articulo);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar el artículo: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
