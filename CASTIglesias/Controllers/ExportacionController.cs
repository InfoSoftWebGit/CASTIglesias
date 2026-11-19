using CapaEntidad;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace CASTIglesias.Controllers
{
    /// <summary>
    /// Descarga en Excel de cualquier listado de la aplicación.
    /// </summary>
    /// <remarks>
    /// Es un volcado genérico a propósito. Cada pantalla tiene su propia consulta,
    /// sus columnas y sus formatos (badges, fechas, sedes), de modo que un endpoint
    /// por listado habría obligado a repetir en C# lo que la vista ya hace en
    /// JavaScript. Aquí llega la tabla tal cual la ve el usuario (ver
    /// <see cref="ExportacionTablaDTO"/>) y este controlador solo le da forma de
    /// hoja de cálculo.
    ///
    /// El PDF no pasa por aquí: se hace imprimiendo desde el navegador ("Guardar
    /// como PDF"), igual que en los informes financieros, para no arrastrar una
    /// librería de PDF al proyecto.
    /// </remarks>
    [Authorize]
    public class ExportacionController : Controller
    {
        // Paleta del producto, la misma que usan las cabeceras de las pantallas.
        private const string AzulCabecera = "#1E3A8A";

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Excel([FromBody] ExportacionTablaDTO datos)
        {
            if (datos == null || datos.Columnas.Count == 0 || datos.Filas.Count == 0)
                return BadRequest("No hay datos que exportar.");

            if (datos.Columnas.Count > ExportacionTablaDTO.LimiteColumnas)
                return BadRequest("Demasiadas columnas en la exportación.");

            if (datos.Filas.Count > ExportacionTablaDTO.LimiteFilas)
                return BadRequest($"La descarga está limitada a {ExportacionTablaDTO.LimiteFilas} filas.");

            string titulo = string.IsNullOrWhiteSpace(datos.Titulo) ? "Listado" : datos.Titulo.Trim();

            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add(NombreHojaValido(titulo));

            int totalColumnas = datos.Columnas.Count;
            int fila = 1;

            // ── Título ─────────────────────────────────────────────────────────
            hoja.Cell(fila, 1).Value = titulo;
            hoja.Range(fila, 1, fila, totalColumnas).Merge();
            hoja.Cell(fila, 1).Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Font.SetFontColor(XLColor.FromHtml(AzulCabecera));
            fila++;

            if (!string.IsNullOrWhiteSpace(datos.Subtitulo))
            {
                hoja.Cell(fila, 1).Value = datos.Subtitulo;
                hoja.Range(fila, 1, fila, totalColumnas).Merge();
                hoja.Cell(fila, 1).Style.Font.SetItalic(true).Font.SetFontSize(10)
                    .Font.SetFontColor(XLColor.FromHtml("#64748b"));
                fila++;
            }

            fila++; // Una fila en blanco entre el título y la tabla

            // ── Cabeceras ──────────────────────────────────────────────────────
            int filaCabecera = fila;
            for (int c = 0; c < totalColumnas; c++)
            {
                hoja.Cell(filaCabecera, c + 1).Value = datos.Columnas[c] ?? string.Empty;
            }
            hoja.Range(filaCabecera, 1, filaCabecera, totalColumnas).Style
                .Font.SetBold(true)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml(AzulCabecera))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            fila++;

            // ── Filas ──────────────────────────────────────────────────────────
            foreach (var registro in datos.Filas)
            {
                for (int c = 0; c < totalColumnas; c++)
                {
                    // Una fila manipulada podría traer más (o menos) celdas que
                    // columnas: se recorre por columnas y lo que sobra se ignora.
                    string valor = c < registro.Count ? registro[c] ?? string.Empty : string.Empty;
                    EscribirCelda(hoja.Cell(fila, c + 1), valor);
                }
                fila++;
            }

            int ultimaFila = fila - 1;
            hoja.Range(filaCabecera, 1, ultimaFila, totalColumnas).Style
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Hair)
                .Border.SetInsideBorderColor(XLColor.FromHtml("#cbd5e1"));

            // ── Pie ────────────────────────────────────────────────────────────
            hoja.Cell(ultimaFila + 2, 1).Value = $"Generado con Congrega CRM — {DateTime.Now:dd/MM/yyyy HH:mm}";
            hoja.Range(ultimaFila + 2, 1, ultimaFila + 2, totalColumnas).Merge();
            hoja.Cell(ultimaFila + 2, 1).Style
                .Font.SetItalic(true).Font.SetFontSize(9)
                .Font.SetFontColor(XLColor.FromHtml("#94a3b8"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            hoja.SheetView.FreezeRows(filaCabecera);
            hoja.Columns().AdjustToContents(5.0, 60.0);

            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);

            string nombreFichero = $"{NombreFicheroValido(titulo)}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(memoria.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreFichero);
        }

        /// <summary>
        /// Escribe una celda respetando el dato que llega de la pantalla.
        /// </summary>
        /// <remarks>
        /// Todo llega como texto, porque es lo que se ve en la tabla. Solo se
        /// convierte a número lo que es un entero sin ceros a la izquierda: así la
        /// edad o el número de hijos se pueden sumar en Excel, mientras que los
        /// códigos postales ("08001"), los números de miembro con prefijo o los
        /// teléfonos con espacios se quedan como texto y no pierden nada.
        ///
        /// SetValue(string) guarda siempre texto: no interpreta como fórmula un
        /// valor que empiece por "=", que es lo que hay que evitar cuando el
        /// contenido viene del navegador.
        /// </remarks>
        private static void EscribirCelda(IXLCell celda, string valor)
        {
            if (valor.Length > 0 && valor.Length <= 9 && valor[0] != '0'
                && int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out int numero))
            {
                celda.SetValue(numero);
                return;
            }

            celda.SetValue(valor);
        }

        /// <summary>
        /// Nombre de hoja admitido por Excel: 31 caracteres como máximo y sin
        /// : \ / ? * [ ]. Un nombre inválido hace que el fichero no se abra.
        /// </summary>
        private static string NombreHojaValido(string titulo)
        {
            var limpio = new StringBuilder();
            foreach (char c in titulo)
            {
                limpio.Append(":\\/?*[]".Contains(c) ? ' ' : c);
            }

            string nombre = limpio.ToString().Trim();
            if (nombre.Length == 0) nombre = "Listado";
            return nombre.Length > 31 ? nombre[..31] : nombre;
        }

        /// <summary>
        /// Nombre de fichero sin caracteres problemáticos. El título lo manda la
        /// vista, así que no se usa tal cual en la cabecera Content-Disposition.
        /// </summary>
        private static string NombreFicheroValido(string titulo)
        {
            var invalidos = Path.GetInvalidFileNameChars();
            var limpio = new StringBuilder();
            foreach (char c in titulo)
            {
                limpio.Append(invalidos.Contains(c) || c == ' ' ? '_' : c);
            }

            string nombre = limpio.ToString().Trim('_');
            if (nombre.Length == 0) nombre = "Listado";
            return nombre.Length > 60 ? nombre[..60] : nombre;
        }
    }
}
