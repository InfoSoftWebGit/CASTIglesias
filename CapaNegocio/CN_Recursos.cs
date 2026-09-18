using MailKit.Net.Smtp;
using MimeKit;
using System.Security.Cryptography;
using System.Text;
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
            public string Servidor { get; set; } = "smtp.gmail.com";
            public int Puerto { get; set; } = 587;
            public string Usuario { get; set; } = string.Empty;
            public string Clave { get; set; } = string.Empty;
            public string Remitente { get; set; } = string.Empty;
            public string NombreRemitente { get; set; } = "Soporte";
        }

        /// <summary>Se llama una vez al arrancar la aplicación.</summary>
        public static void ConfigurarCorreo(ConfiguracionCorreo? configuracion) => _correo = configuracion;

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
                smtp.Connect(_correo.Servidor, _correo.Puerto, MailKit.Security.SecureSocketOptions.StartTls);
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
                email.Subject = asunto;
                email.Body = new TextPart("html") { Text = mensaje };

                using var smtp = new SmtpClient();
                smtp.Connect(_correo.Servidor, _correo.Puerto, MailKit.Security.SecureSocketOptions.StartTls);
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
        #endregion
    }
}
