using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Artículo del blog público de Congrega (congrega.es/blog).
    /// </summary>
    /// <remarks>
    /// No implementa ITieneIglesia a propósito: el blog es de Congrega, no de una
    /// iglesia cliente. Con la interfaz, el filtro global por iglesia dejaría la
    /// tabla vacía en la página pública, donde no hay sesión.
    /// </remarks>
    [Table("blog_articulos")]
    public class BlogArticulo
    {
        [Key]
        public int ID_articulo { get; set; }

        public string titulo { get; set; } = string.Empty;

        /// <summary>Parte final de la dirección: congrega.es/blog/{slug}.</summary>
        public string slug { get; set; } = string.Empty;

        /// <summary>Entradilla del listado; también es la descripción para Google.</summary>
        public string resumen { get; set; } = string.Empty;

        /// <summary>Texto del artículo en Markdown.</summary>
        public string contenido { get; set; } = string.Empty;

        public string? imagen_portada { get; set; }

        public string? autor { get; set; }

        /// <summary>"es" o "en": idioma en que está escrito el artículo.</summary>
        public string idioma { get; set; } = "es";

        public bool publicado { get; set; }

        /// <summary>Se fija la primera vez que se publica y no cambia al editar.</summary>
        public DateTime? fecha_publicacion { get; set; }

        public DateTime fecha_alta { get; set; }

        public DateTime? fecha_modificacion { get; set; }
    }
}
