using CapaDatos;
using CapaEntidad;
using static CapaDatos.CD_AnaliticaGrupos;

namespace CapaNegocio
{
    /// <summary>
    /// Evolución del seguimiento pastoral: cuántos contactos se hacen, a cuánta gente
    /// llegan, si los recién llegados reciben atención pronto y quién se queda sin ella.
    /// </summary>
    /// <remarks>
    /// La pregunta que más importa no es "cuántos seguimientos" sino "¿se está
    /// atendiendo a quien acaba de llegar?". Por eso los indicadores clave son el
    /// primer contacto (en cuántos días) y las personas sin seguimiento reciente.
    /// </remarks>
    public class CN_AnaliticaSeguimiento
    {
        private readonly CD_AnaliticaGrupos _cd;
        private readonly CD_AnaliticaCongregantes _cdCongregantes;

        public CN_AnaliticaSeguimiento(CD_AnaliticaGrupos cd, CD_AnaliticaCongregantes cdCongregantes)
        {
            _cd = cd;
            _cdCongregantes = cdCongregantes;
        }

        /// <summary>Días para considerar que un recién llegado tuvo contacto "a tiempo".</summary>
        public const int DiasPrimerContacto = 7;
        /// <summary>Días sin ningún seguimiento a partir de los cuales alguien está desatendido.</summary>
        public const int DiasSinSeguimiento = 60;
        private static readonly string[] EstadosAtendibles = { "Visitante", "Simpatizante", "Proceso" };

        public PanelAnaliticaDTO Calcular(int sedeID, DateTime desde, DateTime hasta, string? agrupacion)
        {
            var (_, h) = AnaliticaComun.NormalizarRango(desde, hasta);
            return Calcular(_cd.ListarSeguimientos(sedeID, h), _cdCongregantes.ListarEventos(sedeID), desde, hasta, agrupacion);
        }

        public static PanelAnaliticaDTO Calcular(List<SeguimientoFila> segs, List<EventoEstadoDTO> eventos,
                                                 DateTime desde, DateTime hasta, string? agrupacion)
        {
            (desde, hasta) = AnaliticaComun.NormalizarRango(desde, hasta);
            agrupacion = AnaliticaComun.NormalizarAgrupacion(agrupacion);
            var periodos = AnaliticaComun.Periodos(desde, hasta, ref agrupacion);
            var (desdeAnt, hastaAnt) = AnaliticaComun.PeriodoAnterior(desde, hasta);
            var rep = new CN_AnaliticaCongregantes.Reproductor(eventos);

            List<SeguimientoFila> Entre(DateTime a, DateTime b) => segs.Where(s => s.Fecha >= a && s.Fecha <= b).ToList();
            var primerSeg = segs.GroupBy(s => s.IdMiembro).ToDictionary(g => g.Key, g => g.Min(s => s.Fecha));
            var ultimosPorPersona = segs.GroupBy(s => s.IdMiembro).ToDictionary(g => g.Key, g => g.Select(s => s.Fecha).OrderBy(f => f).ToList());

            // Recién llegados del rango: % con primer seguimiento en ≤ 7 días y días hasta el primero
            (int Llegados, int ATiempo, int? Mediana) PrimerContacto(DateTime a, DateTime b)
            {
                var llegados = rep.Altas(a, b);
                var dias = llegados.Where(l => primerSeg.TryGetValue(l.Id, out var p) && p >= l.Fecha.Date)
                                   .Select(l => (primerSeg[l.Id] - l.Fecha.Date).Days).OrderBy(d => d).ToList();
                return (llegados.Count, dias.Count(d => d <= DiasPrimerContacto), dias.Count == 0 ? null : dias[dias.Count / 2]);
            }

            // Activos en estados atendibles sin ningún seguimiento en los últimos 60 días
            int SinSeguimiento(DateTime momento) => rep.ActivosEn(momento)
                .Where(a => EstadosAtendibles.Contains(a.Estado))
                .Count(a => !ultimosPorPersona.TryGetValue(a.Id, out var fechas) ||
                            !fechas.Any(f => f <= momento && f > momento.AddDays(-DiasSinSeguimiento)));

            var actual = Entre(desde, hasta);
            var anterior = Entre(desdeAnt, hastaAnt);
            var pc = PrimerContacto(desde, hasta);
            var pcAnt = PrimerContacto(desdeAnt, hastaAnt);
            decimal Pct(int a, int b) => b == 0 ? 0 : Math.Round(a * 100m / b, 1);

            var r = new PanelAnaliticaDTO
            {
                modulo = "seguimiento",
                desde = desde, hasta = hasta, desde_anterior = desdeAnt, hasta_anterior = hastaAnt,
                agrupacion = agrupacion,
                periodos = periodos.Select(p => p.Etiqueta).ToList()
            };

            var porPeriodo = periodos.Select(p => Entre(p.Inicio, p.Fin)).ToList();
            var referencia = hasta < DateTime.Now ? hasta : DateTime.Now;
            var refAnterior = hastaAnt < DateTime.Now ? hastaAnt : DateTime.Now;

            r.kpis = new List<KpiDTO>
            {
                AnaliticaComun.Kpi("seguimientos", actual.Count, anterior.Count, porPeriodo.Select(l => (decimal)l.Count)),
                AnaliticaComun.Kpi("personas_atendidas", actual.Select(s => s.IdMiembro).Distinct().Count(),
                                   anterior.Select(s => s.IdMiembro).Distinct().Count(),
                                   porPeriodo.Select(l => (decimal)l.Select(s => s.IdMiembro).Distinct().Count())),
                AnaliticaComun.Kpi("primer_contacto_pct", Pct(pc.ATiempo, pc.Llegados), Pct(pcAnt.ATiempo, pcAnt.Llegados), Enumerable.Empty<decimal>(), formato: "porcentaje"),
                AnaliticaComun.Kpi("dias_primer_contacto", pc.Mediana ?? 0, pcAnt.Mediana ?? 0, Enumerable.Empty<decimal>(), subirEsBueno: false),
                AnaliticaComun.Kpi("sin_seguimiento", SinSeguimiento(referencia), SinSeguimiento(refAnterior),
                                   periodos.Select(p => (decimal)SinSeguimiento(p.Fin < DateTime.Now ? p.Fin : DateTime.Now)), subirEsBueno: false)
            };

            r.graficos.Add(new GraficoDTO
            {
                codigo = "seguimientos", tipo = GraficoDTO.Tendencia,
                series =
                {
                    new SerieDTO { codigo = "seguimientos", datos = porPeriodo.Select(l => (decimal?)l.Count).ToList() },
                    new SerieDTO { codigo = "anio_anterior", datos = periodos.Select(p => (decimal?)Entre(p.Inicio.AddYears(-1), p.Fin.AddYears(-1)).Count).ToList() }
                }
            });

            var tipos = actual.GroupBy(s => Nombre(s.Tipo)).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
            r.graficos.Add(new GraficoDTO
            {
                codigo = "por_tipo", tipo = GraficoDTO.Apiladas,
                series = tipos.Select(t => new SerieDTO
                {
                    codigo = t, nombre = t,
                    datos = porPeriodo.Select(l => (decimal?)l.Count(s => Nombre(s.Tipo) == t)).ToList()
                }).ToList()
            });

            var encargados = actual.GroupBy(s => Nombre(s.Encargado)).Select(g => (g.Key, n: g.Count()))
                                   .OrderByDescending(x => x.n).Take(10).ToList();
            r.graficos.Add(new GraficoDTO
            {
                codigo = "por_encargado", tipo = GraficoDTO.BarrasH, baja = true,
                categorias = encargados.Select(e => e.Key).ToList(),
                series =
                {
                    new SerieDTO { codigo = "periodo_actual", datos = encargados.Select(e => (decimal?)e.n).ToList() },
                    new SerieDTO { codigo = "periodo_anterior", datos = encargados.Select(e => (decimal?)anterior.Count(s => Nombre(s.Encargado) == e.Key)).ToList() }
                }
            });

            // ── Resumen ──
            var k = r.kpis.ToDictionary(x => x.codigo);
            var f = r.resumen;
            var seg = k["seguimientos"];
            if (seg.valor == 0) f.Add(AnaliticaComun.Frase("seg_ninguno", "alerta"));
            else if (seg.anterior > 0 && Math.Abs(seg.variacion ?? 0) >= 10)
                f.Add(AnaliticaComun.Frase(seg.variacion > 0 ? "seg_sube" : "seg_baja", seg.variacion > 0 ? "positivo" : "negativo",
                                           ("n", seg.valor), ("pct", Math.Abs(seg.variacion ?? 0)), ("ant", seg.anterior)));
            else f.Add(AnaliticaComun.Frase("seg_total", "neutro", ("n", seg.valor), ("personas", k["personas_atendidas"].valor)));

            if (pc.Llegados > 0)
            {
                var pct = Pct(pc.ATiempo, pc.Llegados);
                f.Add(AnaliticaComun.Frase("seg_primer_contacto", pct >= 80 ? "positivo" : pct < 50 ? "alerta" : "neutro",
                                           ("pct", pct), ("llegados", pc.Llegados), ("dias", DiasPrimerContacto)));
                int sinNinguno = rep.Altas(desde, hasta).Count(l => !primerSeg.ContainsKey(l.Id));
                if (sinNinguno > 0) f.Add(AnaliticaComun.Frase("seg_llegados_sin_contacto", "alerta", ("n", sinNinguno)));
            }

            var sin = k["sin_seguimiento"];
            if (sin.valor > 0) f.Add(AnaliticaComun.Frase("seg_desatendidos", "alerta", ("n", sin.valor), ("dias", DiasSinSeguimiento)));

            if (encargados.Count > 1 && actual.Count >= 10)
            {
                var top = encargados[0];
                var pctTop = Pct(top.n, actual.Count);
                if (pctTop >= 50) f.Add(AnaliticaComun.Frase("seg_concentrado", "neutro", ("encargado", top.Key), ("pct", pctTop)));
            }
            return r;
        }

        private static string Nombre(string s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();
    }
}
