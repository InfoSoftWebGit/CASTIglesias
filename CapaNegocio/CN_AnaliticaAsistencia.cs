using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Evolución de la asistencia a los cultos: media por culto, composición
    /// (adultos, niños, nuevos, invitados), comparación con el periodo anterior y
    /// con el mismo periodo del año pasado.
    /// </summary>
    /// <remarks>
    /// La cifra principal es la MEDIA POR CULTO, no el total: un mes con cinco
    /// domingos o con un culto especial suma más gente sin que la iglesia haya
    /// crecido. El total se da también, pero como dato secundario.
    /// </remarks>
    public class CN_AnaliticaAsistencia
    {
        private readonly CD_AnaliticaAsistencia _cd;

        public CN_AnaliticaAsistencia(CD_AnaliticaAsistencia cd) => _cd = cd;

        public AnaliticaAsistenciaDTO Calcular(int sedeID, DateTime desde, DateTime hasta, string? agrupacion, string? turno)
        {
            var (d, h) = AnaliticaComun.NormalizarRango(desde, hasta);
            var (dAnt, _) = AnaliticaComun.PeriodoAnterior(d, h);
            var inicioLectura = dAnt < d.AddYears(-1) ? dAnt : d.AddYears(-1);
            return Calcular(_cd.ListarCultos(sedeID, inicioLectura, h), d, h, agrupacion, turno);
        }

        /// <summary>El cálculo sin base de datos, para poder probarlo.</summary>
        public static AnaliticaAsistenciaDTO Calcular(List<CultoAsistenciaDTO> todos, DateTime desde, DateTime hasta,
                                                     string? agrupacion, string? turno)
        {
            (desde, hasta) = AnaliticaComun.NormalizarRango(desde, hasta);
            agrupacion = AnaliticaComun.NormalizarAgrupacion(agrupacion);
            var periodos = AnaliticaComun.Periodos(desde, hasta, ref agrupacion);
            var (desdeAnt, hastaAnt) = AnaliticaComun.PeriodoAnterior(desde, hasta);

            turno = string.IsNullOrWhiteSpace(turno) ? null : turno;
            var cultos = turno == null ? todos : todos.Where(c => c.turno == turno).ToList();
            List<CultoAsistenciaDTO> Entre(DateTime a, DateTime b) => cultos.Where(c => c.fecha >= a && c.fecha <= b).ToList();

            var r = new AnaliticaAsistenciaDTO
            {
                desde = desde,
                hasta = hasta,
                desde_anterior = desdeAnt,
                hasta_anterior = hastaAnt,
                agrupacion = agrupacion,
                turno = turno,
                turnos = todos.Select(c => c.turno).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t).ToList()
            };

            foreach (var p in periodos)
            {
                var lista = Entre(p.Inicio, p.Fin);
                r.periodos.Add(new PeriodoAsistenciaDTO
                {
                    etiqueta = p.Etiqueta,
                    inicio = p.Inicio,
                    fin = p.Fin,
                    cultos = lista.Count,
                    adultos = lista.Sum(c => c.adultos),
                    ninos = lista.Sum(c => c.ninos),
                    nuevos = lista.Sum(c => c.nuevos),
                    invitados = lista.Sum(c => c.invitados),
                    total = lista.Sum(c => c.total),
                    media = Media(lista),
                    maximo = lista.Count == 0 ? 0 : lista.Max(c => c.total)
                });
                r.media_anio_anterior.Add(Media(Entre(p.Inicio.AddYears(-1), p.Fin.AddYears(-1))));
            }

            var actual = Entre(desde, hasta);
            var anterior = Entre(desdeAnt, hastaAnt);

            r.kpis = new List<KpiDTO>
            {
                AnaliticaComun.Kpi("media", Media(actual) ?? 0, Media(anterior) ?? 0, r.periodos.Select(p => p.media ?? 0)),
                AnaliticaComun.Kpi("total", actual.Sum(c => c.total), anterior.Sum(c => c.total), r.periodos.Select(p => (decimal)p.total)),
                AnaliticaComun.Kpi("nuevos", actual.Sum(c => c.nuevos), anterior.Sum(c => c.nuevos), r.periodos.Select(p => (decimal)p.nuevos)),
                AnaliticaComun.Kpi("invitados", actual.Sum(c => c.invitados), anterior.Sum(c => c.invitados), r.periodos.Select(p => (decimal)p.invitados)),
                AnaliticaComun.Kpi("maximo", actual.Count == 0 ? 0 : actual.Max(c => c.total), anterior.Count == 0 ? 0 : anterior.Max(c => c.total),
                                   r.periodos.Select(p => (decimal)p.maximo)),
                AnaliticaComun.Kpi("cultos", actual.Count, anterior.Count, r.periodos.Select(p => (decimal)p.cultos))
            };

            r.por_turno = actual.GroupBy(c => c.turno)
                .Select(g => new TurnoAsistenciaDTO
                {
                    turno = g.Key,
                    cultos = g.Count(),
                    media = Media(g.ToList()) ?? 0,
                    media_anterior = Media(anterior.Where(c => c.turno == g.Key).ToList()) ?? 0
                })
                .OrderByDescending(t => t.media)
                .ToList();

            var mejor = actual.OrderByDescending(c => c.total).ThenByDescending(c => c.fecha).FirstOrDefault();
            if (mejor != null) r.mejor_culto = new MejorCultoDTO { fecha = mejor.fecha, turno = mejor.turno, total = mejor.total };

            r.resumen = Resumen(r, actual, Entre(desde.AddYears(-1), hasta.AddYears(-1)));
            return r;
        }

        private static decimal? Media(List<CultoAsistenciaDTO> lista) =>
            lista.Count == 0 ? null : Math.Round((decimal)lista.Sum(c => c.total) / lista.Count, 1);

        #region Motor de reglas

        /// <summary>Variación de la media a partir de la cual se comenta (la asistencia es estable: 5 %).</summary>
        private const decimal Umbral = 5m;

        private static List<FraseResumenDTO> Resumen(AnaliticaAsistenciaDTO r, List<CultoAsistenciaDTO> actual, List<CultoAsistenciaDTO> haceUnAnio)
        {
            var f = new List<FraseResumenDTO>();
            if (actual.Count == 0) { f.Add(AnaliticaComun.Frase("asis_sin_registros", "alerta")); return f; }

            var kMedia = r.kpis.First(k => k.codigo == "media");
            if (kMedia.anterior > 0)
            {
                var v = kMedia.variacion ?? 0;
                f.Add(v >= Umbral ? AnaliticaComun.Frase("asis_media_sube", "positivo", ("n", kMedia.valor), ("pct", v), ("ant", kMedia.anterior))
                    : v <= -Umbral ? AnaliticaComun.Frase("asis_media_baja", "negativo", ("n", kMedia.valor), ("pct", Math.Abs(v)), ("ant", kMedia.anterior))
                    : AnaliticaComun.Frase("asis_media_estable", "neutro", ("n", kMedia.valor), ("ant", kMedia.anterior)));
            }
            else f.Add(AnaliticaComun.Frase("asis_media", "neutro", ("n", kMedia.valor)));

            // Contra el año pasado: lo que de verdad dice si se crece, sin estacionalidad
            var mediaAnio = Media(haceUnAnio);
            if (mediaAnio is > 0)
            {
                var v = Math.Round((kMedia.valor - mediaAnio.Value) * 100m / mediaAnio.Value, 1);
                if (Math.Abs(v) >= Umbral)
                    f.Add(AnaliticaComun.Frase(v > 0 ? "asis_anio_sube" : "asis_anio_baja", v > 0 ? "positivo" : "negativo",
                                               ("pct", Math.Abs(v)), ("ant", mediaAnio.Value)));
            }

            var serie = r.periodos.Where(p => p.media.HasValue).Select(p => p.media!.Value).ToList();
            if (serie.Count >= 4)
            {
                var racha = AnaliticaComun.Racha(serie);
                if (racha <= -3) f.Add(AnaliticaComun.Frase("asis_tendencia_baja", "alerta", ("n", -racha)));
                else if (racha >= 3) f.Add(AnaliticaComun.Frase("asis_tendencia_sube", "positivo", ("n", racha)));
            }

            // El turno que más ha cambiado, si el cambio es notable y hay datos en ambos periodos
            var turno = r.por_turno.Where(t => t.cultos >= 2 && t.media_anterior > 0)
                .Select(t => (t, v: Math.Round((t.media - t.media_anterior) * 100m / t.media_anterior, 1)))
                .Where(x => Math.Abs(x.v) >= 10).OrderByDescending(x => Math.Abs(x.v)).FirstOrDefault();
            if (turno.t != null && r.turno == null)
                f.Add(AnaliticaComun.Frase(turno.v > 0 ? "asis_turno_sube" : "asis_turno_baja", turno.v > 0 ? "positivo" : "negativo",
                                           ("turno", turno.t.turno), ("pct", Math.Abs(turno.v)), ("n", turno.t.media)));

            var total = actual.Sum(c => c.total);
            var nuevos = actual.Sum(c => c.nuevos);
            if (nuevos > 0 && total > 0)
                f.Add(AnaliticaComun.Frase("asis_nuevos", "neutro", ("n", nuevos), ("pct", Math.Round(nuevos * 100m / total, 1))));

            if (r.mejor_culto != null)
                f.Add(AnaliticaComun.Frase("asis_mejor", "neutro", ("fecha", r.mejor_culto.fecha.ToString("yyyy-MM-dd")),
                                           ("turno", r.mejor_culto.turno), ("n", r.mejor_culto.total)));

            // Periodos ya pasados sin ningún registro: casi siempre es que no se apuntó
            if (r.agrupacion != "trimestre")
            {
                int huecos = r.periodos.Count(p => p.cultos == 0 && p.fin < DateTime.Now);
                if (huecos > 0) f.Add(AnaliticaComun.Frase(r.agrupacion == "semana" ? "asis_huecos_semana" : "asis_huecos_mes", "alerta", ("n", huecos)));
            }

            return f;
        }

        #endregion
    }
}
