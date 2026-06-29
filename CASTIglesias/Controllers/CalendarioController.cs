using CapaEntidad;
using CapaNegocio;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CASTIglesias.Controllers
{
    [Authorize]
    public class CalendarioController : BaseController
    {
        private readonly CN_Calendario _cnCalendario;
        private readonly CN_Culto _cnCulto;

        public CalendarioController(
            CN_Calendario cnCalendario,
            CN_Culto cnCulto,
            CN_Sedes cnSedes,
            CN_Permisos cnPermisos) : base(cnSedes, cnPermisos)
        {
            _cnCalendario = cnCalendario;
            _cnCulto = cnCulto;
        }

        public IActionResult Calendarios() => View();

        [HttpGet]
        public JsonResult ObtenerCultos()
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                var cultos = _cnCulto.Listar(sedeId)
                    .Select(c => new { c.id_culto, c.nombre, c.dia_semana })
                    .ToList();
                return Json(new { success = true, data = cultos });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        public JsonResult GenerarCalendario([FromBody] CalendarioRequest req)
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                var cal = _cnCalendario.Generar(req, sedeId, out string error);
                if (!string.IsNullOrEmpty(error))
                    return Json(new { success = false, mensaje = error });
                return Json(new { success = true, data = cal });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        public IActionResult ExportarExcelAgrupado([FromBody] CalendarioAgrupadoRequest req)
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                var calendarios = new List<CalendarioServicioDTO>();
                foreach (int id in req.IdsCultos)
                {
                    var single = new CalendarioRequest
                    {
                        IdCulto        = id,
                        Periodicidad   = req.Periodicidad,
                        TipoCalendario = req.TipoCalendario,
                        FechaInicio    = req.FechaInicio
                    };
                    var cal = _cnCalendario.Generar(single, sedeId, out string err);
                    if (string.IsNullOrEmpty(err) && cal.Entradas.Any())
                        calendarios.Add(cal);
                }
                if (!calendarios.Any())
                    return Json(new { success = false, mensaje = "No se pudieron generar calendarios para los cultos indicados." });

                return ExcelAgrupado(calendarios, req);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        [HttpPost]
        public IActionResult ExportarExcel([FromBody] CalendarioRequest req)
        {
            try
            {
                int sedeId = ObtenerIdSedeUsuario();
                var cal = _cnCalendario.Generar(req, sedeId, out string error);
                if (!string.IsNullOrEmpty(error))
                    return Json(new { success = false, mensaje = error });

                return req.TipoCalendario == CN_Calendario.TIPO_ALABANZA
                    ? ExcelAlabanza(cal, req)
                    : ExcelEstandar(cal, req);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ErrorHelper.Mensaje(ex) });
            }
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Excel estándar (Seguridad y Bienvenida / Audiovisuales): filas=fechas, cols=roles
        // ────────────────────────────────────────────────────────────────────────────
        private IActionResult ExcelEstandar(CalendarioServicioDTO cal, CalendarioRequest req)
        {
            var cultura   = new System.Globalization.CultureInfo("es-ES");
            string tipo   = TipoLabel(req.TipoCalendario);
            string perLabel = PeriodoLabel(req.Periodicidad);
            int totalCols = 1 + cal.Roles.Count; // Fecha + roles (sin columna Día)

            string tituloTrimestre = req.Periodicidad.ToLowerInvariant() == "trimestral"
                ? DeterminarTrimestre(cal.FechaInicio)
                : perLabel + " " + cal.FechaInicio.ToString("MMMM yyyy", cultura);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Calendario");

            // Fila 1 – Título
            ws.Row(1).Height = 34;
            ws.Cell(1, 1).Value = $"CALENDARIO DE {tipo.ToUpperInvariant()} — {tituloTrimestre}";
            ws.Range(1, 1, 1, totalCols).Merge();
            ws.Cell(1, 1).Style
                .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Fila 2 – Nombre del culto
            ws.Row(2).Height = 22;
            ws.Cell(2, 1).Value = cal.NombreCulto.ToUpperInvariant();
            ws.Range(2, 1, 2, totalCols).Merge();
            ws.Cell(2, 1).Style
                .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Fila 3 – Período
            ws.Row(3).Height = 18;
            ws.Cell(3, 1).Value = $"Período: {cal.FechaInicio:dd/MM/yyyy} — {cal.FechaFin:dd/MM/yyyy}   |   {perLabel}";
            ws.Range(3, 1, 3, totalCols).Merge();
            ws.Cell(3, 1).Style
                .Font.SetFontName("Aptos Narrow").Font.SetItalic(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#475569"))
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f1f5f9"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Fila 4 – Cabecera columnas
            ws.Row(4).Height = 26;
            ws.Cell(4, 1).Value = "Fecha";
            for (int i = 0; i < cal.Roles.Count; i++)
                ws.Cell(4, 2 + i).Value = cal.Roles[i];
            ws.Range(4, 1, 4, totalCols).Style
                .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Filas de datos
            for (int r = 0; r < cal.Entradas.Count; r++)
            {
                var entrada = cal.Entradas[r];
                int row = 5 + r;
                bool multiPersona = entrada.Asignaciones.Any(kv => kv.Value?.Count > 1);
                ws.Row(row).Height = multiPersona ? 34 : 22;
                var bg = r % 2 == 0 ? XLColor.White : XLColor.FromHtml("#f0f4ff");

                void SetCell(int col, string val, bool bold = false)
                {
                    var c = ws.Cell(row, col);
                    c.Value = val;
                    c.Style.Font.SetFontName("Aptos Narrow").Font.SetFontSize(11).Font.SetBold(bold)
                            .Fill.SetBackgroundColor(bg)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                            .Alignment.SetWrapText(true);
                    if (val.Contains("Sin asignar")) c.Style.Font.SetFontColor(XLColor.FromHtml("#dc2626"));
                }

                string fechaStr = entrada.Fecha.ToString("dd-MMM", cultura).ToLower();
                SetCell(1, fechaStr, true);
                for (int ci = 0; ci < cal.Roles.Count; ci++)
                {
                    string texto = entrada.Asignaciones.TryGetValue(cal.Roles[ci], out var m)
                        ? string.Join("\n", m) : "";
                    SetCell(2 + ci, texto);
                }
            }

            AplicarEstiloFinal(ws, 4, 4 + cal.Entradas.Count, totalCols);
            ws.SheetView.Freeze(4, 0); // headers siempre visibles al hacer scroll
            ws.Column(1).Width = 11;
            for (int i = 0; i < cal.Roles.Count; i++)
                ws.Column(2 + i).Width = Math.Max(18, cal.Roles[i].Length + 5);

            return GenerarArchivoExcel(wb, $"Calendario_{tipo}_{cal.NombreCulto}");
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Excel Alabanza: filas=roles/instrumentos, columnas=fechas (transpuesto)
        // ────────────────────────────────────────────────────────────────────────────
        private IActionResult ExcelAlabanza(CalendarioServicioDTO cal, CalendarioRequest req)
        {
            var cultura   = new System.Globalization.CultureInfo("es-ES");
            string perLabel = PeriodoLabel(req.Periodicidad);
            int totalCols = 1 + cal.Entradas.Count;

            string tituloTrimestre = req.Periodicidad.ToLowerInvariant() == "trimestral"
                ? DeterminarTrimestre(cal.FechaInicio)
                : perLabel + " " + cal.FechaInicio.ToString("MMMM yyyy", cultura);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Alabanza");

            // Fila 1 – Título
            ws.Row(1).Height = 34;
            ws.Cell(1, 1).Value = $"CRONOGRAMA DE ALABANZA — {tituloTrimestre}";
            ws.Range(1, 1, 1, totalCols).Merge();
            ws.Cell(1, 1).Style
                .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Fila 2 – Nombre del culto
            ws.Row(2).Height = 22;
            ws.Cell(2, 1).Value = cal.NombreCulto.ToUpperInvariant();
            ws.Range(2, 1, 2, totalCols).Merge();
            ws.Cell(2, 1).Style
                .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Fila 3 – Período
            ws.Row(3).Height = 18;
            ws.Cell(3, 1).Value = $"Período: {cal.FechaInicio:dd/MM/yyyy} — {cal.FechaFin:dd/MM/yyyy}   |   {perLabel}";
            ws.Range(3, 1, 3, totalCols).Merge();
            ws.Cell(3, 1).Style
                .Font.SetFontName("Aptos Narrow").Font.SetItalic(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#475569"))
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f1f5f9"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Fila 4 – Cabecera: "Rol / Instrumento" + fechas
            ws.Row(4).Height = 30;
            ws.Cell(4, 1).Value = "Rol / Instrumento";
            ws.Cell(4, 1).Style
                .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            for (int fi = 0; fi < cal.Entradas.Count; fi++)
            {
                var entrada = cal.Entradas[fi];
                string diaAb = entrada.Fecha.ToString("ddd", cultura);
                diaAb = char.ToUpper(diaAb[0]) + diaAb[1..];
                ws.Cell(4, 2 + fi).Value = $"{entrada.Fecha.ToString("dd-MMM", cultura).ToLower()}\n{diaAb}";
                ws.Cell(4, 2 + fi).Style
                    .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetWrapText(true);
            }

            // Filas de roles
            for (int ri = 0; ri < cal.Roles.Count; ri++)
            {
                var rol = cal.Roles[ri];
                int row = 5 + ri;
                ws.Row(row).Height = 22;
                bool esMinistra = rol.Contains("ministra", StringComparison.OrdinalIgnoreCase);
                bool esZona     = rol == "Zona Responsable";
                var bg = ri % 2 == 0 ? XLColor.White : XLColor.FromHtml("#f0f4ff");
                var bgResaltado = esMinistra ? XLColor.FromHtml("#dbeafe")
                                : esZona     ? XLColor.FromHtml("#f0fdf4")
                                : bg;

                var cRol = ws.Cell(row, 1);
                cRol.Value = rol;
                cRol.Style
                    .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11)
                    .Font.SetFontColor(esMinistra ? XLColor.FromHtml("#1E3A8A")
                                     : esZona     ? XLColor.FromHtml("#166534")
                                     : XLColor.FromHtml("#1e293b"))
                    .Fill.SetBackgroundColor(bgResaltado)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetIndent(1);

                for (int fi = 0; fi < cal.Entradas.Count; fi++)
                {
                    var entrada = cal.Entradas[fi];
                    string texto = entrada.Asignaciones.TryGetValue(rol, out var ms)
                        ? string.Join("\n", ms) : "";

                    var cell = ws.Cell(row, 2 + fi);
                    cell.Value = texto;
                    cell.Style
                        .Font.SetFontName("Aptos Narrow").Font.SetFontSize(11)
                        .Fill.SetBackgroundColor(esMinistra ? XLColor.FromHtml("#eff6ff")
                                                : esZona    ? XLColor.FromHtml("#f0fdf4")
                                                : bg)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                        .Alignment.SetWrapText(true);

                    if (texto == "—" || texto.Contains("Sin asignar"))
                        cell.Style.Font.SetFontColor(XLColor.FromHtml("#94a3b8")).Font.SetItalic(true);
                }
            }

            AplicarEstiloFinal(ws, 4, 4 + cal.Roles.Count, totalCols);
            ws.SheetView.Freeze(4, 1); // freeze título + cabecera, y columna de roles fija
            ws.Column(1).Width = 26;
            for (int fi = 0; fi < cal.Entradas.Count; fi++)
                ws.Column(2 + fi).Width = 16;

            return GenerarArchivoExcel(wb, $"Calendario_Alabanza_{cal.NombreCulto}");
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Excel Agrupado: varios cultos en columnas paralelas (estilo "audiovisuales")
        // ────────────────────────────────────────────────────────────────────────────
        private IActionResult ExcelAgrupado(List<CalendarioServicioDTO> calendarios, CalendarioAgrupadoRequest req)
        {
            var cultura    = new System.Globalization.CultureInfo("es-ES");
            string tipo    = TipoLabel(req.TipoCalendario);
            string perLabel = PeriodoLabel(req.Periodicidad);

            // ── Layout de columnas: [Fecha + roles] por culto, 1 col de separación entre ellos
            var colStarts = new List<int>();
            int col = 1;
            for (int ci = 0; ci < calendarios.Count; ci++)
            {
                colStarts.Add(col);
                col += 1 + calendarios[ci].Roles.Count; // Fecha + roles
                if (ci < calendarios.Count - 1) col++;  // separador
            }
            int totalCols   = col - 1;
            int maxEntradas = calendarios.Max(c => c.Entradas.Count);

            // Período global
            var allFechas = calendarios.SelectMany(c => c.Entradas.Select(e => e.Fecha)).ToList();
            var pInicio   = allFechas.Min();
            var pFin      = allFechas.Max();
            string tituloTriestre = req.Periodicidad.ToLowerInvariant() == "trimestral"
                ? DeterminarTrimestre(pInicio)
                : perLabel + " " + pInicio.ToString("MMMM yyyy", cultura);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Calendario");

            // Fila 1 – Título principal
            ws.Row(1).Height = 34;
            ws.Cell(1, 1).Value = $"CALENDARIO DE {tipo.ToUpperInvariant()} — {tituloTriestre}";
            ws.Range(1, 1, 1, totalCols).Merge();
            ws.Cell(1, 1).Style
                .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Fila 2 – Subtítulo período
            ws.Row(2).Height = 18;
            ws.Cell(2, 1).Value = $"Período: {pInicio:dd/MM/yyyy} — {pFin:dd/MM/yyyy}   |   {perLabel}";
            ws.Range(2, 1, 2, totalCols).Merge();
            ws.Cell(2, 1).Style
                .Font.SetFontName("Aptos Narrow").Font.SetItalic(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Fila 3 – Separador vacío
            ws.Row(3).Height = 8;

            const int HDR = 4; // fila cabecera sección
            const int COL = 5; // fila cabecera columnas
            const int DAT = 6; // primera fila de datos

            ws.Row(HDR).Height = 26;
            ws.Row(COL).Height = 24;

            for (int ci = 0; ci < calendarios.Count; ci++)
            {
                var cal = calendarios[ci];
                int cs  = colStarts[ci];              // columna inicio
                int ce  = cs + cal.Roles.Count;       // columna fin (Fecha=cs, roles=cs+1..ce)

                // ── Cabecera sección (nombre culto)
                ws.Cell(HDR, cs).Value = cal.NombreCulto.ToUpperInvariant();
                ws.Range(HDR, cs, HDR, ce).Merge();
                ws.Cell(HDR, cs).Style
                    .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                // ── Cabecera columnas
                ws.Cell(COL, cs).Value = "Fecha";
                ws.Cell(COL, cs).Style
                    .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2563EB"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                for (int ri = 0; ri < cal.Roles.Count; ri++)
                {
                    ws.Cell(COL, cs + 1 + ri).Value = cal.Roles[ri];
                    ws.Cell(COL, cs + 1 + ri).Style
                        .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#2563EB"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                }

                // ── Filas de datos
                for (int ei = 0; ei < cal.Entradas.Count; ei++)
                {
                    var entrada = cal.Entradas[ei];
                    int row     = DAT + ei;
                    bool multi  = entrada.Asignaciones.Any(kv => kv.Value?.Count > 1);
                    ws.Row(row).Height = multi ? 34 : 22;
                    var bg = ei % 2 == 0 ? XLColor.White : XLColor.FromHtml("#f0f4ff");

                    string fechaStr = entrada.Fecha.ToString("dd-MMM", cultura).ToLower();
                    ws.Cell(row, cs).Value = fechaStr;
                    ws.Cell(row, cs).Style
                        .Font.SetFontName("Aptos Narrow").Font.SetBold(true).Font.SetFontSize(11)
                        .Fill.SetBackgroundColor(bg)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                    for (int ri = 0; ri < cal.Roles.Count; ri++)
                    {
                        string texto = entrada.Asignaciones.TryGetValue(cal.Roles[ri], out var ms) && ms.Any()
                            ? string.Join("\n", ms) : "";
                        var cell = ws.Cell(row, cs + 1 + ri);
                        cell.Value = texto;
                        cell.Style
                            .Font.SetFontName("Aptos Narrow").Font.SetFontSize(11)
                            .Fill.SetBackgroundColor(bg)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                            .Alignment.SetWrapText(true);
                        if (texto.Contains("Sin asignar"))
                            cell.Style.Font.SetFontColor(XLColor.FromHtml("#dc2626"));
                    }
                }

                // ── Bordes de la sección
                int secEnd = DAT + cal.Entradas.Count - 1;
                ws.Range(HDR, cs, secEnd, ce).Style
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorderColor(XLColor.FromHtml("#cbd5e1"));
                ws.Range(HDR, cs, COL, ce).Style
                    .Border.SetBottomBorder(XLBorderStyleValues.Medium)
                    .Border.SetBottomBorderColor(XLColor.FromHtml("#1E3A8A"));

                // ── Anchos de columna
                ws.Column(cs).Width = 11;
                for (int ri = 0; ri < cal.Roles.Count; ri++)
                    ws.Column(cs + 1 + ri).Width = Math.Max(18, cal.Roles[ri].Length + 5);
            }

            ws.SheetView.Freeze(5, 0); // título + subtítulo + separador + sección + cols siempre visibles

            // Columnas separadoras: ancho mínimo
            for (int ci = 0; ci < calendarios.Count - 1; ci++)
                ws.Column(colStarts[ci] + 1 + calendarios[ci].Roles.Count).Width = 2;

            // Pie de página
            int footerRow = DAT + maxEntradas + 1;
            ws.Cell(footerRow, 1).Value = $"Generado con Congrega CRM — {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(footerRow, 1, footerRow, totalCols).Merge();
            ws.Cell(footerRow, 1).Style
                .Font.SetItalic(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.FromHtml("#94a3b8"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            return GenerarArchivoExcel(wb, $"Calendario_{tipo}");
        }

        // ── helpers ──────────────────────────────────────────────────────────────────
        private static string TipoLabel(int tipo) => tipo switch
        {
            CN_Calendario.TIPO_ALABANZA      => "Alabanza",
            CN_Calendario.TIPO_AUDIOVISUALES => "Audiovisuales",
            _                                => "Seguridad y Bienvenida"
        };

        private static string DeterminarTrimestre(DateTime fecha)
        {
            int q = (fecha.Month - 1) / 3 + 1;
            string[] nombres = { "I TRIMESTRE", "II TRIMESTRE", "III TRIMESTRE", "IV TRIMESTRE" };
            return $"{nombres[q - 1]} {fecha.Year}";
        }

        private static string PeriodoLabel(string p) => p.ToLowerInvariant() switch
        {
            "semanal"    => "Semanal",
            "trimestral" => "Trimestral",
            _            => "Mensual"
        };

        private static void AplicarEstiloFinal(IXLWorksheet ws, int headerRow, int lastRow, int totalCols)
        {
            ws.Range(headerRow, 1, lastRow, totalCols).Style
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorderColor(XLColor.FromHtml("#cbd5e1"));

            ws.Range(headerRow, 1, headerRow, totalCols).Style
                .Border.SetBottomBorder(XLBorderStyleValues.Medium)
                .Border.SetBottomBorderColor(XLColor.FromHtml("#1E3A8A"));

            ws.Cell(lastRow + 2, 1).Value = $"Generado con Congrega CRM — {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(lastRow + 2, 1, lastRow + 2, totalCols).Merge();
            ws.Cell(lastRow + 2, 1).Style
                .Font.SetItalic(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#94a3b8"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        }

        private IActionResult GenerarArchivoExcel(XLWorkbook wb, string nombre)
        {
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);
            string fileName = $"{nombre}_{DateTime.Now:yyyyMMdd}.xlsx".Replace(" ", "_");
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
