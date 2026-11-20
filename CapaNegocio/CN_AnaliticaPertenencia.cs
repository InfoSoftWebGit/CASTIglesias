using CapaDatos;
using CapaEntidad;
using static CapaDatos.CD_AnaliticaGrupos;

namespace CapaNegocio
{
    /// <summary>
    /// Evolución de zonas, grupos y ministerios (cuánta gente hay en cada uno,
    /// quién entra y quién sale) y del liderazgo (cuántos líderes y a cuánta gente
    /// toca cada uno).
    /// </summary>
    /// <remarks>
    /// Sale del historial de pertenencia (ver HistorialPertenencia). Una persona en
    /// dos grupos cuenta en los dos al mirar grupo a grupo, y una sola vez en el
    /// total de "personas en algún grupo".
    /// </remarks>
    public class CN_AnaliticaPertenencia
    {
        private readonly CD_AnaliticaGrupos _cd;
        private readonly CD_AnaliticaCongregantes _cdCongregantes;

        public CN_AnaliticaPertenencia(CD_AnaliticaGrupos cd, CD_AnaliticaCongregantes cdCongregantes)
        {
            _cd = cd;
            _cdCongregantes = cdCongregantes;
        }

        public static readonly string[] Tipos =
        {
            HistorialPertenencia.TipoZona, HistorialPertenencia.TipoGrupo,
            HistorialPertenencia.TipoMinisterio, HistorialPertenencia.TipoLider
        };

        /// <summary>Destinos que se dibujan por separado en el área apilada; el resto, "otros".</summary>
        private const int MaxDestinos = 6;
        /// <summary>Personas activas por líder a partir de las cuales la carga se considera alta.</summary>
        public const int CargaAltaPorLider = 15;

        public PanelAnaliticaDTO Calcular(int sedeID, string tipo, DateTime desde, DateTime hasta, string? agrupacion)
        {
            if (!Tipos.Contains(tipo)) tipo = HistorialPertenencia.TipoZona;
            return Calcular(tipo, _cd.ListarPertenencia(sedeID, tipo), _cd.NombresDestino(tipo),
                            _cdCongregantes.ListarEventos(sedeID), desde, hasta, agrupacion);
        }

        private sealed record Paso(bool Entra, DateTime Efectiva, DateTime? Real);

        public static PanelAnaliticaDTO Calcular(string tipo, List<PertenenciaFila> filas, Dictionary<int, string> nombres,
                                                 List<EventoEstadoDTO> eventos, DateTime desde, DateTime hasta, string? agrupacion)
        {
            (desde, hasta) = AnaliticaComun.NormalizarRango(desde, hasta);
            agrupacion = AnaliticaComun.NormalizarAgrupacion(agrupacion);
            var periodos = AnaliticaComun.Periodos(desde, hasta, ref agrupacion);
            var (desdeAnt, hastaAnt) = AnaliticaComun.PeriodoAnterior(desde, hasta);
            bool esLider = tipo == HistorialPertenencia.TipoLider;
            var rep = new CN_AnaliticaCongregantes.Reproductor(eventos);

            // Historia de cada (persona, destino), ordenada; sin fecha = desde siempre
            var historias = filas.GroupBy(f => (f.IdMiembro, f.Destino))
                .ToDictionary(g => g.Key, g => g
                    .Select(f => new Paso(f.Evento == HistorialPertenencia.Entra, f.Fecha ?? DateTime.MinValue, f.Fecha))
                    .OrderBy(p => p.Efectiva).ThenBy(p => p.Entra ? 0 : 1).ToList());

            bool Dentro(List<Paso> pasos, DateTime t)
            {
                bool dentro = false;
                foreach (var p in pasos) { if (p.Efectiva > t) break; dentro = p.Entra; }
                return dentro;
            }
            List<(int Miembro, int Destino)> DentroEn(DateTime t) => historias.Where(h => Dentro(h.Value, t)).Select(h => h.Key).ToList();
            int Personas(DateTime t) => DentroEn(t).Select(x => x.Miembro).Distinct().Count();
            int Contar(bool entra, DateTime a, DateTime b) => filas.Count(f => (f.Evento == HistorialPertenencia.Entra) == entra && f.Fecha >= a && f.Fecha <= b);
            int Activos(DateTime t) => rep.ActivosEn(t).Count;
            decimal Ratio(DateTime t) { var l = Personas(t); return l == 0 ? 0 : Math.Round((decimal)Activos(t) / l, 1); }
            string NombreDe(int id) => nombres.TryGetValue(id, out var n) && !string.IsNullOrWhiteSpace(n) ? n : $"#{id}";

            var finPeriodo = periodos.Select(p => p.Fin < DateTime.Now ? p.Fin : DateTime.Now).ToList();
            var refHasta = hasta < DateTime.Now ? hasta : DateTime.Now;
            var r = new PanelAnaliticaDTO
            {
                modulo = tipo,
                desde = desde, hasta = hasta, desde_anterior = desdeAnt, hasta_anterior = hastaAnt,
                agrupacion = agrupacion,
                periodos = periodos.Select(p => p.Etiqueta).ToList()
            };

            var serieTotal = finPeriodo.Select(t => (decimal)Personas(t)).ToList();
            int crecimiento = Personas(refHasta) - Personas(desde.AddTicks(-1));
            int crecimientoAnt = Personas(hastaAnt) - Personas(desdeAnt.AddTicks(-1));

            r.kpis.Add(AnaliticaComun.Kpi("personas", Personas(refHasta), Personas(hastaAnt), serieTotal));
            r.kpis.Add(AnaliticaComun.Kpi("entradas", Contar(true, desde, hasta), Contar(true, desdeAnt, hastaAnt),
                                          periodos.Select(p => (decimal)Contar(true, p.Inicio, p.Fin))));
            r.kpis.Add(AnaliticaComun.Kpi("salidas", Contar(false, desde, hasta), Contar(false, desdeAnt, hastaAnt),
                                          periodos.Select(p => (decimal)Contar(false, p.Inicio, p.Fin)), subirEsBueno: false));
            r.kpis.Add(AnaliticaComun.Kpi("crecimiento", crecimiento, crecimientoAnt, Enumerable.Empty<decimal>()));
            if (esLider)
                r.kpis.Add(AnaliticaComun.Kpi("personas_por_lider", Ratio(refHasta), Ratio(hastaAnt),
                                              finPeriodo.Select(Ratio), subirEsBueno: false));
            else
                r.kpis.Add(AnaliticaComun.Kpi("destinos_activos", DentroEn(refHasta).Select(x => x.Destino).Distinct().Count(),
                                              DentroEn(hastaAnt).Select(x => x.Destino).Distinct().Count(), Enumerable.Empty<decimal>()));

            r.graficos.Add(new GraficoDTO
            {
                codigo = "personas", tipo = GraficoDTO.Tendencia,
                series =
                {
                    new SerieDTO { codigo = "personas", datos = serieTotal.Select(v => (decimal?)v).ToList() },
                    new SerieDTO { codigo = "anio_anterior", datos = periodos.Select(p => (decimal?)Personas(p.Fin.AddYears(-1))).ToList() }
                }
            });
            r.graficos.Add(new GraficoDTO
            {
                codigo = "entradas_salidas", tipo = GraficoDTO.Divergentes,
                series =
                {
                    new SerieDTO { codigo = "entradas", datos = periodos.Select(p => (decimal?)Contar(true, p.Inicio, p.Fin)).ToList() },
                    new SerieDTO { codigo = "salidas", datos = periodos.Select(p => (decimal?)-Contar(false, p.Inicio, p.Fin)).ToList() }
                }
            });

            Dictionary<int, int> tamFinal = new(), tamInicio = new();
            if (esLider)
            {
                r.graficos.Add(new GraficoDTO
                {
                    codigo = "personas_por_lider", tipo = GraficoDTO.Tendencia, invertir = true,
                    series = { new SerieDTO { codigo = "personas_por_lider", datos = finPeriodo.Select(t => (decimal?)Ratio(t)).ToList() } }
                });
            }
            else
            {
                tamFinal = DentroEn(refHasta).GroupBy(x => x.Destino).ToDictionary(g => g.Key, g => g.Count());
                tamInicio = DentroEn(desde.AddTicks(-1)).GroupBy(x => x.Destino).ToDictionary(g => g.Key, g => g.Count());
                var principales = tamFinal.OrderByDescending(x => x.Value).Take(MaxDestinos).Select(x => x.Key).ToList();
                var porPeriodo = finPeriodo.Select(t => DentroEn(t).GroupBy(x => x.Destino).ToDictionary(g => g.Key, g => g.Count())).ToList();

                var grafico = new GraficoDTO { codigo = "por_destino", tipo = GraficoDTO.AreaApilada };
                foreach (var d in principales)
                    grafico.series.Add(new SerieDTO { codigo = $"d{d}", nombre = NombreDe(d), datos = porPeriodo.Select(p => (decimal?)p.GetValueOrDefault(d)).ToList() });
                if (tamFinal.Count > MaxDestinos)
                    grafico.series.Add(new SerieDTO { codigo = "otros", datos = porPeriodo.Select(p => (decimal?)p.Where(x => !principales.Contains(x.Key)).Sum(x => x.Value)).ToList() });
                r.graficos.Add(grafico);

                var cambios = tamFinal.Keys.Union(tamInicio.Keys)
                    .Select(d => (d, c: tamFinal.GetValueOrDefault(d) - tamInicio.GetValueOrDefault(d)))
                    .Where(x => x.c != 0).OrderByDescending(x => Math.Abs(x.c)).Take(8).ToList();
                r.graficos.Add(new GraficoDTO
                {
                    codigo = "cambio_destino", tipo = GraficoDTO.BarrasH, baja = true,
                    categorias = cambios.Select(x => NombreDe(x.d)).ToList(),
                    series = { new SerieDTO { codigo = "cambio", datos = cambios.Select(x => (decimal?)x.c).ToList() } }
                });
            }

            // ── Resumen ──
            var f = r.resumen;
            int total = Personas(refHasta);
            if (crecimiento > 0) f.Add(AnaliticaComun.Frase("pert_crece", "positivo", ("tipo", tipo), ("n", crecimiento), ("total", total)));
            else if (crecimiento < 0) f.Add(AnaliticaComun.Frase("pert_decrece", "negativo", ("tipo", tipo), ("n", -crecimiento), ("total", total)));
            else f.Add(AnaliticaComun.Frase("pert_estable", "neutro", ("tipo", tipo), ("total", total)));

            int entradas = Contar(true, desde, hasta), salidas = Contar(false, desde, hasta);
            if (salidas > entradas && salidas > 0)
                f.Add(AnaliticaComun.Frase("pert_mas_salidas", "alerta", ("tipo", tipo), ("entradas", entradas), ("salidas", salidas)));

            if (esLider)
            {
                var ratio = Ratio(refHasta);
                if (total > 0)
                    f.Add(AnaliticaComun.Frase(ratio > CargaAltaPorLider ? "lid_carga_alta" : "lid_carga", ratio > CargaAltaPorLider ? "alerta" : "neutro",
                                               ("n", ratio), ("max", CargaAltaPorLider)));
                else f.Add(AnaliticaComun.Frase("lid_ninguno", "alerta"));
            }
            else
            {
                var masCrece = tamFinal.Keys.Union(tamInicio.Keys)
                    .Select(d => (d, c: tamFinal.GetValueOrDefault(d) - tamInicio.GetValueOrDefault(d)))
                    .OrderByDescending(x => x.c).ToList();
                if (masCrece.Count > 0 && masCrece[0].c > 0)
                    f.Add(AnaliticaComun.Frase("pert_destino_crece", "positivo", ("tipo", tipo), ("destino", NombreDe(masCrece[0].d)), ("n", masCrece[0].c)));
                if (masCrece.Count > 0 && masCrece[^1].c < 0)
                    f.Add(AnaliticaComun.Frase("pert_destino_cae", "negativo", ("tipo", tipo), ("destino", NombreDe(masCrece[^1].d)), ("n", -masCrece[^1].c)));

                int vacios = nombres.Keys.Count(id => !tamFinal.ContainsKey(id));
                if (vacios > 0) f.Add(AnaliticaComun.Frase("pert_vacios", "alerta", ("tipo", tipo), ("n", vacios)));

                // Personas activas que no están en ninguno: la gente "suelta"
                var dentro = DentroEn(refHasta).Select(x => x.Miembro).ToHashSet();
                var activos = rep.ActivosEn(refHasta);
                int fuera = activos.Count(a => !dentro.Contains(a.Id));
                if (activos.Count > 0 && fuera > 0)
                {
                    var pct = Math.Round(fuera * 100m / activos.Count, 1);
                    f.Add(AnaliticaComun.Frase("pert_sin_asignar", pct >= 50 ? "alerta" : "neutro", ("tipo", tipo), ("n", fuera), ("pct", pct)));
                }
            }

            if (filas.Any(x => x.Aproximado))
                r.nota = AnaliticaComun.Frase("pert_aproximado", "neutro");
            return r;
        }
    }
}
