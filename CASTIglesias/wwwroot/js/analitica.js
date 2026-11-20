/*
 * Piezas comunes de los paneles de informes evolutivos.
 *
 * Cada panel (congregantes, asistencia, economía...) trae sus propios datos y
 * decide qué gráficos pinta; aquí está lo que comparten todos para que se vean
 * y se comporten igual:
 *
 *   - Colores por papel (cada estado tiene siempre el mismo color, en todas las
 *     vistas, y la subida/bajada usa los colores de estado bueno/malo).
 *   - Tarjetas de indicador con flecha ▲▼, % frente al periodo anterior y
 *     minigráfico de tendencia.
 *   - Botones de descarga en cada gráfico: imagen PNG y datos CSV (Excel lo abre
 *     tal cual: separador ";" y BOM para que respete los acentos).
 *   - Rangos rápidos (este mes, este año, últimos 12 meses...) y etiquetas de
 *     periodo en el idioma del usuario.
 *   - Imprimir el panel, que en el navegador es también "Guardar como PDF".
 *
 * Requiere Chart.js 4 cargado antes.
 */
(function () {
    'use strict';

    const idioma = (document.documentElement.lang || 'es').slice(0, 2);

    // Paleta validada (orden fijo, nunca rotado). El color sigue a la entidad: un
    // simpatizante es naranja aquí, en el embudo y en las barras de avances.
    const colores = {
        estados: {
            Visitante: '#2a78d6',
            Simpatizante: '#eb6834',
            Proceso: '#1baf7a',
            Miembro: '#eda100'
        },
        sube: '#006300',      // texto/flecha "a mejor"
        baja: '#d03b3b',      // texto/flecha "a peor"
        neutro: '#52514e',
        tinta: '#0b0b0b',
        tintaSecundaria: '#52514e',
        apagado: '#898781',
        rejilla: '#e1e0d9',
        eje: '#c3c2b7',
        superficie: '#ffffff'
    };

    // Para series que no son estados (adultos/niños, conceptos, cuentas...): los
    // mismos ocho colores en orden fijo. Nunca se generan más: lo que sobra va a "Otros".
    colores.serie = ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4', '#008300', '#6250d6', '#e34948'];
    colores.otros = '#898781';

    const fmtNumero = (n, decimales = 0) =>
        Number(n ?? 0).toLocaleString(idioma, { maximumFractionDigits: decimales, minimumFractionDigits: 0 });

    /** Importe en euros; sin céntimos a partir de mil para que las tarjetas no se desborden. */
    const fmtMoneda = n => Number(n ?? 0).toLocaleString(idioma, {
        style: 'currency', currency: 'EUR',
        maximumFractionDigits: Math.abs(n ?? 0) >= 1000 ? 0 : 2, minimumFractionDigits: 0
    });

    /** Sustituye {clave} por su valor en una plantilla de texto. */
    const plantilla = (texto, datos) =>
        String(texto || '').replace(/\{(\w+)\}/g, (_, k) => (datos && datos[k] != null ? datos[k] : ''));

    const escapar = s => String(s ?? '').replace(/[&<>"']/g, c =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

    // ── Fechas y periodos ──────────────────────────────────────────────────

    const aISO = d => {
        const z = n => String(n).padStart(2, '0');
        return `${d.getFullYear()}-${z(d.getMonth() + 1)}-${z(d.getDate())}`;
    };

    /** Rangos rápidos. Devuelve { desde, hasta } como texto yyyy-mm-dd. */
    function rango(codigo) {
        const hoy = new Date();
        const y = hoy.getFullYear(), m = hoy.getMonth();
        const r = (a, b) => ({ desde: aISO(a), hasta: aISO(b) });
        switch (codigo) {
            case 'mes': return r(new Date(y, m, 1), hoy);
            case 'mes_anterior': return r(new Date(y, m - 1, 1), new Date(y, m, 0));
            case 'trimestre': { const t = Math.floor(m / 3) * 3; return r(new Date(y, t, 1), hoy); }
            case 'anio': return r(new Date(y, 0, 1), hoy);
            case 'anio_anterior': return r(new Date(y - 1, 0, 1), new Date(y - 1, 11, 31));
            case '24m': return r(new Date(y, m - 23, 1), hoy);
            default: return r(new Date(y, m - 11, 1), hoy); // 12m
        }
    }

    /**
     * Traduce la etiqueta neutra del servidor (2026-03, 2026-S12, 2026-T1) al
     * idioma del usuario. textos.semana / textos.trimestre son las iniciales (S/W, T/Q).
     */
    function etiquetaPeriodo(etiqueta, textos) {
        const s = /^(\d{4})-S(\d{2})$/.exec(etiqueta);
        if (s) return `${(textos && textos.semana) || 'S'}${+s[2]} ${s[1]}`;
        const t = /^(\d{4})-T(\d)$/.exec(etiqueta);
        if (t) return `${(textos && textos.trimestre) || 'T'}${t[2]} ${t[1]}`;
        const m = /^(\d{4})-(\d{2})$/.exec(etiqueta);
        if (m) {
            const mes = new Date(+m[1], +m[2] - 1, 1).toLocaleDateString(idioma, { month: 'short' }).replace('.', '');
            return `${mes} ${m[1].slice(2)}`;
        }
        return etiqueta;
    }

    const fechaCorta = iso => new Date(iso).toLocaleDateString(idioma, { day: 'numeric', month: 'short', year: 'numeric' });

    // ── Descargas ──────────────────────────────────────────────────────────

    function descargar(nombre, blobOUrl) {
        const a = document.createElement('a');
        a.href = typeof blobOUrl === 'string' ? blobOUrl : URL.createObjectURL(blobOUrl);
        a.download = nombre;
        document.body.appendChild(a);
        a.click();
        a.remove();
        if (typeof blobOUrl !== 'string') setTimeout(() => URL.revokeObjectURL(a.href), 1000);
    }

    const nombreFichero = base => `${String(base).normalize('NFD').replace(/[̀-ͯ]/g, '')
        .replace(/[^\w-]+/g, '_').replace(/_+/g, '_').toLowerCase()}_${aISO(new Date())}`;

    /** CSV para Excel en español: separador ";" y BOM UTF-8. */
    function descargarCSV(nombre, cabeceras, filas) {
        const celda = v => {
            const s = v == null ? '' : String(v);
            return /[";\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
        };
        const texto = [cabeceras, ...filas].map(f => f.map(celda).join(';')).join('\r\n');
        descargar(`${nombreFichero(nombre)}.csv`, new Blob(['﻿' + texto], { type: 'text/csv;charset=utf-8' }));
    }

    function descargarPNG(grafico, nombre) {
        descargar(`${nombreFichero(nombre)}.png`, grafico.toBase64Image('image/png', 1));
    }

    // Fondo blanco: sin él, el PNG sale transparente y en un visor oscuro no se lee
    const pluginFondo = {
        id: 'fondoAnalitica',
        beforeDraw(chart) {
            const { ctx, width, height } = chart;
            ctx.save();
            ctx.globalCompositeOperation = 'destination-over';
            ctx.fillStyle = colores.superficie;
            ctx.fillRect(0, 0, width, height);
            ctx.restore();
        }
    };

    // ── Gráficos ───────────────────────────────────────────────────────────

    const graficos = {};

    /** Opciones base: tipografía del sistema, rejilla discreta, tooltip por índice. */
    function opcionesBase(extra) {
        const base = {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 300 },
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: {
                    position: 'top', align: 'start',
                    labels: { color: colores.tintaSecundaria, usePointStyle: true, pointStyle: 'rectRounded', boxWidth: 10, boxHeight: 10, padding: 14 }
                },
                tooltip: {
                    backgroundColor: '#ffffff', titleColor: colores.tinta, bodyColor: colores.tintaSecundaria,
                    borderColor: 'rgba(11,11,11,0.12)', borderWidth: 1, padding: 10, boxPadding: 4, usePointStyle: true
                }
            },
            scales: {
                x: { grid: { display: false }, border: { color: colores.eje }, ticks: { color: colores.apagado, maxRotation: 0, autoSkipPadding: 12 } },
                y: { beginAtZero: true, grid: { color: colores.rejilla }, border: { display: false }, ticks: { color: colores.apagado, precision: 0 } }
            }
        };
        return fusionar(base, extra || {});
    }

    function fusionar(a, b) {
        for (const k of Object.keys(b)) {
            if (b[k] && typeof b[k] === 'object' && !Array.isArray(b[k]) && a[k] && typeof a[k] === 'object') fusionar(a[k], b[k]);
            else a[k] = b[k];
        }
        return a;
    }

    /**
     * Crea (o recrea) un gráfico en un canvas y engancha los botones de descarga
     * de su tarjeta: [data-descargar="png"] y [data-descargar="csv"].
     * datosCSV: () => ({ cabeceras: [...], filas: [[...], ...] })
     */
    function grafico(idCanvas, config, titulo, datosCSV) {
        if (graficos[idCanvas]) graficos[idCanvas].destroy();
        const canvas = document.getElementById(idCanvas);
        if (!canvas) return null;

        config.plugins = [...(config.plugins || []), pluginFondo];
        const g = new Chart(canvas, config);
        graficos[idCanvas] = g;

        const tarjeta = canvas.closest('.analitica-tarjeta');
        if (tarjeta) {
            tarjeta.querySelectorAll('[data-descargar]').forEach(b => {
                b.onclick = () => {
                    if (b.dataset.descargar === 'png') descargarPNG(g, titulo);
                    else if (datosCSV) { const d = datosCSV(); descargarCSV(titulo, d.cabeceras, d.filas); }
                };
            });
        }
        return g;
    }

    // ── Tarjetas de indicador ──────────────────────────────────────────────

    /**
     * Tarjeta con valor, flecha y minigráfico.
     * kpi: { valor, anterior, variacion, subir_es_bueno, serie }
     * opciones: { titulo, ayuda, textoAnterior, signo (mostrar +), color }
     */
    function tarjetaKpi(contenedor, kpi, opciones) {
        const dif = kpi.valor - kpi.anterior;
        const direccion = dif > 0 ? 'sube' : dif < 0 ? 'baja' : 'igual';
        const esBueno = direccion === 'igual' ? null : (direccion === 'sube') === kpi.subir_es_bueno;
        const flecha = { sube: '▲', baja: '▼', igual: '■' }[direccion];
        const color = esBueno == null ? colores.neutro : esBueno ? colores.sube : colores.baja;
        const pct = kpi.variacion == null ? '' : ` ${fmtNumero(Math.abs(kpi.variacion), 1)} %`;
        const fmt = kpi.formato === 'moneda' ? fmtMoneda
            : kpi.formato === 'porcentaje' ? v => `${fmtNumero(v, 1)} %` : v => fmtNumero(v, 1);
        const valor = (opciones.signo && kpi.valor > 0 ? '+' : '') + fmt(kpi.valor);
        const idSpark = `spark_${opciones.codigo}`;

        const div = document.createElement('div');
        div.className = 'analitica-kpi';
        div.innerHTML = `
            <div class="analitica-kpi-titulo">
                ${opciones.color ? `<span class="analitica-punto" style="background:${opciones.color}"></span>` : ''}
                ${escapar(opciones.titulo)}
                ${opciones.ayuda ? `<i class="fas fa-circle-info text-muted ms-1" title="${escapar(opciones.ayuda)}"></i>` : ''}
            </div>
            <div class="analitica-kpi-valor">${valor}</div>
            <div class="analitica-kpi-delta" style="color:${color}">
                <span aria-hidden="true">${flecha}</span>${pct}
                <span class="analitica-kpi-anterior">${escapar(plantilla(opciones.textoAnterior, { n: fmt(kpi.anterior) }))}</span>
            </div>
            <div class="analitica-kpi-spark"><canvas id="${idSpark}" aria-hidden="true"></canvas></div>`;
        contenedor.appendChild(div);

        if (kpi.serie && kpi.serie.length > 1) {
            if (graficos[idSpark]) graficos[idSpark].destroy();
            graficos[idSpark] = new Chart(document.getElementById(idSpark), {
                type: 'line',
                data: { labels: kpi.serie.map((_, i) => i), datasets: [{ data: kpi.serie, borderColor: color, borderWidth: 2, pointRadius: 0, tension: 0.3, fill: false }] },
                options: {
                    responsive: true, maintainAspectRatio: false, animation: false,
                    plugins: { legend: { display: false }, tooltip: { enabled: false } },
                    scales: { x: { display: false }, y: { display: false } }
                }
            });
        }
    }

    /**
     * Configuración de la línea "estilo trading": cada tramo se pinta verde si
     * sube y rojo si baja, con una línea discontinua de media como referencia.
     */
    // textos.moneda: formatea eje y tooltips en euros.
    // textos.invertir: para gastos, donde bajar es lo bueno (verde al bajar).
    function lineaTendencia(etiquetas, valores, textos) {
        const cfg = lineaTendenciaBase(etiquetas, valores, textos);
        if (textos.invertir) {
            const verde = colores.sube, rojo = colores.baja;
            const cambiar = c => c === verde ? rojo : c === rojo ? verde : c;
            const ds = cfg.data.datasets[0];
            const tramo = ds.segment.borderColor;
            ds.segment.borderColor = ctx => cambiar(tramo(ctx));
            ds.pointBackgroundColor = ds.pointBackgroundColor.map(cambiar);
        }
        if (textos.moneda) aplicarMoneda(cfg.options);
        return cfg;
    }

    /** Eje de valores y tooltips en euros. eje: 'y' normal, 'x' en barras horizontales. */
    function aplicarMoneda(opciones, eje = 'y') {
        fusionar(opciones, {
            scales: { [eje]: { ticks: { callback: v => fmtMoneda(v) } } },
            plugins: { tooltip: { callbacks: { label: ctx => ` ${ctx.dataset.label}: ${fmtMoneda(ctx.parsed[eje])}` } } }
        });
        return opciones;
    }

    function lineaTendenciaBase(etiquetas, valores, textos) {
        // Los periodos sin dato (null) no cuentan para la media ni se pintan como cero
        const conDato = valores.filter(v => v != null);
        const media = conDato.length ? conDato.reduce((a, b) => a + b, 0) / conDato.length : 0;
        const tramo = ctx => ctx.p1.parsed.y > ctx.p0.parsed.y ? colores.sube
            : ctx.p1.parsed.y < ctx.p0.parsed.y ? colores.baja : colores.neutro;
        return {
            type: 'line',
            data: {
                labels: etiquetas,
                datasets: [
                    {
                        label: textos.serie, data: valores, borderWidth: 2, tension: 0,
                        segment: { borderColor: tramo },
                        borderColor: colores.neutro,
                        pointRadius: 4, pointHoverRadius: 6,
                        pointBackgroundColor: valores.map((v, i) => i === 0 ? colores.neutro : v > valores[i - 1] ? colores.sube : v < valores[i - 1] ? colores.baja : colores.neutro),
                        pointBorderColor: colores.superficie, pointBorderWidth: 2,
                        pointStyle: valores.map((v, i) => i === 0 || v === valores[i - 1] ? 'circle' : 'triangle'),
                        pointRotation: valores.map((v, i) => i > 0 && v < valores[i - 1] ? 180 : 0)
                    },
                    {
                        label: textos.media, data: valores.map(() => Math.round(media * 10) / 10),
                        borderColor: colores.apagado, borderDash: [5, 4], borderWidth: 1.5, pointRadius: 0, fill: false
                    }
                ]
            },
            options: opcionesBase({
                plugins: {
                    tooltip: {
                        callbacks: {
                            afterLabel: ctx => {
                                if (ctx.datasetIndex !== 0 || ctx.dataIndex === 0) return '';
                                const previo = valores[ctx.dataIndex - 1];
                                if (previo == null || ctx.parsed.y == null) return '';
                                const d = ctx.parsed.y - previo;
                                const f = textos.moneda ? v => fmtMoneda(Math.abs(v)) : v => fmtNumero(Math.abs(v), 1);
                                return d === 0 ? '' : `${d > 0 ? '▲ +' : '▼ -'}${f(d)} ${textos.vsAnterior}`;
                            }
                        }
                    }
                }
            })
        };
    }

    // ── Resumen del motor de reglas ────────────────────────────────────────

    /**
     * Pinta las frases del resumen. Cada frase llega como { codigo, nivel, datos } y
     * se redacta con la plantilla plantillas[codigo] ({n}, {pct}...).
     * formato: { numeros: [...claves], moneda: [...], fechas: [...], traducir: { clave: fn } }
     */
    function pintarResumen(ul, frases, plantillas, formato, textoVacio) {
        const iconos = { positivo: 'fa-arrow-trend-up', negativo: 'fa-arrow-trend-down', alerta: 'fa-triangle-exclamation', neutro: 'fa-circle-info' };
        if (!frases.length) { ul.innerHTML = `<li class="text-muted">${escapar(textoVacio || '')}</li>`; return; }
        const f = Object.assign({ numeros: ['n', 'ant', 'total', 'dias', 'casos', 'cohorte'], moneda: [], fechas: ['fecha'], traducir: {} }, formato);

        ul.innerHTML = frases.map(fr => {
            // Lo normal: la frase llega ya redactada del servidor (FormateadorAnalitica)
            if (fr.texto) return `<li class="nivel-${fr.nivel}"><i class="fas ${iconos[fr.nivel] || iconos.neutro}" aria-hidden="true"></i><span>${escapar(fr.texto)}</span></li>`;
            const d = Object.assign({}, fr.datos);
            // moneda: lista de claves, o función (codigo, clave) => bool cuando depende de la frase
            const esMoneda = k => typeof f.moneda === 'function' ? f.moneda(fr.codigo, k) : f.moneda.includes(k);
            for (const k of Object.keys(d)) {
                if (esMoneda(k)) d[k] = fmtMoneda(parseFloat(d[k]));
                else if (f.fechas.includes(k)) d[k] = fechaCorta(d[k]);
                else if (k === 'pct') d[k] = fmtNumero(parseFloat(d[k]), 1);
                else if (f.numeros.includes(k)) d[k] = fmtNumero(parseFloat(d[k]), 1);
                if (f.traducir[k]) d[k] = f.traducir[k](d[k]);
            }
            const texto = plantilla(plantillas[fr.codigo] || fr.codigo, d);
            return `<li class="nivel-${fr.nivel}"><i class="fas ${iconos[fr.nivel] || iconos.neutro}" aria-hidden="true"></i><span>${escapar(texto)}</span></li>`;
        }).join('');
    }

    // ── Panel completo: filtros, plegado y carga ───────────────────────────

    /**
     * Engancha un panel estándar (ver Views/Shared/_FiltrosAnalitica.cshtml).
     * cfg: {
     *   prefijo,          // prefijo de los id de los controles (p. ej. "as")
     *   panelId, cuerpoId, botonId,
     *   url,              // endpoint JSON
     *   claveLocal,       // para recordar si estaba abierto
     *   textos: { mostrar, ocultar, cargando, error, rango },
     *   params: () => ({}),        // parámetros extra (turno...)
     *   pintar: datos => {},
     *   descargarTodo: datos => {} // botón "Descargar datos"
     * }
     */
    function panel(cfg) {
        const $ = s => document.getElementById(cfg.prefijo + s);
        const cuerpo = document.getElementById(cfg.cuerpoId);
        const boton = document.getElementById(cfg.botonId);
        const panelEl = document.getElementById(cfg.panelId);
        let ultimo = null, cargado = false;

        const aplicarRango = codigo => {
            if (codigo === 'personalizado') return;
            const r = rango(codigo);
            $('Desde').value = r.desde;
            $('Hasta').value = r.hasta;
        };

        async function cargar() {
            cargado = true;
            const resumen = $('Resumen');
            if (resumen) resumen.innerHTML = `<li class="text-muted"><i class="fas fa-spinner fa-spin"></i> ${escapar(cfg.textos.cargando)}</li>`;
            const q = new URLSearchParams(Object.assign({
                desde: $('Desde').value, hasta: $('Hasta').value, agrupacion: $('Agrupacion').value
            }, cfg.params ? cfg.params() : {}));
            try {
                const resp = await fetch(`${cfg.url}?${q}`, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
                const json = await resp.json();
                if (!json.ok) throw new Error(json.mensaje || json.message || cfg.textos.error);
                ultimo = json.datos;
                if (ultimo.agrupacion && ultimo.agrupacion !== $('Agrupacion').value) $('Agrupacion').value = ultimo.agrupacion;
                const rt = $('RangoTexto');
                if (rt) rt.textContent = plantilla(cfg.textos.rango, {
                    desde: fechaCorta(ultimo.desde), hasta: fechaCorta(ultimo.hasta),
                    desdeAnt: fechaCorta(ultimo.desde_anterior), hastaAnt: fechaCorta(ultimo.hasta_anterior)
                });
                cfg.pintar(ultimo);
            } catch (e) {
                if (resumen) resumen.innerHTML = `<li class="nivel-negativo"><i class="fas fa-triangle-exclamation"></i> ${escapar(e.message || cfg.textos.error)}</li>`;
            }
        }

        $('Rango').addEventListener('change', e => { aplicarRango(e.target.value); cargar(); });
        ['Desde', 'Hasta'].forEach(s => $(s).addEventListener('change', () => { $('Rango').value = 'personalizado'; cargar(); }));
        $('Agrupacion').addEventListener('change', cargar);
        $('Imprimir').addEventListener('click', () => imprimir(panelEl));
        $('DescargarDatos').addEventListener('click', () => ultimo && cfg.descargarTodo && cfg.descargarTodo(ultimo));
        aplicarRango($('Rango').value);

        const texto = boton.querySelector('span');
        cuerpo.addEventListener('shown.bs.collapse', () => {
            texto.textContent = cfg.textos.ocultar;
            try { localStorage.setItem(cfg.claveLocal, '1'); } catch (e) { }
            if (!cargado) cargar();
        });
        cuerpo.addEventListener('hidden.bs.collapse', () => {
            texto.textContent = cfg.textos.mostrar;
            try { localStorage.removeItem(cfg.claveLocal); } catch (e) { }
        });
        let abrir = false;
        try { abrir = localStorage.getItem(cfg.claveLocal) === '1'; } catch (e) { }
        if (abrir) document.addEventListener('DOMContentLoaded', () => bootstrap.Collapse.getOrCreateInstance(cuerpo).show());

        return { cargar, datos: () => ultimo };
    }

    // ── Panel genérico (PanelAnaliticaDTO) ─────────────────────────────────

    /**
     * Pinta un panel genérico: indicadores, gráficos y resumen, todo con los textos
     * que ya trae del servidor. contenedores: { kpis, graficos, resumen, nota }.
     * TC: textos comunes (window.T.analiticaComun).
     */
    function pintarGenerico(d, contenedores, TC, prefijo) {
        const etiquetas = d.periodos.map(p => etiquetaPeriodo(p, TC));
        pintarResumen(contenedores.resumen, d.resumen, {}, {}, TC.sinDatos);

        contenedores.kpis.innerHTML = '';
        d.kpis.forEach(k => tarjetaKpi(contenedores.kpis, k, {
            codigo: `${prefijo}_${k.codigo}`, titulo: k.etiqueta || k.codigo, textoAnterior: TC.antes,
            signo: k.codigo === 'crecimiento'
        }));

        if (contenedores.nota) {
            contenedores.nota.style.display = d.nota ? '' : 'none';
            contenedores.nota.textContent = d.nota ? d.nota.texto : '';
        }

        // Las tarjetas se crean aquí: cada módulo trae los gráficos que necesita
        Object.keys(graficos).filter(id => id.startsWith(prefijo + 'G')).forEach(id => { graficos[id].destroy(); delete graficos[id]; });
        contenedores.graficos.innerHTML = '';
        d.graficos.forEach((g, i) => {
            const id = `${prefijo}G${i}`;
            const tarjeta = document.createElement('div');
            tarjeta.className = 'analitica-tarjeta';
            if (g.ancha) tarjeta.style.gridColumn = '1/-1';
            tarjeta.innerHTML = `
                <div class="analitica-tarjeta-cabecera">
                    <div><h6>${escapar(g.titulo)}</h6>${g.subtitulo ? `<small>${escapar(g.subtitulo)}</small>` : ''}</div>
                    <div class="analitica-acciones analitica-no-imprimir">
                        <button type="button" class="btn btn-outline-secondary" data-descargar="png"><i class="fas fa-image"></i></button>
                        <button type="button" class="btn btn-outline-secondary" data-descargar="csv"><i class="fas fa-table"></i></button>
                    </div>
                </div>
                <div class="analitica-lienzo ${g.baja ? 'bajo' : ''}"><canvas id="${id}" role="img" aria-label="${escapar(g.titulo)}"></canvas></div>`;
            contenedores.graficos.appendChild(tarjeta);
            grafico(id, configGenerica(g, etiquetas), g.titulo, () => csvGenerico(g, etiquetas, TC));
        });
    }

    function colorSerie(s, i) {
        if (colores.estados[s.codigo]) return colores.estados[s.codigo];
        if (s.codigo === 'otros') return colores.otros;
        if (s.codigo === 'anio_anterior') return colores.serie[6];
        if (s.codigo === 'periodo_anterior') return colores.eje;
        if (s.codigo === 'salidas') return colores.baja;
        if (s.codigo === 'entradas') return colores.serie[0];
        return colores.serie[i % colores.serie.length];
    }

    function configGenerica(g, etiquetasPeriodo) {
        const labels = g.categorias || etiquetasPeriodo;
        let cfg;
        switch (g.tipo) {
            case 'tendencia': {
                const [principal, ...resto] = g.series;
                cfg = lineaTendencia(labels, principal.datos, {
                    serie: principal.nombre, media: (window.T.analiticaComun || {}).media || 'Media',
                    vsAnterior: (window.T.analiticaComun || {}).vsAnterior || '', moneda: g.moneda, invertir: g.invertir
                });
                cfg.data.datasets[0].spanGaps = true;
                resto.forEach((s, i) => cfg.data.datasets.push({
                    label: s.nombre, data: s.datos, borderColor: colorSerie(s, i + 6), borderWidth: 1.5,
                    pointRadius: 0, pointHoverRadius: 4, spanGaps: true, fill: false
                }));
                return cfg;
            }
            case 'area_apilada':
                cfg = {
                    type: 'line',
                    data: { labels, datasets: g.series.map((s, i) => ({ label: s.nombre, data: s.datos, borderColor: colorSerie(s, i), backgroundColor: colorSerie(s, i) + 'd9', borderWidth: 2, pointRadius: 0, pointHoverRadius: 5, tension: 0.25, fill: true })) },
                    options: opcionesBase({ scales: { y: { stacked: true } }, plugins: { legend: { reverse: true }, tooltip: { itemSort: (a, b) => b.datasetIndex - a.datasetIndex } } })
                };
                break;
            case 'apiladas':
            case 'divergentes':
                cfg = {
                    type: 'bar',
                    data: { labels, datasets: g.series.map((s, i) => ({ label: s.nombre, data: s.datos, backgroundColor: colorSerie(s, i), borderColor: colores.superficie, borderWidth: 2, borderRadius: 4, borderSkipped: false, maxBarThickness: 42 })) },
                    options: opcionesBase({
                        scales: { x: { stacked: true }, y: { stacked: true, beginAtZero: g.tipo !== 'divergentes' } },
                        plugins: { tooltip: { callbacks: { label: ctx => ` ${ctx.dataset.label}: ${fmtNumero(Math.abs(ctx.parsed.y), 1)}` } } }
                    })
                };
                if (g.tipo === 'divergentes') cfg.options.scales.y.ticks.callback = v => fmtNumero(Math.abs(v));
                break;
            case 'barras_h': {
                const unaSerie = g.series.length === 1;
                cfg = {
                    type: 'bar',
                    data: {
                        labels,
                        datasets: g.series.map((s, i) => ({
                            label: s.nombre, data: s.datos,
                            // Una sola serie con negativos (cambios): azul si suma, rojo si resta
                            backgroundColor: unaSerie ? s.datos.map(v => (v ?? 0) < 0 ? colores.baja : colores.serie[0]) : colorSerie(s, i),
                            borderRadius: 4, borderSkipped: 'start', maxBarThickness: 24
                        }))
                    },
                    options: opcionesBase({
                        indexAxis: 'y',
                        interaction: { mode: 'index', axis: 'y', intersect: false },
                        plugins: { legend: { display: !unaSerie } },
                        scales: {
                            x: { beginAtZero: true, grid: { color: colores.rejilla, display: true }, border: { display: false } },
                            y: { grid: { display: false }, ticks: { color: colores.tintaSecundaria } }
                        }
                    })
                };
                if (g.moneda) aplicarMoneda(cfg.options, 'x');
                return cfg;
            }
            default:
                cfg = { type: 'line', data: { labels, datasets: g.series.map((s, i) => ({ label: s.nombre, data: s.datos, borderColor: colorSerie(s, i), borderWidth: 2, pointRadius: 3 })) }, options: opcionesBase() };
        }
        if (g.moneda) aplicarMoneda(cfg.options);
        return cfg;
    }

    function csvGenerico(g, etiquetasPeriodo, TC) {
        const labels = g.categorias || etiquetasPeriodo;
        return {
            cabeceras: [g.categorias ? '' : TC.periodo, ...g.series.map(s => s.nombre)],
            filas: labels.map((l, i) => [l, ...g.series.map(s => s.datos[i] ?? '')])
        };
    }

    // ── Imprimir / PDF ─────────────────────────────────────────────────────

    /** Imprime solo el panel indicado (el navegador ofrece "Guardar como PDF"). */
    function imprimir(panel) {
        panel.classList.add('analitica-imprimiendo');
        document.body.classList.add('analitica-modo-impresion');
        const limpiar = () => {
            panel.classList.remove('analitica-imprimiendo');
            document.body.classList.remove('analitica-modo-impresion');
            window.removeEventListener('afterprint', limpiar);
        };
        window.addEventListener('afterprint', limpiar);
        // Un instante para que Chart.js recoloque los lienzos al ancho de papel
        setTimeout(() => window.print(), 150);
    }

    window.Analitica = {
        idioma, colores, fmtNumero, fmtMoneda, plantilla, escapar, rango, etiquetaPeriodo, fechaCorta,
        opcionesBase, grafico, tarjetaKpi, lineaTendencia, aplicarMoneda, descargarCSV, descargarPNG, imprimir,
        pintarResumen, panel, pintarGenerico
    };
})();
