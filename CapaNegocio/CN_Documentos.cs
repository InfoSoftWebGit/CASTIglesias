using CapaDatos;
using CapaEntidad.Financiero;
using System.Security.Cryptography;

namespace CapaNegocio
{
    /// <summary>
    /// Justificantes: qué se admite, dónde se guarda y cómo se recupera.
    /// </summary>
    /// <remarks>
    /// Esta capa decide QUÉ ficheros entran. Es el punto donde una aplicación web se
    /// rompe con más facilidad, así que las reglas están explícitas y no repartidas:
    ///
    ///   - Solo PDF e imágenes. Nada ejecutable, nada de Office con macros.
    ///   - Se comprueba la EXTENSIÓN y el tipo declarado, y además la firma binaria
    ///     del fichero. Renombrar un .exe a .pdf no cuela, porque el contenido no
    ///     empieza por %PDF.
    ///   - El nombre con el que se escribe en disco lo genera el programa, nunca el
    ///     usuario: así ningún nombre puede salirse de la carpeta.
    ///   - Tope de 10 MB, como dice el documento funcional.
    /// </remarks>
    public class CN_Documentos
    {
        private readonly CD_Documentos _cdDocumentos;

        public CN_Documentos(CD_Documentos cdDocumentos) => _cdDocumentos = cdDocumentos;

        /// <summary>Tope por fichero. Una factura escaneada no pasa de aquí.</summary>
        public const long MaximoBytes = 10 * 1024 * 1024;

        /// <summary>Extensiones admitidas, en minúsculas y con punto.</summary>
        public static readonly string[] Extensiones = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>
        /// Primeros bytes que debe tener cada tipo para ser lo que dice ser.
        /// </summary>
        /// <remarks>
        /// Comprobar solo la extensión no sirve de nada: la pone quien sube el fichero.
        /// Esto mira el contenido real, que es lo único que no se puede falsear
        /// simplemente renombrando.
        /// </remarks>
        private static readonly Dictionary<string, byte[][]> Firmas = new()
        {
            [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },                     // %PDF
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } }                     // RIFF
        };

        public List<CD_Documentos.AdjuntoDTO> ListarDe(string tipoEntidad, int idEntidad)
            => _cdDocumentos.ListarDe(tipoEntidad, idEntidad);

        public Dictionary<int, int> ContarPorEntidad(string tipoEntidad, List<int> ids)
            => _cdDocumentos.ContarPorEntidad(tipoEntidad, ids);

        public Document? Obtener(int id) => _cdDocumentos.Obtener(id);

        public bool MarcarBorrado(int id, out string mensaje)
            => _cdDocumentos.MarcarBorrado(id, out mensaje);

        /// <summary>Resultado de intentar adjuntar un fichero.</summary>
        public class ResultadoSubidaDTO
        {
            public bool correcto { get; set; }
            public string mensaje { get; set; } = "";
            public int id { get; set; }
            /// <summary>Nombre con el que hay que escribirlo en disco.</summary>
            public string? clave_almacen { get; set; }
            /// <summary>true si ya existía otro fichero idéntico adjunto en la iglesia.</summary>
            public bool duplicado { get; set; }
        }

        /// <summary>
        /// Comprueba un fichero y devuelve con qué nombre hay que escribirlo.
        /// </summary>
        /// <remarks>
        /// El contenido llega ya en memoria porque hay que leerlo entero igualmente
        /// para calcular su huella y mirar su firma. Con el tope de 10 MB eso no es un
        /// problema de memoria, y evita escribir en disco algo que luego se rechaza.
        /// </remarks>
        public ResultadoSubidaDTO Comprobar(byte[] contenido, string nombreOriginal)
        {
            var r = new ResultadoSubidaDTO();

            if (contenido == null || contenido.Length == 0)
            {
                r.mensaje = "El fichero está vacío.";
                return r;
            }

            if (contenido.Length > MaximoBytes)
            {
                r.mensaje = $"El fichero pasa de {MaximoBytes / (1024 * 1024)} MB.";
                return r;
            }

            string extension = Path.GetExtension(nombreOriginal ?? "").ToLowerInvariant();
            if (!Extensiones.Contains(extension))
            {
                r.mensaje = "Solo se admiten PDF e imágenes (JPG, PNG o WEBP).";
                return r;
            }

            if (!FirmaCorrecta(contenido, extension))
            {
                r.mensaje = "El contenido del fichero no se corresponde con su extensión. " +
                            "Vuelve a guardarlo con el programa que lo creó y súbelo de nuevo.";
                return r;
            }

            // Huella del contenido: identifica el fichero con independencia de su nombre
            string huella = Convert.ToHexString(SHA256.HashData(contenido)).ToLowerInvariant();

            var existente = _cdDocumentos.PorHuella(huella);
            r.duplicado = existente != null;

            // El nombre en disco lo pone el programa. La extensión se conserva para
            // que el navegador sepa qué hacer al descargarlo.
            r.clave_almacen = $"{DateTime.UtcNow:yyyyMM}/{Guid.NewGuid():N}{extension}";
            r.correcto = true;
            r.mensaje = huella;   // el controlador lo necesita para guardarlo
            return r;
        }

        private static bool FirmaCorrecta(byte[] contenido, string extension)
        {
            if (!Firmas.TryGetValue(extension, out var posibles)) return false;

            foreach (var firma in posibles)
            {
                if (contenido.Length < firma.Length) continue;
                bool coincide = true;
                for (int i = 0; i < firma.Length; i++)
                {
                    if (contenido[i] != firma[i]) { coincide = false; break; }
                }
                if (coincide) return true;
            }
            return false;
        }

        /// <summary>Guarda los datos del fichero y lo engancha a su operación.</summary>
        public int Registrar(Document documento, string tipoEntidad, int idEntidad,
                             out string mensaje)
            => _cdDocumentos.Registrar(documento, tipoEntidad, idEntidad, "justificante", out mensaje);

        /// <summary>Tipo MIME que se declara al descargar, según la extensión.</summary>
        /// <remarks>
        /// Se deduce de la extensión y NO se usa el que envió el navegador al subir:
        /// ese lo controla quien sube, y devolverlo tal cual permitiría que un fichero
        /// se sirviera como HTML y se ejecutara en el navegador de quien lo abre.
        /// </remarks>
        public static string TipoMime(string? nombre) => Path.GetExtension(nombre ?? "").ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
