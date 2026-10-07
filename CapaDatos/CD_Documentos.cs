using CapaEntidad;
using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Justificantes y documentos adjuntos a las operaciones.
    /// </summary>
    /// <remarks>
    /// Aquí solo se guardan los DATOS del fichero, nunca el fichero. El contenido vive
    /// en disco, en una carpeta FUERA de wwwroot, y se sirve por una acción que
    /// comprueba permisos. Una factura puede llevar el NIF de un proveedor o el nombre
    /// de una familia a la que se ayudó: si estuviera en wwwroot, cualquiera que
    /// acertara la URL la descargaría sin pasar por la sesión.
    ///
    /// Dos columnas que parecen redundantes y no lo son:
    ///   - storage_key: el nombre con el que está en disco, que lo genera el programa.
    ///   - original_filename: cómo lo llamaba el usuario, que es lo que se le enseña.
    /// Separarlas evita que un nombre de fichero del usuario ("../../web.config")
    /// acabe decidiendo dónde se escribe algo.
    ///
    /// sha256_hash sirve para detectar que se sube dos veces la misma factura, que es
    /// el aviso de "no está duplicada" del documento funcional.
    /// </remarks>
    public class CD_Documentos
    {
        private readonly AppDbContext _context;

        public CD_Documentos(AppDbContext context) => _context = context;

        public const string Disponible = "available";
        public const string Borrado = "deleted";

        /// <summary>Tipos de entidad a los que se puede enganchar un documento.</summary>
        public const string EntidadOperacion = "financial_transactions";
        public const string EntidadAsiento = "journal_entries";
        public const string EntidadArqueo = "cash_sessions";

        /// <summary>Un adjunto, con lo que hace falta para listarlo y descargarlo.</summary>
        public class AdjuntoDTO
        {
            public int id { get; set; }
            public string? original_filename { get; set; }
            public string? content_type { get; set; }
            public long size_bytes { get; set; }
            public DateTime uploaded_at { get; set; }
            public string? subido_por { get; set; }
            public string? document_type { get; set; }
        }

        /// <summary>Documentos enganchados a una entidad concreta.</summary>
        public List<AdjuntoDTO> ListarDe(string tipoEntidad, int idEntidad)
        {
            var ids = _context.DocumentLinks.AsNoTracking()
                .Where(e => e.entity_type == tipoEntidad && e.entity_id == idEntidad)
                .Select(e => e.document_id)
                .ToList();

            if (ids.Count == 0) return new List<AdjuntoDTO>();

            var documentos = _context.Documents.AsNoTracking()
                .Where(d => ids.Contains(d.id) && d.status == Disponible)
                .OrderByDescending(d => d.uploaded_at)
                .ToList();

            // Sin el filtro de iglesia: quien sube puede ser el administrador de
            // plataforma, cuya fila de usuario está en la iglesia interna de Congrega.
            var usuarios = _context.Usuarios.AsNoTracking().IgnoreQueryFilters()
                .ToDictionary(u => u.ID_usuario, u => u.nombre_usuario ?? "");

            return documentos.Select(d => new AdjuntoDTO
            {
                id = d.id,
                original_filename = d.original_filename,
                content_type = d.content_type,
                size_bytes = d.size_bytes,
                uploaded_at = d.uploaded_at,
                document_type = d.document_type,
                subido_por = usuarios.ContainsKey(d.uploaded_by)
                    ? usuarios[d.uploaded_by] : "#" + d.uploaded_by
            }).ToList();
        }

        /// <summary>Cuántos adjuntos tiene cada entidad de una lista, de una sola consulta.</summary>
        /// <remarks>
        /// Lo usa el listado de gastos para pintar la columna del justificante sin
        /// hacer una consulta por fila.
        /// </remarks>
        public Dictionary<int, int> ContarPorEntidad(string tipoEntidad, List<int> ids)
        {
            if (ids.Count == 0) return new Dictionary<int, int>();

            var disponibles = _context.Documents.AsNoTracking()
                .Where(d => d.status == Disponible).Select(d => d.id);

            return _context.DocumentLinks.AsNoTracking()
                .Where(e => e.entity_type == tipoEntidad
                         && ids.Contains(e.entity_id)
                         && disponibles.Contains(e.document_id))
                .GroupBy(e => e.entity_id)
                .Select(g => new { entidad = g.Key, cuantos = g.Count() })
                .ToDictionary(x => x.entidad, x => x.cuantos);
        }

        public Document? Obtener(int id)
            => _context.Documents.AsNoTracking()
                .FirstOrDefault(d => d.id == id && d.status == Disponible);

        /// <summary>Si ya hay un documento con el mismo contenido en esta iglesia.</summary>
        public Document? PorHuella(string sha256)
            => _context.Documents.AsNoTracking()
                .FirstOrDefault(d => d.sha256_hash == sha256 && d.status == Disponible);

        /// <summary>
        /// Guarda los datos de un fichero ya escrito en disco y lo engancha a su entidad.
        /// </summary>
        /// <remarks>
        /// Las dos cosas van en la misma transacción: un documento sin enlace sería un
        /// fichero huérfano que ocupa sitio y no se ve desde ninguna pantalla.
        /// </remarks>
        public int Registrar(Document documento, string tipoEntidad, int idEntidad,
                             string relacion, out string mensaje)
        {
            mensaje = string.Empty;
            using var transaccion = _context.Database.BeginTransaction();
            try
            {
                documento.uploaded_at = DateTime.UtcNow;
                documento.status = Disponible;
                _context.Documents.Add(documento);
                _context.SaveChanges();   // hace falta el id para el enlace

                _context.DocumentLinks.Add(new DocumentLink
                {
                    document_id = documento.id,
                    entity_type = tipoEntidad,
                    entity_id = idEntidad,
                    relation_type = relacion
                });

                _context.SaveChanges();
                transaccion.Commit();

                mensaje = "Justificante adjuntado.";
                return documento.id;
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                mensaje = "Error al guardar el justificante: " + ErrorHelper.Mensaje(ex);
                return 0;
            }
        }

        /// <summary>
        /// Marca un documento como borrado, sin tocar el fichero del disco.
        /// </summary>
        /// <remarks>
        /// No se borra el fichero a propósito. Si el justificante de un gasto ya
        /// contabilizado desapareciera del disco, la operación quedaría sin respaldo y
        /// eso no se puede deshacer. Se oculta, y queda el rastro de quién lo quitó.
        /// </remarks>
        public bool MarcarBorrado(int id, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var documento = _context.Documents.FirstOrDefault(d => d.id == id);
                if (documento == null)
                {
                    mensaje = "El documento no existe.";
                    return false;
                }

                documento.status = Borrado;
                _context.SaveChanges();

                mensaje = "Justificante quitado.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = "Error al quitar el justificante: " + ErrorHelper.Mensaje(ex);
                return false;
            }
        }
    }
}
