// Archivo: CASTIglesias.Controllers/HomeController.cs

using CapaDatos;
using CapaEntidad;
using CapaNegocio;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CASTIglesias.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        #region Constructor
        private readonly CN_Miembros _cnMiembros;
        private readonly CN_Diezmo _cnDiezmo;
        private readonly CN_Usuarios _cnUsuarios;
        private readonly AppDbContext _context;

        public HomeController(
          CN_Miembros negocioMiembros,
          CN_Diezmo negocioDiezmo,
          CN_Usuarios negocioUsuarios,
          CN_Sedes negocioSedes,
          CN_Permisos negocioPermisos,
          AppDbContext context)
          : base(negocioSedes, negocioPermisos)
        {
            _cnMiembros = negocioMiembros;
            _cnDiezmo = negocioDiezmo;
            _cnUsuarios = negocioUsuarios;
            _context = context;
        }
        #endregion

        public IActionResult Index() => View();
        public IActionResult Bienvenida() => View();

        // ----------------------------------------------------
        #region DASHBOARD
        [HttpGet]
        public JsonResult VerDashboard()
        {
            try
            {
                // 1. Obtener el ID de sede. Ahora devuelve INT (1000 para Global).
                int sedeID = ObtenerIdSedeUsuario(); // 👈 Cambio: Retorno de INT no anulable

                // Ahora enviamos INT (1000 si es global)
                var oListaDiezmos = _cnDiezmo.ListarDiezmos(sedeID); // 👈 Usar sedeID directamente

                var data = oListaDiezmos.Select(d => new
                {
                    d.ID_diezmo,
                    d.nombre_miembro,
                    d.cantidad_diezmo,
                    fecha_diezmo = d.fecha_diezmo,
                    FechaFormateada = d.FechaFormateada
                });
                return Json(new { data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
        }

        #region MÉTODOS PARA LAS TARJETAS DEL DASHBOARD
        [HttpGet]
        public IActionResult ObtenerZonasConMiembros()
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                var resultado = (
                    from z in _context.Zona
                    join mzg in _context.Miembros_Zona_Grupo_Ministerio on z.ID_zona equals mzg.ID_zona
                    where sedeID == 1000 || z.ID_sede == sedeID
                    group mzg by new { z.ID_zona, z.nombre_zona } into g
                    select new
                    {
                        ID_zona = g.Key.ID_zona,
                        nombre_zona = g.Key.nombre_zona,
                        total_miembros = g.Count()
                    }
                )
                .OrderBy(x => x.nombre_zona)
                .ToList();

                return new JsonResult(resultado);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener zonas con miembros: {ex.Message}");
                return new JsonResult(new List<object>());
            }
        }
        #endregion MÉTODOS PARA LAS TARJETAS DEL DASHBOARD
        #endregion DASHBOARD

        #region DIEZMOS
        public IActionResult Diezmos() => View();



        [HttpPost]
        public IActionResult ExportarHistorialDiezmo(string fechainicio, string fechafin)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                DateTime fechaInicioParsed = DateTime.ParseExact(fechainicio, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime fechaFinParsed = DateTime.ParseExact(fechafin, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                                        .Date.AddDays(1).AddTicks(-1);

                var fechaInicioBD = fechaInicioParsed.ToString("yyyy-MM-dd");
                var fechaFinBD = fechaFinParsed.ToString("yyyy-MM-dd");

                // Ahora enviamos INT
                var oLista = _cnDiezmo.HistorialDiezmos(fechaInicioBD, fechaFinBD, sedeID); // ⬅️ CORREGIDO

                DataTable dt = new DataTable();
                dt.Locale = new System.Globalization.CultureInfo("es-ES");
                dt.Columns.Add("Fecha de Diezmo", typeof(string));
                dt.Columns.Add("Miembro", typeof(string));
                dt.Columns.Add("Concepto", typeof(string));
                dt.Columns.Add("Cantidad", typeof(decimal));

                foreach (var rp in oLista)
                {
                    string fechaDiezmoFormatted = Convert.ToDateTime(rp.fecha_diezmo).ToString("dd/MM/yyyy");
                    dt.Rows.Add(fechaDiezmoFormatted, rp.nombre_miembro, rp.nombre_concepto, rp.cantidad_diezmo);
                }

                dt.TableName = "Datos";

                using (XLWorkbook wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add(dt, "Reporte de Diezmos");
                    ws.Columns().AdjustToContents();

                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        return File(stream.ToArray(),
                          "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                          $"ReporteDiezmos_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerHistorialDiezmos(string fechainicio, string fechafin)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();

                if (string.IsNullOrEmpty(fechainicio)) fechainicio = DateTime.Today.ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(fechafin)) fechafin = DateTime.Today.ToString("yyyy-MM-dd");

                DateTime fechaInicioParsed = DateTime.ParseExact(fechainicio, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime fechaFinParsed = DateTime.ParseExact(fechafin, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                                         .Date.AddDays(1).AddTicks(-1);


                var lista = _cnDiezmo.HistorialDiezmos(
                    fechaInicioParsed.ToString("yyyy-MM-dd"),
                    fechaFinParsed.ToString("yyyy-MM-dd"),
                    sedeID
                );

                return Json(lista);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { data = new object[0], error = true, mensaje = ex.Message });
            }
            catch
            {
                return Json(new object[0]);
            }
        }
        public IActionResult SumaDiezmosTotales(string fechainicio, string fechafin)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario(); 

                DateTime fechaInicioParsed;
                DateTime fechaFinParsed;

                if (!DateTime.TryParse(fechainicio, out fechaInicioParsed) || !DateTime.TryParse(fechafin, out fechaFinParsed))
                {
                    return Json(new { total = 0 });
                }

                fechaFinParsed = fechaFinParsed.Date.AddDays(1).AddSeconds(-1);

                var consultaDiezmo = _context.Diezmo.AsQueryable();

                if (sedeID != 1000)
                {
                    consultaDiezmo = consultaDiezmo.Where(d => d.ID_sede == sedeID);
                }
                
                var total = consultaDiezmo
                  .Where(d => d.fecha_diezmo >= fechaInicioParsed && d.fecha_diezmo <= fechaFinParsed)
                  .Sum(d => (decimal?)d.cantidad_diezmo) ?? 0;

                System.Diagnostics.Debug.WriteLine($"Total de Diezmos filtrado: {total}");

                return Json(new { total = total });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { total = 0, error = ex.Message });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al calcular el total de Diezmos: {ex.Message}");
                return Json(new { total = 0 });
            }
        }
        #endregion DIEZMOS
    }
}