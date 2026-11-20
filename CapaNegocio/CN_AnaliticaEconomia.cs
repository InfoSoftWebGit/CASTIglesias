using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Evolución de ingresos (diezmos y ofrendas) y gastos de la congregación.
    /// </summary>
    /// <remarks>
    /// Además del importe se mira la PARTICIPACIÓN: cuántas personas aportan, cuántas
    /// empiezan y cuántas dejan de hacerlo. Un mes con un donativo grande puede
    /// esconder que cada vez aporta menos gente, y eso es lo que más interesa saber.
    ///
    /// Los datos salen del registro de diezmos y gastos de Economía, no de la
    /// contabilidad del área financiera (que tiene su propio panel).
    /// </remarks>
    public class CN_AnaliticaEconomia
    {
        private readonly CD_AnaliticaEconomia _cd;

        public CN_AnaliticaEconomia(CD_AnaliticaEconomia cd) => _cd = cd;

        /// <summary>Conceptos que se muestran por separado; el resto va a "Otros".</summary>
        private const int MaxConceptos = 5;
        public const string Otros = "Otros";

        public AnaliticaEconomiaDTO Calcular(int sedeID, DateTime desde, DateTime hasta, string? agrupacion,
                                             bool ingresos, bool gastos)
        {
            var (d, h) = AnaliticaComun.NormalizarRango(desde, hasta);
            var (dAnt, _) = AnaliticaComun.PeriodoAnterior(d, h);
            var inicio = dAnt < d.AddYears(-1) ? dAnt : d.AddYears(-1);

            return Calcular(
                ingresos ? _cd.ListarAportaciones(sedeID, h) : null,
                gastos ? _cd.ListarGastos(sedeID, inicio, h) : null,
                d, h, agrupacion);
        }

        /// <summary>
        /// El cálculo sin base de datos. Una lista null significa "sin permiso para
        /// esa parte": no se calcula ni se devuelve nada de ella.
        /// </summary>
        public static AnaliticaEconomiaDTO Calcular(List<AportacionDTO>? aportaciones, List<GastoAnaliticaDTO>? gastos,
                                                   DateTime desde, DateTime hasta, string? agrupacion)
        {
            (desde, hasta) = AnaliticaComun.NormalizarRango(desde, hasta);
            agrupacion = AnaliticaComun.NormalizarAgrupacion(agrupacion);
            var periodos = AnaliticaComun.Periodos(desde, hasta, ref agrupacion);
            var (desdeAnt, hastaAnt) = AnaliticaComun.PeriodoAnterior(desde, hasta);

            var r = new AnaliticaEconomiaDTO
            {
                desde = desde,
                hasta = hasta,
                desde_anterior = desdeAnt,
                hasta_anterior = hastaAnt,
                agrupacion = agrupacion,
                incluye_ingresos = aportaciones != null,
                incluye_gastos = gastos != null
            };

            var ap = aportaciones ?? new();
            var gs = gastos ?? new();
            List<AportacionDTO> Aport(DateTime a, DateTime b) => ap.Where(x => x.fecha >= a && x.fecha <= b).ToList();
            List<GastoAnaliticaDTO> Gast(DateTime a, DateTime b) => gs.Where(x => x.fecha >= a && x.fecha <= b).ToList();

            var apActual = Aport(desde, hasta);
            var gsActual = Gast(desde, hasta);

            // Conceptos y cuentas por importe en el rango; los pequeños se pliegan en "Otros"
            // para que el gráfico apilado no tenga quince colores.
            r.conceptos = Principales(apActual.GroupBy(x => Nombre(x.concepto)).ToDictionary(g => g.Key, g => g.Sum(x => x.cantidad)));
            r.cuentas = Principales(gsActual.GroupBy(x => Nombre(x.cuenta)).ToDictionary(g => g.Key, g => g.Sum(x => x.cantidad)));
            string Plegar(string nombre, List<string> principales) => principales.Contains(nombre) ? nombre : Otros;

            foreach (var p in periodos)
            {
                var a = Aport(p.Inicio, p.Fin);
                var g = Gast(p.Inicio, p.Fin);
                r.periodos.Add(new PeriodoEconomiaDTO
                {
                    etiqueta = p.Etiqueta,
                    inicio = p.Inicio,
                    fin = p.Fin,
                    ingresos = a.Sum(x => x.cantidad),
                    gastos = g.Sum(x => x.cantidad),
                    donantes = Donantes(a),
                    ingresos_por_concepto = a.GroupBy(x => Plegar(Nombre(x.concepto), r.conceptos)).ToDictionary(x => x.Key, x => x.Sum(y => y.cantidad)),
                    gastos_por_cuenta = g.GroupBy(x => Plegar(Nombre(x.cuenta), r.cuentas)).ToDictionary(x => x.Key, x => x.Sum(y => y.cantidad))
                });
                r.ingresos_anio_anterior.Add(Aport(p.Inicio.AddYears(-1), p.Fin.AddYears(-1)).Sum(x => x.cantidad));
                r.gastos_anio_anterior.Add(Gast(p.Inicio.AddYears(-1), p.Fin.AddYears(-1)).Sum(x => x.cantidad));
            }

            r.gastos_por_zona = gsActual
                .GroupBy(x => string.IsNullOrWhiteSpace(x.zona) ? "" : x.zona)
                .Select(g => new ImporteDTO { nombre = g.Key, importe = g.Sum(x => x.cantidad) })
                .OrderByDescending(x => x.importe).Take(8).ToList();

            var apAnterior = Aport(desdeAnt, hastaAnt);
            var gsAnterior = Gast(desdeAnt, hastaAnt);

            if (r.incluye_ingresos)
            {
                var (nuevos, nuevosAnt) = (DonantesNuevos(ap, desde, hasta), DonantesNuevos(ap, desdeAnt, hastaAnt));
                r.kpis.Add(AnaliticaComun.Kpi("ingresos", apActual.Sum(x => x.cantidad), apAnterior.Sum(x => x.cantidad),
                                              r.periodos.Select(p => p.ingresos), formato: "moneda"));
                r.kpis.Add(AnaliticaComun.Kpi("donantes", Donantes(apActual), Donantes(apAnterior), r.periodos.Select(p => (decimal)p.donantes)));
                r.kpis.Add(AnaliticaComun.Kpi("donantes_nuevos", nuevos, nuevosAnt, r.periodos.Select(p => (decimal)DonantesNuevos(ap, p.inicio, p.fin))));
                r.kpis.Add(AnaliticaComun.Kpi("aportacion_media", PorDonante(apActual), PorDonante(apAnterior),
                                              r.periodos.Select(p => p.donantes == 0 ? 0 : Math.Round(p.ingresos / p.donantes, 2)), formato: "moneda"));
            }
            if (r.incluye_gastos)
            {
                r.kpis.Add(AnaliticaComun.Kpi("gastos", gsActual.Sum(x => x.cantidad), gsAnterior.Sum(x => x.cantidad),
                                              r.periodos.Select(p => p.gastos), subirEsBueno: false, formato: "moneda"));
                r.kpis.Add(AnaliticaComun.Kpi("num_gastos", gsActual.Count, gsAnterior.Count, Enumerable.Empty<decimal>(), subirEsBueno: false));
                r.kpis.Add(AnaliticaComun.Kpi("gasto_mayor", gsActual.Count == 0 ? 0 : gsActual.Max(x => x.cantidad),
                                              gsAnterior.Count == 0 ? 0 : gsAnterior.Max(x => x.cantidad), Enumerable.Empty<decimal>(),
                                              subirEsBueno: false, formato: "moneda"));
            }
            if (r.incluye_ingresos && r.incluye_gastos)
            {
                r.kpis.Add(AnaliticaComun.Kpi("balance",
                    apActual.Sum(x => x.cantidad) - gsActual.Sum(x => x.cantidad),
                    apAnterior.Sum(x => x.cantidad) - gsAnterior.Sum(x => x.cantidad),
                    r.periodos.Select(p => p.ingresos - p.gastos), formato: "moneda"));
            }

            r.resumen = Resumen(r, ap, apActual, apAnterior, Aport(desde.AddYears(-1), hasta.AddYears(-1)), desde, hasta, desdeAnt, hastaAnt);
            return r;
        }

        private static string Nombre(string? s) => string.IsNullOrWhiteSpace(s) ? Otros : s.Trim();

        private static List<string> Principales(Dictionary<string, decimal> importes)
        {
            var orden = importes.OrderByDescending(x => x.Value).Select(x => x.Key).Where(k => k != Otros).ToList();
            var lista = orden.Take(MaxConceptos).ToList();
            if (orden.Count > MaxConceptos || importes.ContainsKey(Otros)) lista.Add(Otros);
            return lista;
        }

        private static int Donantes(List<AportacionDTO> lista) => lista.Where(x => x.id_miembro != null).Select(x => x.id_miembro).Distinct().Count();

        private static decimal PorDonante(List<AportacionDTO> lista)
        {
            var n = Donantes(lista);
            return n == 0 ? 0 : Math.Round(lista.Where(x => x.id_miembro != null).Sum(x => x.cantidad) / n, 2);
        }

        /// <summary>Personas cuya primera aportación registrada cae en el rango.</summary>
        private static int DonantesNuevos(List<AportacionDTO> todas, DateTime desde, DateTime hasta) =>
            todas.Where(x => x.id_miembro != null)
                 .GroupBy(x => x.id_miembro)
                 .Count(g => { var primera = g.Min(x => x.fecha); return primera >= desde && primera <= hasta; });

        /// <summary>Aportaron en el periodo anterior y no en este.</summary>
        private static int DonantesPerdidos(List<AportacionDTO> actual, List<AportacionDTO> anterior)
        {
            var ahora = actual.Where(x => x.id_miembro != null).Select(x => x.id_miembro).ToHashSet();
            return anterior.Where(x => x.id_miembro != null).Select(x => x.id_miembro).Distinct().Count(id => !ahora.Contains(id));
        }

        #region Motor de reglas

        private const decimal Umbral = 10m;

        private static List<FraseResumenDTO> Resumen(AnaliticaEconomiaDTO r, List<AportacionDTO> todas,
            List<AportacionDTO> apActual, List<AportacionDTO> apAnterior, List<AportacionDTO> apHaceUnAnio,
            DateTime desde, DateTime hasta, DateTime desdeAnt, DateTime hastaAnt)
        {
            var f = new List<FraseResumenDTO>();
            var k = r.kpis.ToDictionary(x => x.codigo);

            if (r.incluye_ingresos)
            {
                var ing = k["ingresos"];
                if (ing.valor == 0) f.Add(AnaliticaComun.Frase("eco_sin_ingresos", "alerta"));
                else if (ing.anterior > 0)
                {
                    var v = ing.variacion ?? 0;
                    f.Add(v >= Umbral ? AnaliticaComun.Frase("eco_ingresos_sube", "positivo", ("n", ing.valor), ("pct", v), ("ant", ing.anterior))
                        : v <= -Umbral ? AnaliticaComun.Frase("eco_ingresos_baja", "negativo", ("n", ing.valor), ("pct", Math.Abs(v)), ("ant", ing.anterior))
                        : AnaliticaComun.Frase("eco_ingresos_estable", "neutro", ("n", ing.valor), ("ant", ing.anterior)));
                }

                var anio = apHaceUnAnio.Sum(x => x.cantidad);
                if (anio > 0 && ing.valor > 0)
                {
                    var v = Math.Round((ing.valor - anio) * 100m / anio, 1);
                    if (Math.Abs(v) >= Umbral)
                        f.Add(AnaliticaComun.Frase(v > 0 ? "eco_anio_sube" : "eco_anio_baja", v > 0 ? "positivo" : "negativo", ("pct", Math.Abs(v)), ("ant", anio)));
                }

                var don = k["donantes"];
                if (don.valor > 0)
                    f.Add(AnaliticaComun.Frase("eco_donantes", don.valor >= don.anterior ? "positivo" : "negativo", ("n", don.valor), ("ant", don.anterior)));
                var nuevos = k["donantes_nuevos"].valor;
                if (nuevos > 0) f.Add(AnaliticaComun.Frase("eco_donantes_nuevos", "positivo", ("n", nuevos)));
                var perdidos = DonantesPerdidos(apActual, apAnterior);
                if (perdidos > 0) f.Add(AnaliticaComun.Frase("eco_donantes_perdidos", "alerta", ("n", perdidos)));

                var racha = AnaliticaComun.Racha(r.periodos.Select(p => p.ingresos).ToList());
                if (r.periodos.Count >= 4 && racha <= -3) f.Add(AnaliticaComun.Frase("eco_tendencia_baja", "alerta", ("n", -racha)));

                // Dependencia de un solo concepto (normalmente el diezmo): solo informativo
                var porConcepto = apActual.GroupBy(x => Nombre(x.concepto)).Select(g => (g.Key, Total: g.Sum(x => x.cantidad)))
                                          .OrderByDescending(x => x.Total).FirstOrDefault();
                if (ing.valor > 0 && porConcepto.Key != null)
                    f.Add(AnaliticaComun.Frase("eco_concepto_principal", "neutro", ("concepto", porConcepto.Key),
                                               ("pct", Math.Round(porConcepto.Total * 100m / ing.valor, 1))));
            }

            if (r.incluye_gastos)
            {
                var gas = k["gastos"];
                if (gas.anterior > 0 && gas.valor > 0)
                {
                    var v = gas.variacion ?? 0;
                    if (v >= Umbral) f.Add(AnaliticaComun.Frase("eco_gastos_sube", "negativo", ("n", gas.valor), ("pct", v), ("ant", gas.anterior)));
                    else if (v <= -Umbral) f.Add(AnaliticaComun.Frase("eco_gastos_baja", "positivo", ("n", gas.valor), ("pct", Math.Abs(v)), ("ant", gas.anterior)));
                }
                var zona = r.gastos_por_zona.FirstOrDefault();
                if (zona != null && gas.valor > 0)
                    f.Add(AnaliticaComun.Frase(zona.nombre == "" ? "eco_zona_sin" : "eco_zona_principal", "neutro",
                                               ("zona", zona.nombre), ("n", zona.importe), ("pct", Math.Round(zona.importe * 100m / gas.valor, 1))));
            }

            if (r.incluye_ingresos && r.incluye_gastos)
            {
                var bal = k["balance"];
                if (bal.valor < 0) f.Add(AnaliticaComun.Frase("eco_balance_negativo", "alerta", ("n", Math.Abs(bal.valor))));
                else if (bal.valor > 0) f.Add(AnaliticaComun.Frase("eco_balance_positivo", "positivo", ("n", bal.valor)));
            }

            return f;
        }

        #endregion
    }
}
