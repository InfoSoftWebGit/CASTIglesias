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
                return Json(new { success = false, mensaje = ex.Message });
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
                return Json(new { success = false, mensaje = ex.Message });
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
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Excel estándar (Seguridad y Bienvenida / Audiovisuales): filas=fechas, cols=roles
        // ────────────────────────────────────────────────────────────────────────────
        private IActionResult ExcelEstandar(CalendarioServicioDTO cal, CalendarioRequest req)
        {
            string tipoLabel = TipoLabel(req.TipoCalendario);
            int totalCols = 2 + cal.Roles.Count;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Calendario");

            ws.Row(1).Height = 42;
            ws.Cell(1, 1).Value = $"Congrega CRM — Calendario de {tipoLabel}";
            ws.Range(1, 1, 1, totalCols).Merge();
            ws.Cell(1, 1).Style
                .Font.SetBold(true).Font.SetFontSize(22).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            ws.Row(2).Height = 30;
            ws.Cell(2, 1).Value = cal.NombreCulto;
            ws.Range(2, 1, 2, totalCols).Merge();
            ws.Cell(2, 1).Style
                .Font.SetBold(true).Font.SetFontSize(18).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            ws.Row(3).Height = 24;
            string perLabel = PeriodoLabel(req.Periodicidad);
            ws.Cell(3, 1).Value = $"Período: {cal.FechaInicio:dd/MM/yyyy} — {cal.FechaFin:dd/MM/yyyy}   |   {perLabel}";
            ws.Range(3, 1, 3, totalCols).Merge();
            ws.Cell(3, 1).Style
                .Font.SetItalic(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#475569"))
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f1f5f9"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            ws.Row(4).Height = 32;
            ws.Cell(4, 1).Value = "Fecha";
            ws.Cell(4, 2).Value = "Día";
            for (int i = 0; i < cal.Roles.Count; i++)
                ws.Cell(4, 3 + i).Value = cal.Roles[i];

            ws.Range(4, 1, 4, totalCols).Style
                .Font.SetBold(true).Font.SetFontSize(16).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            for (int r = 0; r < cal.Entradas.Count; r++)
            {
                var entrada = cal.Entradas[r];
                int row = 5 + r;
                ws.Row(row).Height = 28;
                var bg = r % 2 == 0 ? XLColor.White : XLColor.FromHtml("#f0f4ff");

                void SetCell(int col, string val, bool bold = false)
                {
                    var c = ws.Cell(row, col);
                    c.Value = val;
                    c.Style.Font.SetFontSize(16).Font.SetBold(bold)
                            .Fill.SetBackgroundColor(bg)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                            .Alignment.SetWrapText(true);
                    if (val.Contains("Sin asignar")) c.Style.Font.SetFontColor(XLColor.FromHtml("#dc2626"));
                }

                SetCell(1, entrada.Fecha.ToString("dd/MM/yyyy"), true);
                string dia = entrada.DiaSemana.Length > 0
                    ? char.ToUpper(entrada.DiaSemana[0]) + entrada.DiaSemana[1..] : "";
                SetCell(2, dia);
                for (int ci = 0; ci < cal.Roles.Count; ci++)
                {
                    string texto = entrada.Asignaciones.TryGetValue(cal.Roles[ci], out var m)
                        ? string.Join("\n", m) : "";
                    SetCell(3 + ci, texto);
                }
            }

            AplicarEstiloFinal(ws, 4, 4 + cal.Entradas.Count, totalCols);
            ws.Column(1).Width = 16;
            ws.Column(2).Width = 14;
            for (int i = 0; i < cal.Roles.Count; i++)
                ws.Column(3 + i).Width = Math.Max(20, cal.Roles[i].Length + 4);

            return GenerarArchivoExcel(wb, $"Calendario_{tipoLabel}_{cal.NombreCulto}");
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Excel Alabanza: filas=roles/instrumentos, columnas=fechas (transpuesto)
        // ────────────────────────────────────────────────────────────────────────────
        private IActionResult ExcelAlabanza(CalendarioServicioDTO cal, CalendarioRequest req)
        {
            int numFechas = cal.Entradas.Count;
            int totalCols = 1 + numFechas;
            var cultura = new System.Globalization.CultureInfo("es-ES");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Alabanza");

            ws.Row(1).Height = 42;
            ws.Cell(1, 1).Value = "Congrega CRM — Cronograma Ministerio de Alabanza";
            ws.Range(1, 1, 1, totalCols).Merge();
            ws.Cell(1, 1).Style
                .Font.SetBold(true).Font.SetFontSize(20).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            ws.Row(2).Height = 30;
            ws.Cell(2, 1).Value = cal.NombreCulto;
            ws.Range(2, 1, 2, totalCols).Merge();
            ws.Cell(2, 1).Style
                .Font.SetBold(true).Font.SetFontSize(18).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            ws.Row(3).Height = 24;
            ws.Cell(3, 1).Value = $"Mes: {cal.FechaInicio.ToString("MMMM yyyy", cultura)}   |   {PeriodoLabel(req.Periodicidad)}";
            ws.Range(3, 1, 3, totalCols).Merge();
            ws.Cell(3, 1).Style
                .Font.SetItalic(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#475569"))
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f1f5f9"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Cabecera: col 1 = "Rol/Instrumento", col 2+ = fechas
            ws.Row(4).Height = 36;
            ws.Cell(4, 1).Value = "Rol / Instrumento";
            ws.Cell(4, 1).Style
                .Font.SetBold(true).Font.SetFontSize(16).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            for (int fi = 0; fi < cal.Entradas.Count; fi++)
            {
                var entrada = cal.Entradas[fi];
                string diaAb = entrada.Fecha.ToString("ddd", cultura);
                diaAb = char.ToUpper(diaAb[0]) + diaAb[1..];
                ws.Cell(4, 2 + fi).Value = $"{entrada.Fecha:dd/MM/yy}\n{diaAb}";
                ws.Cell(4, 2 + fi).Style
                    .Font.SetBold(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.White)
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
                ws.Row(row).Height = 30;
                bool esMinistra  = rol.Contains("ministra", StringComparison.OrdinalIgnoreCase);
                bool esZona      = rol == "Zona Responsable";
                var bg = ri % 2 == 0 ? XLColor.White : XLColor.FromHtml("#f0f4ff");
                var bgResaltado  = esMinistra ? XLColor.FromHtml("#dbeafe")
                                 : esZona     ? XLColor.FromHtml("#f0fdf4")
                                 : bg;

                var cRol = ws.Cell(row, 1);
                cRol.Value = rol;
                cRol.Style
                    .Font.SetBold(true).Font.SetFontSize(16)
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
                        .Font.SetFontSize(16)
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
            ws.Column(1).Width = 24;
            for (int fi = 0; fi < cal.Entradas.Count; fi++)
                ws.Column(2 + fi).Width = 20;

            return GenerarArchivoExcel(wb, $"Calendario_Alabanza_{cal.NombreCulto}");
        }

        // ── helpers ──────────────────────────────────────────────────────────────────
        private static string TipoLabel(int tipo) => tipo switch
        {
            CN_Calendario.TIPO_ALABANZA      => "Alabanza",
            CN_Calendario.TIPO_AUDIOVISUALES => "Audiovisuales",
            _                                => "Seguridad y Bienvenida"
        };

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
