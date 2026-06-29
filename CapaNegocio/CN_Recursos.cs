using MailKit.Net.Smtp;
using MimeKit;
using System.Security.Cryptography;
using System.Text;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Recursos
    {
        //Generar clave automática que mande al usuario.

        public static string GenerarClave()
        {
            //NewGuid permite generar un nuevo código (método de C#)
            string clave = Guid.NewGuid().ToString("N").Substring(0, 6);
            return clave;
        }

        public static string ConvertirSha256(string texto)
        {
            using var hash = SHA256.Create();  // ✅ más seguro que SHA256Managed
            var bytes = Encoding.UTF8.GetBytes(texto);
            var result = hash.ComputeHash(bytes);

            return BitConverter.ToString(result).Replace("-", "").ToLower();
        }


        public static bool EnviarCorreo(string correo, string asunto, string mensaje)
        {
            bool resultado = false;

            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Soporte", "infosoftweb1910@gmail.com"));
                email.To.Add(MailboxAddress.Parse(correo));
                email.Subject = asunto;
                email.Body = new TextPart("html") { Text = mensaje };

                using (var smtp = new SmtpClient())
                {
                    smtp.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    smtp.Authenticate("infosoftweb1910@gmail.com", "nnqctdavjsrqphqn");
                    smtp.Send(email);
                    smtp.Disconnect(true);
                }

                resultado = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar correo: " + ErrorHelper.Mensaje(ex));
                resultado = false;
            }

            return resultado;
        }
    }
}
