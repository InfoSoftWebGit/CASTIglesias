using CapaDatos;
using CapaEntidad;
using static CapaDatos.CD_AnaliticaGrupos;

namespace CapaNegocio
{
    /// <summary>
    /// Evolución de los jóvenes: cuántos hay (por edad, según el rango configurado en
    /// Ajustes > Jóvenes), en qué estado están, cuántos llegan y cuántos pasan a adultos.
    /// </summary>
    /// <remarks>
    /// La edad en cada fecha sale de la fecha de nacimiento. Si no la hay, se estima
    /// a partir de la edad guardada (que es la de hoy). Quien no tiene ninguna de las
    /// dos no se puede clasificar y se avisa al pie.
    /// </remarks>
    public class CN_AnaliticaJovenes
    {
        private readonly CD_AnaliticaGrupos _cd;
        private readonly CD_AnaliticaCongregantes _cdCongregantes;

        public CN_AnaliticaJovenes(CD_AnaliticaGrupos cd, CD_AnaliticaCongregantes cdCongregantes)
        {
            _cd = cd;
            _cdCongregantes = cdCongregantes;
        }

        public PanelAnaliticaDTO Calcular(int sedeID, DateTime desde, DateTime hasta, string? agrupacion)
        {
            var (min, max) = _cd.RangoJovenes(sedeID);
            return Calcular(_cdCongregantes.ListarEventos(sedeID), _cd.ListarEdades(sedeID), min, max, desde, hasta, agrupacion, DateTime.Today);
        }

        public static PanelAnaliticaDTO Calcular(List<EventoEstadoDTO> eventos, List<EdadFila> edades, int edadMin, int edadMax,
                                                 DateTime desde, DateTime hasta, string? agrupacion, DateTime hoy)
        {
            (desde, hasta) = AnaliticaComun.NormalizarRango(desde, hasta);
            agrupacion = AnaliticaComun.NormalizarAgrupacion(agrupacion);
            var periodos = AnaliticaComun.Periodos(desde, hasta, ref agrupacion);
            var (desdeAnt, hastaAnt) = AnaliticaComun.PeriodoAnterior(desde, hasta);
            var rep = new CN_AnaliticaCongregantes.Reproductor(eventos);
            var porId = edades.GroupBy(e => e.IdMiembro).ToDictionary(g => g.Key, g => g.First());

            int? EdadEn(int id, DateTime fecha)
            {
                if (!porId.TryGetValue(id, out var e)) return null;
                if (e.Cumpleanios is { } nac)
                {
                    int años = fecha.Year - nac.Year;
                    if (fecha.Date < nac.Date.AddYears(años)) años--;
                    return años;
                }
                return e.Edad is { } edad and > 0 ? edad - (hoy.Year - fecha.Year) : null;
            }
            bool EsJoven(int id, DateTime fecha) => EdadEn(id, fecha) is { } a && a >= edadMin && a <= edadMax;

            var refHasta = hasta < DateTime.Now ? hasta : DateTime.Now;
            var finPeriodo = periodos.Select(p => p.Fin < DateTime.Now ? p.Fin : DateTime.Now).ToList();
            List<(int Id, string? Estado, DateTime? Desde)> Jovenes(DateTime t) => rep.ActivosEn(t).Where(a => EsJoven(a.Id, t)).ToList();
            int Nuevos(DateTime a, DateTime b) => rep.Altas(a, b).Count(x => EsJoven(x.Id, x.Fecha));
            int AMiembro(DateTime a, DateTime b) => rep.CambiosA("Miembro", a, b).Count(x => EsJoven(x.Id, x.Fecha));
            int PasanAdultos(DateTime a, DateTime b) => rep.ActivosEn(b < DateTime.Now ? b : DateTime.Now)
                .Count(x => EdadEn(x.Id, a.AddTicks(-1)) is { } antes && antes <= edadMax && antes >= edadMin
                            && EdadEn(x.Id, b) is { } despues && despues > edadMax);
            decimal Pct(DateTime t) { var act = rep.ActivosEn(t).Count; return act == 0 ? 0 : Math.Round(Jovenes(t).Count * 100m / act, 1); }

            var r = new PanelAnaliticaDTO
            {
                modulo = "jovenes",
                desde = desde, hasta = hasta, desde_anterior = desdeAnt, hasta_anterior = hastaAnt,
                agrupacion = agrupacion,
                periodos = periodos.Select(p => p.Etiqueta).ToList()
            };

            var fotos = finPeriodo.Select(Jovenes).ToList();
            r.kpis.Add(AnaliticaComun.Kpi("jovenes", Jovenes(refHasta).Count, Jovenes(hastaAnt).Count, fotos.Select(f => (decimal)f.Count)));
            r.kpis.Add(AnaliticaComun.Kpi("nuevos_jovenes", Nuevos(desde, hasta), Nuevos(desdeAnt, hastaAnt), periodos.Select(p => (decimal)Nuevos(p.Inicio, p.Fin))));
            r.kpis.Add(AnaliticaComun.Kpi("jovenes_a_miembro", AMiembro(desde, hasta), AMiembro(desdeAnt, hastaAnt), periodos.Select(p => (decimal)AMiembro(p.Inicio, p.Fin))));
            r.kpis.Add(AnaliticaComun.Kpi("pasan_adultos", PasanAdultos(desde, hasta), PasanAdultos(desdeAnt, hastaAnt), Enumerable.Empty<decimal>()));
            r.kpis.Add(AnaliticaComun.Kpi("pct_jovenes", Pct(refHasta), Pct(hastaAnt), finPeriodo.Select(Pct), formato: "porcentaje"));

            r.graficos.Add(new GraficoDTO
            {
                codigo = "jovenes_por_estado", tipo = GraficoDTO.AreaApilada,
                series = Enumerable.Reverse(CN_AnaliticaCongregantes.Estados)
                    .Select(e => new SerieDTO { codigo = e, datos = fotos.Select(f => (decimal?)f.Count(x => x.Estado == e)).ToList() }).ToList()
            });
            r.graficos.Add(new GraficoDTO
            {
                codigo = "nuevos_jovenes", tipo = GraficoDTO.Tendencia,
                series =
                {
                    new SerieDTO { codigo = "nuevos_jovenes", datos = periodos.Select(p => (decimal?)Nuevos(p.Inicio, p.Fin)).ToList() },
                    new SerieDTO { codigo = "anio_anterior", datos = periodos.Select(p => (decimal?)Nuevos(p.Inicio.AddYears(-1), p.Fin.AddYears(-1))).ToList() }
                }
            });
            var hoyJovenes = Jovenes(refHasta);
            var edadesRango = Enumerable.Range(edadMin, Math.Max(0, edadMax - edadMin + 1)).ToList();
            r.graficos.Add(new GraficoDTO
            {
                codigo = "por_edad", tipo = GraficoDTO.BarrasH, baja = true,
                categorias = edadesRango.Select(e => e.ToString()).ToList(),
                series = { new SerieDTO { codigo = "personas", datos = edadesRango.Select(e => (decimal?)hoyJovenes.Count(j => EdadEn(j.Id, refHasta) == e)).ToList() } }
            });

            // ── Resumen ──
            var k = r.kpis.ToDictionary(x => x.codigo);
            var f = r.resumen;
            var jov = k["jovenes"];
            if (jov.valor > jov.anterior) f.Add(AnaliticaComun.Frase("jov_crece", "positivo", ("n", jov.valor), ("ant", jov.anterior), ("min", edadMin), ("max", edadMax)));
            else if (jov.valor < jov.anterior) f.Add(AnaliticaComun.Frase("jov_decrece", "negativo", ("n", jov.valor), ("ant", jov.anterior), ("min", edadMin), ("max", edadMax)));
            else f.Add(AnaliticaComun.Frase("jov_estable", "neutro", ("n", jov.valor), ("min", edadMin), ("max", edadMax)));

            f.Add(AnaliticaComun.Frase("jov_pct", "neutro", ("pct", k["pct_jovenes"].valor)));
            if (k["nuevos_jovenes"].valor > 0) f.Add(AnaliticaComun.Frase("jov_nuevos", "positivo", ("n", k["nuevos_jovenes"].valor)));
            if (k["jovenes_a_miembro"].valor > 0) f.Add(AnaliticaComun.Frase("jov_miembros", "positivo", ("n", k["jovenes_a_miembro"].valor)));
            if (k["pasan_adultos"].valor > 0) f.Add(AnaliticaComun.Frase("jov_adultos", "neutro", ("n", k["pasan_adultos"].valor), ("max", edadMax)));

            // Los que superan pronto la edad: preparar su paso al grupo de adultos
            int proximos = hoyJovenes.Count(j => EdadEn(j.Id, refHasta) == edadMax);
            if (proximos > 0) f.Add(AnaliticaComun.Frase("jov_proximos", "neutro", ("n", proximos), ("max", edadMax)));

            int sinEdad = rep.ActivosEn(refHasta).Count(a => EdadEn(a.Id, refHasta) == null);
            if (sinEdad > 0) r.nota = AnaliticaComun.Frase("jov_sin_edad", "neutro", ("n", sinEdad));
            return r;
        }
    }
}
