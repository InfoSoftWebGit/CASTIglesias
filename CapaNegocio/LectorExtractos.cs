using System.Globalization;
using System.Text;

namespace CapaNegocio
{
    /// <summary>
    /// Lee un extracto bancario y lo convierte en movimientos.
    /// </summary>
    /// <remarks>
    /// Dos formatos, y la razón de que sean esos dos:
    ///
    ///   - NORMA 43 (also llamada C43 o Q43): es el cuaderno del Consejo Superior
    ///     Bancario que TODOS los bancos españoles saben exportar. Es el formato
    ///     correcto para esto y el que hay que pedirle al banco.
    ///   - CSV genérico: porque la banca electrónica de muchas entidades lo ofrece de
    ///     un clic y una iglesia pequeña no siempre sabe pedir un N43.
    ///
    /// Esta clase NO toca la base de datos: entra texto y salen movimientos. Así se
    /// puede razonar sobre el formato sin mezclarlo con la conciliación, que es donde
    /// está la lógica difícil de verdad.
    /// </remarks>
    public static class LectorExtractos
    {
        /// <summary>Un movimiento leído del fichero, antes de guardarlo.</summary>
        public class MovimientoLeido
        {
            public DateTime fecha_operacion { get; set; }
            public DateTime? fecha_valor { get; set; }
            /// <summary>Positivo entra, negativo sale.</summary>
            public decimal importe { get; set; }
            public string? concepto { get; set; }
            public string? referencia { get; set; }
            public string? contrapartida { get; set; }
        }

        /// <summary>Lo que se ha podido leer de un fichero.</summary>
        public class ExtractoLeido
        {
            public bool correcto { get; set; }
            public string mensaje { get; set; } = "";
            public string? cuenta { get; set; }
            public DateTime? desde { get; set; }
            public DateTime? hasta { get; set; }
            public decimal saldo_inicial { get; set; }
            public decimal saldo_final { get; set; }
            public List<MovimientoLeido> movimientos { get; set; } = new();
        }

        /// <summary>
        /// Decide el formato por el contenido, no por la extensión.
        /// </summary>
        /// <remarks>
        /// Un N43 se reconoce porque su primera línea empieza por "11" y mide 80
        /// caracteres. Mirar el nombre del fichero no vale: los bancos lo descargan con
        /// extensión .txt, .q43, .n43 o sin ninguna.
        /// </remarks>
        public static ExtractoLeido Leer(byte[] contenido, string? nombreFichero)
        {
            // Los N43 vienen en ISO-8859-1 casi siempre; los CSV, en UTF-8 o en ANSI.
            // Se prueba UTF-8 y, si aparece el carácter de sustitución, se reintenta en
            // Latin-1: así las eñes y las tildes no se convierten en interrogantes.
            string texto = Encoding.UTF8.GetString(contenido);
            if (texto.Contains('�'))
                texto = Encoding.Latin1.GetString(contenido);

            var lineas = texto.Replace("\r\n", "\n").Replace('\r', '\n')
                              .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lineas.Length == 0)
                return new ExtractoLeido { mensaje = "El fichero está vacío." };

            bool pareceN43 = lineas[0].Length >= 78 && lineas[0].StartsWith("11");

            return pareceN43 ? LeerNorma43(lineas) : LeerCsv(lineas);
        }

        // --------------------------------------------------------------------
        // Norma 43
        // --------------------------------------------------------------------

        /// <summary>
        /// Lee un fichero en Norma 43.
        /// </summary>
        /// <remarks>
        /// Los registros van por posición fija, no por separadores. Los que importan:
        ///   11 = cabecera de cuenta (cuenta, fechas y saldo inicial)
        ///   22 = movimiento
        ///   23 = texto adicional del movimiento anterior (hasta cinco)
        ///   33 = final de cuenta (saldo final)
        ///
        /// Los importes vienen SIN separador decimal: son 14 dígitos de los que los dos
        /// últimos son los céntimos. Y el signo va aparte, en una posición propia, con
        /// 1 para el Debe (sale dinero) y 2 para el Haber (entra).
        ///
        /// Las fechas son AAMMDD sin siglo. Se interpreta que 00-79 es 2000-2079 y
        /// 80-99 es 1980-1999: un extracto bancario de antes de 1980 no existe.
        /// </remarks>
        private static ExtractoLeido LeerNorma43(string[] lineas)
        {
            var r = new ExtractoLeido();
            MovimientoLeido? ultimo = null;

            try
            {
                foreach (var linea in lineas)
                {
                    if (linea.Length < 2) continue;
                    string tipo = linea.Substring(0, 2);

                    if (tipo == "11" && linea.Length >= 47)
                    {
                        r.cuenta = Trozo(linea, 2, 18);          // entidad + oficina + cuenta
                        r.desde = FechaN43(Trozo(linea, 20, 6));
                        r.hasta = FechaN43(Trozo(linea, 26, 6));
                        r.saldo_inicial = ImporteN43(Trozo(linea, 33, 14), Trozo(linea, 32, 1));
                    }
                    else if (tipo == "22" && linea.Length >= 76)
                    {
                        ultimo = new MovimientoLeido
                        {
                            fecha_operacion = FechaN43(Trozo(linea, 6, 6)) ?? DateTime.Today,
                            fecha_valor = FechaN43(Trozo(linea, 12, 6)),
                            importe = ImporteN43(Trozo(linea, 24, 14), Trozo(linea, 23, 1)),
                            referencia = Trozo(linea, 48, 12).Trim(),
                            concepto = ""
                        };
                        r.movimientos.Add(ultimo);
                    }
                    else if (tipo == "23" && ultimo != null && linea.Length >= 42)
                    {
                        // El texto del movimiento llega en registros aparte y se va
                        // concatenando. Sin esto, el concepto queda vacío y conciliar a
                        // mano se vuelve adivinar.
                        string trozo = (Trozo(linea, 4, 38) + " " +
                                        (linea.Length >= 80 ? Trozo(linea, 42, 38) : "")).Trim();
                        if (trozo.Length > 0)
                            ultimo.concepto = ((ultimo.concepto ?? "") + " " + trozo).Trim();
                    }
                    else if (tipo == "33" && linea.Length >= 73)
                    {
                        // Registro 33 (final de cuenta), por posiciones 1-indexadas:
                        //   21-25 nº apuntes debe · 26-39 total debe
                        //   40-44 nº apuntes haber · 45-58 total haber
                        //   59 signo del saldo final · 60-73 saldo final
                        r.saldo_final = ImporteN43(Trozo(linea, 59, 14), Trozo(linea, 58, 1));
                    }
                }

                // Los conceptos sueltos se dejan limpios de espacios repetidos
                foreach (var m in r.movimientos)
                    m.concepto = string.Join(' ', (m.concepto ?? "")
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries));

                if (r.movimientos.Count == 0)
                {
                    r.mensaje = "El fichero parece una Norma 43 pero no tiene ningún movimiento.";
                    return r;
                }

                r.correcto = true;
                r.mensaje = $"Leídos {r.movimientos.Count} movimientos en formato Norma 43.";
                return r;
            }
            catch (Exception ex)
            {
                r.correcto = false;
                r.mensaje = "El fichero Norma 43 no se ha podido leer: " + ex.Message;
                return r;
            }
        }

        /// <summary>Trozo seguro: si la línea se queda corta, devuelve lo que haya.</summary>
        private static string Trozo(string linea, int desde, int largo)
        {
            if (desde >= linea.Length) return "";
            return linea.Substring(desde, Math.Min(largo, linea.Length - desde));
        }

        private static DateTime? FechaN43(string aammdd)
        {
            if (aammdd.Length < 6 || !aammdd.All(char.IsDigit)) return null;

            int aa = int.Parse(aammdd.Substring(0, 2));
            int mm = int.Parse(aammdd.Substring(2, 2));
            int dd = int.Parse(aammdd.Substring(4, 2));

            if (mm < 1 || mm > 12 || dd < 1 || dd > 31) return null;

            int anio = aa <= 79 ? 2000 + aa : 1900 + aa;
            try { return new DateTime(anio, mm, dd); }
            catch { return null; }
        }

        private static decimal ImporteN43(string digitos, string signo)
        {
            digitos = new string(digitos.Where(char.IsDigit).ToArray());
            if (digitos.Length == 0) return 0;

            decimal valor = decimal.Parse(digitos, CultureInfo.InvariantCulture) / 100m;

            // 1 = Debe (sale dinero de la cuenta), 2 = Haber (entra)
            return signo.Trim() == "1" ? -valor : valor;
        }

        // --------------------------------------------------------------------
        // CSV genérico
        // --------------------------------------------------------------------

        /// <summary>
        /// Lee un CSV sencillo: fecha; concepto; importe; referencia.
        /// </summary>
        /// <remarks>
        /// Se admite punto y coma o coma como separador, y los dos formatos de número
        /// que usan los bancos españoles (1.234,56 y 1234.56). Si la primera línea no
        /// tiene una fecha reconocible en su primera columna, se da por cabecera y se
        /// salta: así da igual que el banco la incluya o no.
        /// </remarks>
        private static ExtractoLeido LeerCsv(string[] lineas)
        {
            var r = new ExtractoLeido();

            char separador = lineas[0].Count(c => c == ';') >= lineas[0].Count(c => c == ',')
                ? ';' : ',';

            int saltadas = 0;
            foreach (var linea in lineas)
            {
                var campos = linea.Split(separador);
                if (campos.Length < 3) { saltadas++; continue; }

                DateTime? fecha = LeerFecha(Limpia(campos[0]));
                if (fecha == null) { saltadas++; continue; }   // cabecera o basura

                // El importe es el último campo que parece un número; así funciona
                // tanto con "fecha;concepto;importe" como con columnas intermedias.
                decimal? importe = null;
                int indiceImporte = -1;
                for (int i = campos.Length - 1; i >= 1; i--)
                {
                    var candidato = LeerImporte(Limpia(campos[i]));
                    if (candidato != null) { importe = candidato; indiceImporte = i; break; }
                }
                if (importe == null) { saltadas++; continue; }

                string concepto = string.Join(" ", campos
                    .Where((c, i) => i > 0 && i != indiceImporte)
                    .Select(Limpia)
                    .Where(c => c.Length > 0));

                r.movimientos.Add(new MovimientoLeido
                {
                    fecha_operacion = fecha.Value,
                    fecha_valor = fecha,
                    importe = importe.Value,
                    concepto = concepto
                });
            }

            if (r.movimientos.Count == 0)
            {
                r.mensaje = "No se ha reconocido ningún movimiento. El CSV debe llevar, "
                          + "por lo menos, una columna de fecha y otra de importe.";
                return r;
            }

            r.desde = r.movimientos.Min(m => m.fecha_operacion);
            r.hasta = r.movimientos.Max(m => m.fecha_operacion);
            r.correcto = true;
            r.mensaje = $"Leídos {r.movimientos.Count} movimientos del CSV"
                      + (saltadas > 0 ? $" ({saltadas} líneas no se han reconocido y se han saltado)." : ".");
            return r;
        }

        private static string Limpia(string campo) => campo.Trim().Trim('"').Trim();

        private static DateTime? LeerFecha(string texto)
        {
            string[] formatos = { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd",
                                  "dd/MM/yy", "d/M/yy", "ddMMyyyy" };
            if (DateTime.TryParseExact(texto, formatos, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out var f))
                return f;
            return null;
        }

        private static decimal? LeerImporte(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;

            texto = texto.Replace("€", "").Replace(" ", "").Trim();
            if (texto.Length == 0) return null;

            // Formato español (1.234,56): el punto es de miles y la coma, decimal.
            // Se distingue por cuál aparece más a la derecha.
            int ultimaComa = texto.LastIndexOf(',');
            int ultimoPunto = texto.LastIndexOf('.');

            if (ultimaComa > ultimoPunto)
                texto = texto.Replace(".", "").Replace(',', '.');
            else
                texto = texto.Replace(",", "");

            return decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                ? v : null;
        }
    }
}
