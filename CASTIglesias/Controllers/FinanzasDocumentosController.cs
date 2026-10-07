using CapaDatos;
using CapaEntidad.Financiero;
using CapaNegocio;
using CASTIglesias.Filters;
using CASTIglesias.Models;
using CASTIglesias.Services;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Justificantes adjuntos: subirlos, listarlos, descargarlos y quitarlos.
    /// </summary>
    /// <remarks>
    /// Los ficheros NO están en wwwroot: se sirven desde aquí, después de comprobar la
    /// sesión, el módulo y el permiso. Ver AlmacenDocumentos para el porqué.
    ///
    /// Cada descarga deja rastro en la auditoría. El documento funcional lo pide
    /// expresamente ("descargas de documentos sensibles"), y tiene sentido: una
    /// factura o un justificante de una ayuda a una familia no es un dato más.
    /// </remarks>
    public class FinanzasDocumentosController : AreaFinancieraController
    {
        private readonly CN_Documentos _negocioDocumentos;
        private readonly AlmacenDocumentos _almacen;
        private readonly CD_Asientos _cdAsientos;

        public FinanzasDocumentosController(CN_Sedes negocioSedes, CN_Permisos negocioPermisos,
                                            CN_Plataforma negocioPlataforma,
                                            CN_Documentos negocioDocumentos,
                                            AlmacenDocumentos almacen,
                                            CD_Asientos cdAsientos)
            : base(negocioSedes, negocioPermisos, negocioPlataforma)
        {
            _negocioDocumentos = negocioDocumentos;
            _almacen = almacen;
            _cdAsientos = cdAsientos;
        }

        /// <summary>Adjuntos de una operación.</summary>
        [HttpGet]
        public JsonResult Listar(string tipoEntidad, int idEntidad)
            => Json(new { data = _negocioDocumentos.ListarDe(tipoEntidad, idEntidad) });

        /// <summary>
        /// Sube un justificante y lo engancha a su operación.
        /// </summary>
        /// <remarks>
        /// El orden importa: primero se comprueba el fichero, después se escribe en
        /// disco y por último se guarda la fila. Si se guardara la fila antes de
        /// escribir, un fallo de disco dejaría un justificante que la pantalla enseña
        /// y que no se puede descargar.
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasOperacionesCrearEditar))]
        public async Task<JsonResult> Subir(IFormFile fichero, string tipoEntidad, int idEntidad)
        {
            if (fichero == null || fichero.Length == 0)
                return Json(new { resultado = false, mensaje = "No has elegido ningún fichero." });

            // El tope se comprueba también aquí, antes de leer nada en memoria
            if (fichero.Length > CN_Documentos.MaximoBytes)
                return Json(new
                {
                    resultado = false,
                    mensaje = $"El fichero pasa de {CN_Documentos.MaximoBytes / (1024 * 1024)} MB."
                });

            byte[] contenido;
            using (var memoria = new MemoryStream())
            {
                await fichero.CopyToAsync(memoria);
                contenido = memoria.ToArray();
            }

            var comprobacion = _negocioDocumentos.Comprobar(contenido, fichero.FileName);
            if (!comprobacion.correcto)
                return Json(new { resultado = false, mensaje = comprobacion.mensaje });

            // Comprobar() devuelve la huella en 'mensaje' cuando todo va bien
            string huella = comprobacion.mensaje;

            try
            {
                await _almacen.GuardarAsync(comprobacion.clave_almacen!, contenido);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    resultado = false,
                    mensaje = "No se ha podido guardar el fichero en el servidor: " + ex.Message
                });
            }

            var documento = new Document
            {
                site_id = ObtenerIdSedeUsuario() == CapaEntidad.Sedes.TodasLasSedes
                    ? null : ObtenerIdSedeUsuario(),
                storage_key = comprobacion.clave_almacen,
                original_filename = fichero.FileName,
                safe_filename = Path.GetFileName(comprobacion.clave_almacen),
                content_type = CN_Documentos.TipoMime(fichero.FileName),
                size_bytes = contenido.LongLength,
                sha256_hash = huella,
                document_type = "justificante",
                classification = "internal",
                uploaded_by = SesionClaims.ObtenerIdUsuario(User)
            };

            int id = _negocioDocumentos.Registrar(documento, tipoEntidad, idEntidad, out string mensaje);

            return Json(new
            {
                resultado = id > 0,
                id,
                mensaje,
                // Se avisa, no se bloquea: subir dos veces la misma factura puede ser
                // un error o puede ser legítimo (una factura que cubre dos gastos).
                duplicado = comprobacion.duplicado
            });
        }

        /// <summary>
        /// Devuelve el fichero. Es la ÚNICA forma de llegar a él.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Descargar(int id)
        {
            var documento = _negocioDocumentos.Obtener(id);
            // El filtro global por iglesia hace que el documento de otra iglesia
            // simplemente "no exista", así que esto cubre también ese caso.
            if (documento == null || string.IsNullOrWhiteSpace(documento.storage_key))
                return NotFound();

            if (!_almacen.Existe(documento.storage_key))
                return NotFound();

            // El rastro se escribe ANTES de entregar el fichero: si se escribiera
            // después, una descarga interrumpida no dejaría constancia.
            _cdAsientos.RegistrarAuditoriaYGuardar(
                SesionClaims.ObtenerIdIglesia(User), documento.site_id,
                SesionClaims.ObtenerIdUsuario(User),
                "document.downloaded", "documents", documento.id, "download",
                $"{{\"fichero\":\"{documento.original_filename}\"}}");

            byte[] contenido = await _almacen.LeerAsync(documento.storage_key);

            // El tipo se deduce de la extensión, no se usa el que envió el navegador al
            // subir: devolver aquel tal cual permitiría servir un fichero como HTML y
            // que se ejecutara en el navegador de quien lo abre.
            return File(contenido,
                        CN_Documentos.TipoMime(documento.original_filename),
                        documento.original_filename);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(nameof(CapaEntidad.Permisos.FinanzasOperacionesCrearEditar))]
        public JsonResult Quitar(int id)
        {
            bool correcto = _negocioDocumentos.MarcarBorrado(id, out string mensaje);
            return Json(new { resultado = correcto, mensaje });
        }
    }
}
