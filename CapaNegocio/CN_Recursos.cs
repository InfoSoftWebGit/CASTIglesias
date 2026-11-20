using MailKit.Net.Smtp;
using MimeKit;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Recursos
    {
        // Caracteres de las claves temporales: sin 0/O ni 1/l/I, que se confunden al teclearlas
        private const string CaracteresClave = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        /// <summary>
        /// Genera la clave temporal que se envía al usuario (alta o recuperación).
        /// </summary>
        /// <remarks>
        /// Antes eran 6 caracteres hexadecimales de un GUID: pocas combinaciones y un
        /// generador que no está pensado para secretos. Ahora son 12 caracteres elegidos
        /// con el generador criptográfico del sistema.
        /// </remarks>
        public static string GenerarClave()
        {
            return RandomNumberGenerator.GetString(CaracteresClave, 12);
        }

        #region Hash de contraseñas
        // Formato guardado: pbkdf2-sha256$<iteraciones>$<sal base64>$<hash base64>
        // Las iteraciones van dentro del propio valor para poder subirlas en el futuro
        // sin invalidar las contraseñas ya guardadas.
        private const string PrefijoPbkdf2 = "pbkdf2-sha256";
        private const int Iteraciones = 600_000; // recomendación OWASP para PBKDF2-SHA256
        private const int BytesSal = 16;
        private const int BytesHash = 32;

        /// <summary>
        /// Calcula el hash de una contraseña para guardarlo en usuarios.contrasenia.
        /// </summary>
        /// <remarks>
        /// Sustituye al SHA256 sin sal: con él, dos usuarios con la misma clave tenían el
        /// mismo hash y una clave filtrada se podía descifrar con tablas precalculadas.
        /// PBKDF2 añade una sal aleatoria por usuario y es lento a propósito.
        /// </remarks>
        public static string HashearClave(string clave)
        {
            byte[] sal = RandomNumberGenerator.GetBytes(BytesSal);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, BytesHash);
            return $"{PrefijoPbkdf2}${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Comprueba una contraseña contra el hash guardado.
        /// </summary>
        /// <param name="requiereActualizar">true si la clave es correcta pero está guardada
        /// con el formato antiguo (SHA256) o con menos iteraciones: quien llama debe
        /// guardar un hash nuevo. Así las contraseñas se migran solas al iniciar sesión.</param>
        public static bool VerificarClave(string clave, string? hashGuardado, out bool requiereActualizar)
        {
            requiereActualizar = false;
            if (string.IsNullOrEmpty(clave) || string.IsNullOrEmpty(hashGuardado)) return false;

            var partes = hashGuardado.Split('$');
            if (partes.Length == 4 && partes[0] == PrefijoPbkdf2)
            {
                if (!int.TryParse(partes[1], out int iteraciones) || iteraciones <= 0) return false;
                byte[] sal, esperado;
                try
                {
                    sal = Convert.FromBase64String(partes[2]);
                    esperado = Convert.FromBase64String(partes[3]);
                }
                catch (FormatException)
                {
                    return false;
                }

                byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(clave, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
                bool correcta = CryptographicOperations.FixedTimeEquals(calculado, esperado);
                requiereActualizar = correcta && iteraciones < Iteraciones;
                return correcta;
            }

            // Formato antiguo: SHA256 en hexadecimal (64 caracteres)
            if (hashGuardado.Length == 64)
            {
                byte[] calculadoLegado = Encoding.ASCII.GetBytes(Sha256Hex(clave));
                byte[] guardadoLegado = Encoding.ASCII.GetBytes(hashGuardado.ToLowerInvariant());
                bool correcta = CryptographicOperations.FixedTimeEquals(calculadoLegado, guardadoLegado);
                requiereActualizar = correcta;
                return correcta;
            }

            return false;
        }

        /// <summary>
        /// SHA256 sin sal. Solo se usa para validar las contraseñas antiguas mientras se
        /// migran; no debe usarse para guardar contraseñas nuevas.
        /// </summary>
        [Obsolete("Usar HashearClave / VerificarClave. Solo queda para leer hashes antiguos.")]
        public static string ConvertirSha256(string texto) => Sha256Hex(texto);

        private static string Sha256Hex(string texto)
        {
            var result = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
            return Convert.ToHexString(result).ToLowerInvariant();
        }
        #endregion

        #region Correo
        // La configuración llega desde Program.cs (sección "Correo" de appsettings).
        // Antes el usuario y la contraseña de la cuenta estaban escritos aquí, en el
        // código y en el historial de git; appsettings*.json está fuera del repositorio.
        private static ConfiguracionCorreo? _correo;

        public sealed class ConfiguracionCorreo
        {
            public string Servidor { get; set; } = "smtp.ionos.es";
            public int Puerto { get; set; } = 587;
            public string Usuario { get; set; } = string.Empty;
            public string Clave { get; set; } = string.Empty;
            public string Remitente { get; set; } = string.Empty;
            public string NombreRemitente { get; set; } = "Congrega CRM";

            /// <summary>
            /// Buzón al que se responde si el destinatario contesta al correo. Si está
            /// vacío se responde al remitente.
            /// </summary>
            public string ResponderA { get; set; } = string.Empty;

            /// <summary>
            /// Dirección pública de la aplicación (por ejemplo https://congrega.es), para
            /// poner el enlace de inicio de sesión en los correos. Cada entorno tiene la
            /// suya, así que si está vacía los correos se envían sin enlace.
            /// </summary>
            public string UrlAplicacion { get; set; } = string.Empty;
        }

        /// <summary>Se llama una vez al arrancar la aplicación.</summary>
        public static void ConfigurarCorreo(ConfiguracionCorreo? configuracion) => _correo = configuracion;

        /// <summary>
        /// Enlace al formulario de inicio de sesión, o cadena vacía si no se ha
        /// configurado "Correo:UrlAplicacion".
        /// </summary>
        public static string UrlLogin()
        {
            var baseUrl = _correo?.UrlAplicacion;
            if (string.IsNullOrWhiteSpace(baseUrl)) return string.Empty;
            return baseUrl.TrimEnd('/') + "/Acceso/Login";
        }

        /// <summary>
        /// Cifrado de la conexión según el puerto configurado.
        /// </summary>
        /// <remarks>
        /// IONOS admite 587 con STARTTLS y 465 con TLS directo; Gmail usaba 587. Se
        /// deduce del puerto para no añadir otro valor de configuración que se pueda
        /// quedar descuadrado con él.
        /// </remarks>
        private static MailKit.Security.SecureSocketOptions CifradoSegunPuerto(int puerto) =>
            puerto == 465
                ? MailKit.Security.SecureSocketOptions.SslOnConnect
                : MailKit.Security.SecureSocketOptions.StartTls;

        /// <summary>
        /// Comprueba que se puede enviar correo: conecta con el servidor y se autentica,
        /// pero no envía nada.
        /// </summary>
        /// <remarks>
        /// Se usa antes de dar de alta un usuario. Si la cuenta de correo está caída
        /// (por ejemplo, con la contraseña de aplicación caducada), el usuario se creaba
        /// igualmente y su clave temporal no la conocía nadie: quedaba inaccesible y solo
        /// se podía arreglar por SQL. Vale más no crearlo y avisar.
        /// </remarks>
        public static bool SePuedeEnviarCorreo(out string error)
        {
            error = string.Empty;

            if (_correo == null || string.IsNullOrWhiteSpace(_correo.Usuario) || string.IsNullOrWhiteSpace(_correo.Clave))
            {
                error = "Falta la configuración de la cuenta de correo.";
                return false;
            }

            try
            {
                using var smtp = new SmtpClient();
                smtp.Connect(_correo.Servidor, _correo.Puerto, CifradoSegunPuerto(_correo.Puerto));
                smtp.Authenticate(_correo.Usuario, _correo.Clave);
                smtp.Disconnect(true);
                return true;
            }
            catch (Exception ex)
            {
                error = ErrorHelper.Mensaje(ex);
                Console.WriteLine("La cuenta de correo no responde: " + error);
                return false;
            }
        }

        public static bool EnviarCorreo(string correo, string asunto, string mensaje)
        {
            if (_correo == null || string.IsNullOrWhiteSpace(_correo.Usuario) || string.IsNullOrWhiteSpace(_correo.Clave))
            {
                Console.WriteLine("Error al enviar correo: falta la sección \"Correo\" en appsettings.");
                return false;
            }

            try
            {
                var email = new MimeMessage();
                var remitente = string.IsNullOrWhiteSpace(_correo.Remitente) ? _correo.Usuario : _correo.Remitente;
                email.From.Add(new MailboxAddress(_correo.NombreRemitente, remitente));
                email.To.Add(MailboxAddress.Parse(correo));

                // Si el usuario responde, que la respuesta llegue al buzón de soporte y no
                // a la cuenta técnica que autentica el envío (pueden no ser la misma).
                if (!string.IsNullOrWhiteSpace(_correo.ResponderA))
                    email.ReplyTo.Add(MailboxAddress.Parse(_correo.ResponderA));

                email.Subject = asunto;

                // Cuerpo en HTML y en texto plano a la vez: un correo que solo lleva HTML
                // es una de las señales que más puntúan como spam, y además algunos
                // clientes (y las vistas previas) muestran el texto plano.
                email.Body = new BodyBuilder
                {
                    HtmlBody = mensaje,
                    TextBody = HtmlATextoPlano(mensaje)
                }.ToMessageBody();

                using var smtp = new SmtpClient();
                smtp.Connect(_correo.Servidor, _correo.Puerto, CifradoSegunPuerto(_correo.Puerto));
                smtp.Authenticate(_correo.Usuario, _correo.Clave);
                smtp.Send(email);
                smtp.Disconnect(true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar correo: " + ErrorHelper.Mensaje(ex));
                return false;
            }
        }

        /// <summary>
        /// Versión en texto plano del cuerpo HTML de un correo.
        /// </summary>
        /// <remarks>
        /// No pretende ser un conversor completo: basta con respetar los saltos de línea
        /// de los bloques y quitar las etiquetas, porque todo el HTML de los correos lo
        /// genera la propia aplicación.
        /// </remarks>
        private static string HtmlATextoPlano(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            // El cierre de los bloques y los <br> pasan a ser saltos de línea
            string texto = Regex.Replace(html, @"<\s*(br|/p|/div|/h[1-6]|/li|/tr)\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
            // Las reglas horizontales separan secciones
            texto = Regex.Replace(texto, @"<\s*hr\s*/?\s*>", "\n----------\n", RegexOptions.IgnoreCase);
            // El resto de etiquetas se descarta
            texto = Regex.Replace(texto, @"<[^>]+>", string.Empty);
            // &nbsp;, &amp;, etc. vuelven a su carácter
            texto = WebUtility.HtmlDecode(texto);
            // La sangría del HTML del código fuente deja espacios al principio de línea
            texto = Regex.Replace(texto, @"[ \t]+\n", "\n");
            texto = Regex.Replace(texto, @"\n[ \t]+", "\n");
            // Nunca más de una línea en blanco seguida
            texto = Regex.Replace(texto, @"\n{3,}", "\n\n");
            return texto.Trim();
        }

        /// <summary>
        /// Envuelve el contenido de un correo en la plantilla de Congrega CRM.
        /// </summary>
        /// <remarks>
        /// Los estilos van escritos en cada etiqueta (no en una hoja de estilos) porque
        /// los clientes de correo descartan los bloques &lt;style&gt;. El pie con el
        /// nombre y el correo de contacto también ayuda a no caer en spam: un correo sin
        /// identificación del remitente es sospechoso.
        /// </remarks>
        public static string ComponerCorreo(string titulo, string contenidoHtml, string? pie = null)
        {
            pie ??= "Este es un correo automático del alta y la recuperación de acceso. Si no has solicitado\n    nada, avísanos respondiendo a este mensaje.";
            return $@"
<div style=""font-family:Segoe UI,Arial,sans-serif;font-size:15px;color:#1f2937;line-height:1.6;max-width:560px"">
  <div style=""background:#1E3A8A;border-bottom:3px solid #00BFA5;padding:16px 20px"">
    <span style=""color:#00BFA5;font-size:22px;font-weight:700"">Congrega</span>
    <span style=""color:#ffffff;opacity:0.5;font-size:12px"">CRM</span>
  </div>
  <div style=""padding:20px"">
    <h2 style=""font-size:18px;color:#1E3A8A;margin:0 0 16px 0"">{WebUtility.HtmlEncode(titulo)}</h2>
    {contenidoHtml}
  </div>
  <div style=""padding:0 20px 20px 20px;font-size:12px;color:#6B7280;border-top:1px solid #e5e7eb;padding-top:12px"">
    Congrega CRM &middot; <a href=""mailto:soporte@congrega.es"" style=""color:#1E3A8A"">soporte@congrega.es</a><br/>
    {WebUtility.HtmlEncode(pie)}
  </div>
</div>";
        }
        #endregion
    }
}
