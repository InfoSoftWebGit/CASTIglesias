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

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Calendario");

                string tipoLabel = req.TipoCalendario == 2 ? "Alabanza" : "Servidores";
                string titulo = $"Calendario de {tipoLabel} — {cal.NombreCulto}";
                int totalCols = 2 + cal.Roles.Count;

                // Fila 1: título
                ws.Cell(1, 1).Value = titulo;
                ws.Range(1, 1, 1, totalCols).Merge();
                ws.Cell(1, 1).Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(14)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                    .Font.SetFontColor(XLColor.White);

                // Fila 2: subtítulo con período
                string periodo = $"{cal.FechaInicio:dd/MM/yyyy} – {cal.FechaFin:dd/MM/yyyy}";
                ws.Cell(2, 1).Value = periodo;
                ws.Range(2, 1, 2, totalCols).Merge();
                ws.Cell(2, 1).Style
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Font.SetItalic(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"));

                // Fila 3: cabeceras
                ws.Cell(3, 1).Value = "Fecha";
                ws.Cell(3, 2).Value = "Día";
                for (int i = 0; i < cal.Roles.Count; i++)
                    ws.Cell(3, 3 + i).Value = cal.Roles[i];

                var headerRange = ws.Range(3, 1, 3, totalCols);
                headerRange.Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2c6e49"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Filas de datos
                for (int r = 0; r < cal.Entradas.Count; r++)
                {
                    var entrada = cal.Entradas[r];
                    int row = 4 + r;
                    ws.Cell(row, 1).Value = entrada.Fecha.ToString("dd/MM/yyyy");
                    ws.Cell(row, 2).Value = entrada.DiaSemana;

                    for (int c = 0; c < cal.Roles.Count; c++)
                    {
                        var rol = cal.Roles[c];
                        if (entrada.Asignaciones.TryGetValue(rol, out var miembros))
                            ws.Cell(row, 3 + c).Value = string.Join(", ", miembros);
                    }

                    if (r % 2 == 1)
                        ws.Range(row, 1, row, totalCols).Style
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#f1f5f9"));
                }

                // Bordes y ajuste automático
                var dataRange = ws.Range(3, 1, 3 + cal.Entradas.Count, totalCols);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                stream.Seek(0, SeekOrigin.Begin);

                string fileName = $"Calendario_{tipoLabel}_{cal.NombreCulto}_{DateTime.Now:yyyyMMdd}.xlsx"
                    .Replace(" ", "_");
                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }
    }
}
