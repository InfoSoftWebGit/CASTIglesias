/*
 * Descarga de listados a Excel (y a PDF por impresión) con columnas a elegir.
 *
 * Vive en un fichero suelto y no en cada vista porque todas las pantallas de
 * congregantes necesitan lo mismo: un boton, un modal con las columnas, y el
 * volcado de lo que el usuario esta viendo. El layout lo carga para todas las
 * vistas, asi que aqui no se puede dar por hecho que exista ninguna tabla.
 *
 * Lo que se descarga es SIEMPRE lo que hay en pantalla: la pagina actual del
 * DataTable, con su orden y su filtro de busqueda. Es una decision de producto:
 * la descarga sirve para compartir un listado concreto, no para sacar la base de
 * datos entera. De ahi el tope de LIMITE_FILAS.
 *
 * Los textos salen de window.T (los declara el layout), que ya viene traducido.
 */
window.ExportarTabla = (function () {
    'use strict';

    // Mismo tope que ExportacionTablaDTO.LimiteFilas en el servidor. Si cambia
    // uno hay que cambiar el otro: el servidor rechaza la peticion si se pasa.
    var LIMITE_FILAS = 50;

    var URL_EXCEL = '/Exportacion/Excel';
    var ID_MODAL = 'modalExportarColumnas';

    // Configuracion de la ultima tabla sobre la que se abrio el modal. El modal
    // es uno solo para toda la pagina, asi que hay que recordar de quien es.
    var configuracionActiva = null;

    function texto(clave, porDefecto) {
        return (window.T && window.T[clave]) || porDefecto;
    }

    function escaparHtml(valor) {
        return $('<div>').text(valor == null ? '' : valor).html();
    }

    function avisar(titulo, mensaje, icono) {
        if (window.Swal) {
            Swal.fire(titulo, mensaje, icono || 'warning');
        } else {
            alert(mensaje);
        }
    }

    // ── El modal ───────────────────────────────────────────────────────────
    //
    // Se crea una sola vez y se reutiliza. Se construye por JavaScript y no como
    // parcial de Razor para no tener que anadir el mismo bloque de HTML en las
    // once vistas que lo usan.
    function asegurarModal() {
        if (document.getElementById(ID_MODAL)) return;

        var html =
            '<div class="modal fade" id="' + ID_MODAL + '" tabindex="-1" aria-hidden="true">' +
              '<div class="modal-dialog modal-lg modal-dialog-scrollable">' +
                '<div class="modal-content">' +
                  '<div class="modal-header text-white" style="background-color:#1E3A8A;">' +
                    '<h5 class="modal-title"><i class="fas fa-file-excel me-1"></i> ' +
                        escaparHtml(texto('xExportar', 'Exportar')) + '</h5>' +
                    '<button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>' +
                  '</div>' +
                  '<div class="modal-body">' +
                    '<p class="text-muted small mb-2" id="exportarAyuda"></p>' +
                    '<div class="d-flex gap-2 mb-2">' +
                      '<button type="button" class="btn btn-outline-secondary btn-sm" id="btnExportarTodas">' +
                        '<i class="fas fa-check-double me-1"></i>' + escaparHtml(texto('xMarcarTodas', 'Marcar todas')) +
                      '</button>' +
                      '<button type="button" class="btn btn-outline-secondary btn-sm" id="btnExportarNinguna">' +
                        '<i class="fas fa-times me-1"></i>' + escaparHtml(texto('xDesmarcarTodas', 'Desmarcar todas')) +
                      '</button>' +
                    '</div>' +
                    '<div class="row" id="exportarListaColumnas"></div>' +
                  '</div>' +
                  '<div class="modal-footer">' +
                    '<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">' +
                        escaparHtml(texto('cancelar', 'Cancelar')) + '</button>' +
                    '<button type="button" class="btn btn-outline-primary" id="btnExportarImprimir">' +
                      '<i class="fas fa-file-pdf me-1"></i>' + escaparHtml(texto('xImprimirPdf', 'Imprimir o guardar en PDF')) +
                    '</button>' +
                    '<button type="button" class="btn btn-success" id="btnExportarDescargar">' +
                      '<i class="fas fa-download me-1"></i>' + escaparHtml(texto('xDescargarExcel', 'Descargar Excel')) +
                    '</button>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>';

        $('body').append(html);

        $('#btnExportarTodas').on('click', function () {
            $('#exportarListaColumnas input[type=checkbox]').prop('checked', true);
        });
        $('#btnExportarNinguna').on('click', function () {
            $('#exportarListaColumnas input[type=checkbox]').prop('checked', false);
        });
        $('#btnExportarDescargar').on('click', function () { descargar('excel'); });
        $('#btnExportarImprimir').on('click', function () { descargar('pdf'); });
    }

    function abrirModal(configuracion) {
        asegurarModal();
        configuracionActiva = configuracion;

        // El titulo del modal lleva el nombre del listado: el modal es uno solo
        // para toda la pagina y conviene ver de que tabla se esta exportando.
        $('#' + ID_MODAL + ' .modal-title').html(
            '<i class="fas fa-file-excel me-1"></i> ' +
            escaparHtml(texto('xExportar', 'Exportar') + ': ' + (configuracion.titulo || '')));

        var contenedor = $('#exportarListaColumnas').empty();
        configuracion.columnas.forEach(function (columna, indice) {
            // Por defecto se marcan todas salvo las que la vista marque como
            // opcionales (columnas que no estan en la tabla pero si en la ficha).
            var marcada = columna.porDefecto === false ? '' : ' checked';
            contenedor.append(
                '<div class="col-sm-6 col-lg-4">' +
                  '<div class="form-check">' +
                    '<input class="form-check-input" type="checkbox" value="' + indice + '" id="expCol' + indice + '"' + marcada + '>' +
                    '<label class="form-check-label" for="expCol' + indice + '">' + escaparHtml(columna.etiqueta) + '</label>' +
                  '</div>' +
                '</div>');
        });

        var limite = calcularLimite(configuracion);
        $('#exportarAyuda').text(
            window.fmt
                ? window.fmt(texto('xAyudaExportar',
                      'Se descargan como máximo {0} filas: las de la página que estás viendo, con el orden y la búsqueda que tengas aplicados.'), limite)
                : texto('xAyudaExportar', 'Se descarga la página que estás viendo.'));

        // El boton de imprimir solo se ofrece donde la vista lo pida: en los
        // listados largos el PDF del navegador no aporta nada sobre el Excel.
        $('#btnExportarImprimir').toggle(configuracion.pdf === true);

        var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById(ID_MODAL));
        modal.show();
    }

    // ── Datos ──────────────────────────────────────────────────────────────

    function obtenerApi(configuracion) {
        // La tabla se pasa como funcion a proposito: cuando se registra el boton
        // el DataTable puede no estar creado todavia.
        var tabla = typeof configuracion.tabla === 'function' ? configuracion.tabla() : configuracion.tabla;
        if (!tabla) return null;
        return typeof tabla.rows === 'function' ? tabla : $(tabla).DataTable();
    }

    /// Cuantas filas se pueden descargar: las que muestra la paginacion, y nunca
    /// mas de LIMITE_FILAS. Con "Mostrar: todos" (page.len() === -1) manda el tope.
    function calcularLimite(configuracion) {
        var api = obtenerApi(configuracion);
        if (!api) return LIMITE_FILAS;
        var porPagina = api.page.len();
        return porPagina > 0 ? Math.min(porPagina, LIMITE_FILAS) : LIMITE_FILAS;
    }

    function obtenerFilas(configuracion) {
        var api = obtenerApi(configuracion);
        if (!api) return [];

        var datos = api.rows({ page: 'current', search: 'applied', order: 'applied' }).data().toArray();
        return datos.slice(0, calcularLimite(configuracion));
    }

    /// Valor de una celda, siempre como texto plano: lo que se ve en la tabla
    /// puede ser un badge o un enlace, y en una hoja de calculo no sirve.
    function valorCelda(columna, fila) {
        var valor = typeof columna.valor === 'function' ? columna.valor(fila) : fila[columna.clave];
        if (valor == null) return '';
        // Un booleano que llega por "clave" (sin funcion propia) se escribe como
        // Si/No: una celda con "false" dentro no la entiende nadie.
        if (valor === true) return texto('si', 'Sí');
        if (valor === false) return texto('no', 'No');
        return String(valor).replace(/\s+/g, ' ').trim();
    }

    // ── Salida ─────────────────────────────────────────────────────────────

    function descargar(formato) {
        var configuracion = configuracionActiva;
        if (!configuracion) return;

        var indices = $('#exportarListaColumnas input[type=checkbox]:checked')
            .map(function () { return parseInt(this.value, 10); }).get();

        if (indices.length === 0) {
            avisar(texto('mAtencion', 'Atención'), texto('xSinColumnas', 'Marca al menos una columna.'), 'warning');
            return;
        }

        var columnas = indices.map(function (i) { return configuracion.columnas[i]; });
        var filas = obtenerFilas(configuracion);

        if (filas.length === 0) {
            avisar(texto('mAtencion', 'Atención'),
                   texto('xSinFilas', 'No hay filas que exportar en esta página.'), 'info');
            return;
        }

        var cuerpo = {
            Titulo: configuracion.titulo,
            Subtitulo: typeof configuracion.subtitulo === 'function'
                ? configuracion.subtitulo() : (configuracion.subtitulo || null),
            Columnas: columnas.map(function (c) { return c.etiqueta; }),
            Filas: filas.map(function (fila) {
                return columnas.map(function (columna) { return valorCelda(columna, fila); });
            })
        };

        bootstrap.Modal.getOrCreateInstance(document.getElementById(ID_MODAL)).hide();

        if (formato === 'pdf') {
            imprimir(cuerpo);
        } else {
            enviarAExcel(cuerpo);
        }
    }

    function enviarAExcel(cuerpo) {
        // fetch y no $.ajax: la respuesta es un fichero binario, y jQuery no
        // sabe manejar blobs. El token antifalsificacion hay que ponerlo a mano
        // porque el prefiltro del layout solo cubre las llamadas de jQuery.
        var token = $('input[name="__RequestVerificationToken"]').first().val() || '';

        if (window.$ && $.LoadingOverlay) $.LoadingOverlay('show');

        fetch(URL_EXCEL, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(cuerpo)
        }).then(function (respuesta) {
            if (!respuesta.ok) throw new Error('HTTP ' + respuesta.status);
            return respuesta.blob().then(function (blob) {
                return { blob: blob, nombre: nombreDeLaRespuesta(respuesta) };
            });
        }).then(function (resultado) {
            if (window.$ && $.LoadingOverlay) $.LoadingOverlay('hide');
            var url = URL.createObjectURL(resultado.blob);
            var enlace = document.createElement('a');
            enlace.href = url;
            enlace.download = resultado.nombre;
            document.body.appendChild(enlace);
            enlace.click();
            document.body.removeChild(enlace);
            // Se libera con retraso: en algunos navegadores revocar la URL en el
            // mismo tick cancela la descarga que acaba de empezar.
            setTimeout(function () { URL.revokeObjectURL(url); }, 2000);
        }).catch(function (error) {
            if (window.$ && $.LoadingOverlay) $.LoadingOverlay('hide');
            console.error('Error al exportar:', error);
            avisar(texto('error', 'Error'), texto('xErrorDescarga', 'No se pudo generar el fichero.'), 'error');
        });
    }

    /// Nombre del fichero que propone el servidor en Content-Disposition.
    ///
    /// Se mira primero filename*, porque cuando el nombre lleva acentos ("Jovenes"
    /// no, pero "Jóvenes" si) el filename a secas viaja con los caracteres no ASCII
    /// sustituidos y saldria un nombre roto.
    function nombreDeLaRespuesta(respuesta) {
        var cabecera = respuesta.headers.get('Content-Disposition') || '';

        var utf8 = /filename\*=\s*UTF-8''([^;]+)/i.exec(cabecera);
        if (utf8) {
            try { return decodeURIComponent(utf8[1].trim()); } catch (e) { /* sigue abajo */ }
        }

        var simple = /filename=\s*"?([^";]+)"?/i.exec(cabecera);
        return simple ? simple[1].trim() : 'listado.xlsx';
    }

    /*
     * PDF: se imprime desde el navegador en una ventana aparte.
     *
     * No se usa el CSS @media print de la pagina porque el listado esta dentro de
     * un modal y de un contenedor con scroll; una ventana nueva con solo la tabla
     * sale siempre igual y evita arrastrar una libreria de PDF al proyecto (la
     * misma decision que en los informes financieros).
     */
    function imprimir(cuerpo) {
        var ventana = window.open('', '_blank');
        if (!ventana) {
            avisar(texto('mAtencion', 'Atención'),
                   texto('xBloqueoVentana', 'El navegador ha bloqueado la ventana de impresión. Permítela y vuelve a intentarlo.'),
                   'warning');
            return;
        }

        var cabeceras = cuerpo.Columnas.map(function (c) { return '<th>' + escaparHtml(c) + '</th>'; }).join('');
        var filas = cuerpo.Filas.map(function (fila) {
            return '<tr>' + fila.map(function (celda) { return '<td>' + escaparHtml(celda) + '</td>'; }).join('') + '</tr>';
        }).join('');

        ventana.document.write(documentoImprimible(
            cuerpo.Titulo, cuerpo.Subtitulo,
            '<table><thead><tr>' + cabeceras + '</tr></thead><tbody>' + filas + '</tbody></table>'));
        ventana.document.close();
    }

    /// Documento HTML autocontenido para imprimir. Lo usan tanto el modal de
    /// columnas como el informe de encargados, que arma su propio contenido.
    function documentoImprimible(titulo, subtitulo, contenidoHtml) {
        var pie = texto('xGeneradoCon', 'Generado con Congrega CRM');
        return '<!DOCTYPE html><html><head><meta charset="utf-8">' +
            '<title>' + escaparHtml(titulo || '') + '</title>' +
            '<style>' +
            'body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#1f2937;}' +
            'h1{font-size:20px;color:#1E3A8A;margin:0 0 4px;}' +
            '.subtitulo{font-size:12px;color:#64748b;margin:0 0 16px;}' +
            'h2{font-size:14px;color:#1E3A8A;margin:18px 0 6px;page-break-after:avoid;}' +
            'table{width:100%;border-collapse:collapse;margin-bottom:12px;}' +
            'th,td{border:1px solid #cbd5e1;padding:5px 7px;font-size:11px;text-align:left;vertical-align:top;}' +
            'th{background:#1E3A8A;color:#fff;}' +
            'tr{page-break-inside:avoid;}' +
            'tbody tr:nth-child(even){background:#f8fafc;}' +
            '.pie{margin-top:18px;font-size:10px;color:#94a3b8;text-align:right;font-style:italic;}' +
            '@media print{body{margin:10mm;}}' +
            '</style></head><body>' +
            '<h1>' + escaparHtml(titulo || '') + '</h1>' +
            (subtitulo ? '<p class="subtitulo">' + escaparHtml(subtitulo) + '</p>' : '') +
            contenidoHtml +
            '<div class="pie">' + escaparHtml(pie) + ' — ' + new Date().toLocaleString(window.LOCALE || undefined) + '</div>' +
            '<script>window.onload=function(){window.print();}<\/script>' +
            '</body></html>';
    }

    // ── API publica ────────────────────────────────────────────────────────
    return {
        LIMITE_FILAS: LIMITE_FILAS,

        /// Engancha un boton al modal de exportacion de una tabla.
        ///
        /// opciones = {
        ///   boton:      selector del boton,
        ///   tabla:      instancia DataTable o funcion que la devuelve,
        ///   titulo:     nombre del listado (fichero y titulo de la hoja),
        ///   subtitulo:  texto o funcion, opcional,
        ///   columnas:   [{ etiqueta, clave | valor(fila), porDefecto }],
        ///   pdf:        true para ofrecer tambien imprimir
        /// }
        configurar: function (opciones) {
            $(document).on('click', opciones.boton, function () { abrirModal(opciones); });
        },

        /// Imprime un contenido ya montado (lo usa el informe de encargados).
        imprimirHtml: function (titulo, subtitulo, contenidoHtml) {
            var ventana = window.open('', '_blank');
            if (!ventana) {
                avisar(texto('mAtencion', 'Atención'),
                       texto('xBloqueoVentana', 'El navegador ha bloqueado la ventana de impresión. Permítela y vuelve a intentarlo.'),
                       'warning');
                return;
            }
            ventana.document.write(documentoImprimible(titulo, subtitulo, contenidoHtml));
            ventana.document.close();
        },

        /// Fecha en el formato del idioma activo.
        ///
        /// Las fechas llegan del servidor en ISO ("2026-03-14T00:00:00"), que en una
        /// hoja de calculo no se lee. Se corta la parte de la hora en lugar de
        /// construir un Date a secas: con la hora a cero, la conversion a la zona
        /// local puede restar un dia y cambiar la fecha de nacimiento de alguien.
        fecha: function (valor) {
            if (!valor) return '';
            var partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(valor));
            if (!partes) return String(valor);
            var fechaLocal = new Date(+partes[1], +partes[2] - 1, +partes[3]);
            return fechaLocal.toLocaleDateString(window.LOCALE || undefined);
        },

        /// Booleano como "Si"/"No" traducido. Los campos de la BBDD llegan de varias
        /// formas (true, "Si", 1), asi que se admiten todas.
        siNo: function (valor) {
            var afirmativo = valor === true || valor === 1 || valor === '1'
                || (typeof valor === 'string' && valor.trim().toLowerCase() === 'si');
            return afirmativo ? texto('si', 'Sí') : texto('no', 'No');
        }
    };
})();
