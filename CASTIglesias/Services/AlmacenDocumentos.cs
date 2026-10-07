namespace CASTIglesias.Services
{
    /// <summary>
    /// Dónde se guardan físicamente los justificantes.
    /// </summary>
    /// <remarks>
    /// La carpeta se configura en appsettings ("Documentos:Carpeta"). Si no se indica
    /// nada, se usa una carpeta "DocumentosFinancieros" HERMANA de la aplicación, no
    /// dentro de ella.
    ///
    /// Que esté fuera de wwwroot es el punto importante y no es una preferencia: todo
    /// lo que vive en wwwroot lo sirve el servidor a quien pida la URL, sin pasar por
    /// la sesión ni por los permisos. Una factura puede llevar el NIF de un proveedor
    /// o el nombre de una familia a la que se ayudó.
    ///
    /// Que sea hermana y no interna evita además que un despliegue que borra y copia
    /// la carpeta de la aplicación se lleve por delante los justificantes de años.
    /// </remarks>
    public class AlmacenDocumentos
    {
        private readonly string _raiz;

        public AlmacenDocumentos(IConfiguration configuracion, IWebHostEnvironment entorno)
        {
            string? configurada = configuracion["Documentos:Carpeta"];

            _raiz = string.IsNullOrWhiteSpace(configurada)
                ? Path.Combine(Directory.GetParent(entorno.ContentRootPath)?.FullName
                               ?? entorno.ContentRootPath, "DocumentosFinancieros")
                : configurada;
        }

        /// <summary>Ruta completa de un documento a partir de su clave de almacén.</summary>
        /// <remarks>
        /// Comprueba que la ruta resultante siga dentro de la carpeta raíz. La clave la
        /// genera el programa, así que no debería poder salirse, pero si algún día un
        /// valor manipulado llegara hasta aquí, esto lo corta: es el fallo clásico que
        /// permite leer o escribir cualquier fichero del servidor.
        /// </remarks>
        public string RutaDe(string claveAlmacen)
        {
            string completa = Path.GetFullPath(Path.Combine(_raiz, claveAlmacen));
            string raizCompleta = Path.GetFullPath(_raiz);

            if (!completa.StartsWith(raizCompleta, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ruta de documento fuera de la carpeta permitida.");

            return completa;
        }

        /// <summary>Escribe el fichero, creando las carpetas que hagan falta.</summary>
        public async Task GuardarAsync(string claveAlmacen, byte[] contenido)
        {
            string ruta = RutaDe(claveAlmacen);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            await File.WriteAllBytesAsync(ruta, contenido);
        }

        public bool Existe(string claveAlmacen)
        {
            try { return File.Exists(RutaDe(claveAlmacen)); }
            catch (InvalidOperationException) { return false; }
        }

        public async Task<byte[]> LeerAsync(string claveAlmacen)
            => await File.ReadAllBytesAsync(RutaDe(claveAlmacen));
    }
}
