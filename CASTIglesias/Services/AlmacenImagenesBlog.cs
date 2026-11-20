namespace CASTIglesias.Services
{
    /// <summary>
    /// Dónde se guardan las imágenes que se suben para el blog.
    /// </summary>
    /// <remarks>
    /// Mismo criterio que <see cref="AlmacenDocumentos"/>: la carpeta se configura en
    /// appsettings ("Blog:CarpetaImagenes") y, si no se indica, es una carpeta
    /// "BlogImagenes" HERMANA de la aplicación. Si viviera dentro de wwwroot, un
    /// despliegue que borra y copia la aplicación se llevaría todas las imágenes
    /// publicadas. Se sirven a través de BlogController.Imagen.
    /// </remarks>
    public class AlmacenImagenesBlog
    {
        /// <summary>Tamaño máximo de una imagen subida.</summary>
        public const long TamanoMaximo = 5 * 1024 * 1024;

        private static readonly Dictionary<string, string> TiposPorExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"]  = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"]  = "image/png",
            [".gif"]  = "image/gif",
            [".webp"] = "image/webp"
        };

        private readonly string _raiz;

        public AlmacenImagenesBlog(IConfiguration configuracion, IWebHostEnvironment entorno)
        {
            string? configurada = configuracion["Blog:CarpetaImagenes"];

            _raiz = string.IsNullOrWhiteSpace(configurada)
                ? Path.Combine(Directory.GetParent(entorno.ContentRootPath)?.FullName
                               ?? entorno.ContentRootPath, "BlogImagenes")
                : configurada;
        }

        /// <summary>
        /// Comprueba que el fichero sea de verdad una imagen y lo guarda con un
        /// nombre nuevo.
        /// </summary>
        /// <returns>El nombre con el que quedó guardado, o null si se rechaza.</returns>
        public async Task<string?> GuardarAsync(IFormFile fichero)
        {
            string extension = Path.GetExtension(fichero.FileName);
            if (!TiposPorExtension.ContainsKey(extension) || fichero.Length == 0 || fichero.Length > TamanoMaximo)
                return null;

            using var memoria = new MemoryStream();
            await fichero.CopyToAsync(memoria);
            byte[] bytes = memoria.ToArray();

            // La extensión la elige quien sube el fichero; la cabecera binaria no.
            if (!EsImagen(bytes)) return null;

            // Nombre generado: nunca se usa el original, que podría traer rutas
            // ("../../web.config") o pisar otra imagen.
            string nombre = $"{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            Directory.CreateDirectory(_raiz);
            await File.WriteAllBytesAsync(Path.Combine(_raiz, nombre), bytes);
            return nombre;
        }

        /// <summary>Ruta física de una imagen, o null si el nombre no es válido o no existe.</summary>
        public string? RutaDe(string nombre)
        {
            // Solo nombres planos con extensión de imagen: nada de carpetas.
            if (string.IsNullOrWhiteSpace(nombre) || nombre != Path.GetFileName(nombre)
                || !TiposPorExtension.ContainsKey(Path.GetExtension(nombre)))
                return null;

            string ruta = Path.GetFullPath(Path.Combine(_raiz, nombre));
            if (!ruta.StartsWith(Path.GetFullPath(_raiz), StringComparison.OrdinalIgnoreCase)) return null;
            return File.Exists(ruta) ? ruta : null;
        }

        public static string TipoDe(string nombre) =>
            TiposPorExtension.TryGetValue(Path.GetExtension(nombre), out var tipo) ? tipo : "application/octet-stream";

        private static bool EsImagen(byte[] b)
        {
            if (b.Length < 12) return false;
            bool jpg  = b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;
            bool png  = b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;
            bool gif  = b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38;
            bool webp = b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
                     && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50;
            return jpg || png || gif || webp;
        }
    }
}
