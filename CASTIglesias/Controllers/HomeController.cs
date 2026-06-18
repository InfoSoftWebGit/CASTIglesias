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

        #region GRÁFICO DE ASISTENCIA
        [HttpGet]
        public JsonResult ObtenerGraficoAsistencia(string modo = "anual", int? anio = null, int? mes = null, int? dia = null, string turno = "todos")
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                int anioFiltro = anio ?? DateTime.Today.Year;

                var query = _context.Asistencia_Culto.AsQueryable();
                if (sedeID != 1000)
                    query = query.Where(a => a.ID_sede == sedeID);

                if (turno == "mañana" || turno == "tarde")
                    query = query.Where(a => a.turno_culto == turno);

                query = query.Where(a => a.fecha_asistencia_culto.Year == anioFiltro);

                if (modo == "anual")
                {
                    var agrupado = query
                        .GroupBy(a => a.fecha_asistencia_culto.Month)
                        .Select(g => new
                        {
                            Mes = g.Key,
                            Adultos = g.Sum(a => a.adulto_asistencia_culto),
                            Ninos = g.Sum(a => (int?)a.niños_asistencia_culto) ?? 0,
                            Invitados = g.Sum(a => a.invi_visit_asistencia_culto)
                        })
                        .OrderBy(x => x.Mes)
                        .ToList();

                    return Json(agrupado.Select(x => new
                    {
                        Etiqueta = new DateTime(anioFiltro, x.Mes, 1).ToString("MMM yyyy", new CultureInfo("es-ES")),
                        x.Adultos,
                        x.Ninos,
                        x.Invitados
                    }));
                }
                else if (modo == "mensual")
                {
                    int mesFiltro = mes ?? DateTime.Today.Month;
                    query = query.Where(a => a.fecha_asistencia_culto.Month == mesFiltro);

                    var agrupado = query
                        .GroupBy(a => new
                        {
                            a.fecha_asistencia_culto.Year,
                            a.fecha_asistencia_culto.Month,
                            a.fecha_asistencia_culto.Day
                        })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Month,
                            g.Key.Day,
                            Adultos = g.Sum(a => a.adulto_asistencia_culto),
                            Ninos = g.Sum(a => (int?)a.niños_asistencia_culto) ?? 0,
                            Invitados = g.Sum(a => a.invi_visit_asistencia_culto)
                        })
                        .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
                        .ToList();

                    return Json(agrupado.Select(x => new
                    {
                        Etiqueta = new DateTime(x.Year, x.Month, x.Day).ToString("dd/MM/yyyy"),
                        x.Adultos,
                        x.Ninos,
                        x.Invitados
                    }));
                }
                else // diario
                {
                    int mesFiltro = mes ?? DateTime.Today.Month;
                    int diaFiltro = dia ?? DateTime.Today.Day;

                    var totales = query
                        .Where(a => a.fecha_asistencia_culto.Month == mesFiltro
                                 && a.fecha_asistencia_culto.Day == diaFiltro)
                        .GroupBy(a => 1)
                        .Select(g => new
                        {
                            Adultos = g.Sum(a => a.adulto_asistencia_culto),
                            Ninos = g.Sum(a => (int?)a.niños_asistencia_culto) ?? 0,
                            Invitados = g.Sum(a => a.invi_visit_asistencia_culto)
                        })
                        .FirstOrDefault();

                    if (totales == null)
                        return Json(new object[0]);

                    return Json(new[]
                    {
                        new
                        {
                            Etiqueta = new DateTime(anioFiltro, mesFiltro, diaFiltro).ToString("dd/MM/yyyy"),
                            totales.Adultos,
                            totales.Ninos,
                            totales.Invitados
                        }
                    });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerGraficoAsistencia: {ex.Message}");
                return Json(new object[0]);
            }
        }
        #endregion GRÁFICO DE ASISTENCIA

        #region GRÁFICO DE DIEZMOS
        [HttpGet]
        public JsonResult ObtenerGraficoDiezmos(string modo = "anual", int? anio = null, int? mes = null)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                int anioFiltro = anio ?? DateTime.Today.Year;
                int mesFiltro = mes ?? DateTime.Today.Month;

                var baseQuery = _context.Diezmo
                    .Where(d => d.fecha_diezmo.HasValue && d.fecha_diezmo.Value.Year == anioFiltro);

                if (sedeID != 1000)
                    baseQuery = baseQuery.Where(d => d.ID_sede == sedeID);

                if (modo == "anual")
                {
                    var agrupado = baseQuery
                        .GroupBy(d => new { Mes = d.fecha_diezmo.Value.Month, Concepto = d.nombre_concepto ?? "Sin concepto" })
                        .Select(g => new { g.Key.Mes, g.Key.Concepto, Total = g.Sum(d => d.cantidad_diezmo) })
                        .ToList();

                    var conceptos = agrupado.Select(a => a.Concepto).Distinct().OrderBy(c => c).ToList();
                    var nombresMeses = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
                    var labels = nombresMeses.Select(m => $"{m} {anioFiltro}").ToList();

                    var series = conceptos.Select(c => new
                    {
                        concepto = c,
                        datos = Enumerable.Range(1, 12).Select(m =>
                            agrupado.Where(a => a.Mes == m && a.Concepto == c).Sum(a => a.Total)
                        ).ToList()
                    }).ToList();

                    return Json(new { labels, series });
                }
                else
                {
                    var filtrado = baseQuery.Where(d => d.fecha_diezmo.Value.Month == mesFiltro);
                    var agrupado = filtrado
                        .GroupBy(d => new { Dia = d.fecha_diezmo.Value.Day, Concepto = d.nombre_concepto ?? "Sin concepto" })
                        .Select(g => new { g.Key.Dia, g.Key.Concepto, Total = g.Sum(d => d.cantidad_diezmo) })
                        .ToList();

                    var conceptos = agrupado.Select(a => a.Concepto).Distinct().OrderBy(c => c).ToList();
                    int diasEnMes = DateTime.DaysInMonth(anioFiltro, mesFiltro);
                    var labels = Enumerable.Range(1, diasEnMes).Select(d => d.ToString()).ToList();

                    var series = conceptos.Select(c => new
                    {
                        concepto = c,
                        datos = Enumerable.Range(1, diasEnMes).Select(d =>
                            agrupado.Where(a => a.Dia == d && a.Concepto == c).Sum(a => a.Total)
                        ).ToList()
                    }).ToList();

                    return Json(new { labels, series });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerGraficoDiezmos: {ex.Message}");
                return Json(new { labels = new List<string>(), series = new List<object>() });
            }
        }
        #endregion GRÁFICO DE DIEZMOS

        #region GRÁFICO DE GASTOS
        [HttpGet]
        public JsonResult ObtenerGraficoGastos(string modo = "anual", int? anio = null, int? mes = null)
        {
            try
            {
                int sedeID = ObtenerIdSedeUsuario();
                int anioFiltro = anio ?? DateTime.Today.Year;
                int mesFiltro = mes ?? DateTime.Today.Month;

                var baseQuery = _context.Gastos
                    .Where(g => g.fecha_gasto.HasValue && g.fecha_gasto.Value.Year == anioFiltro);

                if (sedeID != 1000)
                    baseQuery = baseQuery.Where(g => g.id_sede == sedeID);

                var zonasNombres = _context.Zona
                    .Where(z => sedeID == 1000 || z.ID_sede == sedeID)
                    .Select(z => new { z.ID_zona, z.nombre_zona })
                    .ToList();

                if (modo == "anual")
                {
                    var agrupado = baseQuery
                        .GroupBy(g => new { Mes = g.fecha_gasto!.Value.Month, IdZona = (int?)g.id_zona })
                        .Select(g => new { g.Key.Mes, g.Key.IdZona, Total = g.Sum(x => x.cantidad) })
                        .ToList();

                    var nombresMeses = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
                    var labels = nombresMeses.Select(m => $"{m} {anioFiltro}").ToList();

                    var totales = Enumerable.Range(1, 12)
                        .Select(m => agrupado.Where(a => a.Mes == m).Sum(a => a.Total))
                        .ToList();

                    var zonaBreakdown = zonasNombres.Select(z => new
                    {
                        zona = z.nombre_zona,
                        datos = Enumerable.Range(1, 12)
                            .Select(m => agrupado.Where(a => a.Mes == m && a.IdZona == z.ID_zona).Sum(a => a.Total))
                            .ToList()
                    }).ToList();

                    // Incluir gastos sin zona asignada
                    var sinZonaDatos = Enumerable.Range(1, 12)
                        .Select(m => agrupado.Where(a => a.Mes == m && a.IdZona == null).Sum(a => a.Total))
                        .ToList();
                    if (sinZonaDatos.Any(v => v > 0))
                    {
                        zonaBreakdown = zonaBreakdown.Append(new { zona = "Sin zona", datos = sinZonaDatos }).ToList();
                    }

                    return Json(new { labels, totales, zonaBreakdown });
                }
                else
                {
                    var filtrado = baseQuery.Where(g => g.fecha_gasto!.Value.Month == mesFiltro);
                    var agrupado = filtrado
                        .GroupBy(g => new { Dia = g.fecha_gasto!.Value.Day, IdZona = (int?)g.id_zona })
                        .Select(g => new { g.Key.Dia, g.Key.IdZona, Total = g.Sum(x => x.cantidad) })
                        .ToList();

                    int diasEnMes = DateTime.DaysInMonth(anioFiltro, mesFiltro);
                    var labels = Enumerable.Range(1, diasEnMes).Select(d => d.ToString()).ToList();

                    var totales = Enumerable.Range(1, diasEnMes)
                        .Select(d => agrupado.Where(a => a.Dia == d).Sum(a => a.Total))
                        .ToList();

                    var zonaBreakdown = zonasNombres.Select(z => new
                    {
                        zona = z.nombre_zona,
                        datos = Enumerable.Range(1, diasEnMes)
                            .Select(d => agrupado.Where(a => a.Dia == d && a.IdZona == z.ID_zona).Sum(a => a.Total))
                            .ToList()
                    }).ToList();

                    var sinZonaDatos = Enumerable.Range(1, diasEnMes)
                        .Select(d => agrupado.Where(a => a.Dia == d && a.IdZona == null).Sum(a => a.Total))
                        .ToList();
                    if (sinZonaDatos.Any(v => v > 0))
                    {
                        zonaBreakdown = zonaBreakdown.Append(new { zona = "Sin zona", datos = sinZonaDatos }).ToList();
                    }

                    return Json(new { labels, totales, zonaBreakdown });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerGraficoGastos: {ex.Message}");
                return Json(new { labels = new List<string>(), totales = new List<decimal>(), zonaBreakdown = new List<object>() });
            }
        }
        #endregion GRÁFICO DE GASTOS

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