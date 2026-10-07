using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

namespace CapaNegocio
{
    /// <summary>
    /// Reglas de las inscripciones al webinar de presentación de Congrega CRM.
    /// </summary>
    public class CN_Webinar
    {
        private readonly CD_Webinar _capaDatos;

        public CN_Webinar(CD_Webinar capaDatos)
        {
            _capaDatos = capaDatos;
        }

        /// <summary>
        /// Buzón al que llega el aviso interno de cada inscripción.
        /// </summary>
        private const string CorreoSoporte = "soporte@congrega.es";

        /// <summary>
        /// Sesiones convocadas, en el orden en que se muestran en la página.
        /// </summary>
        /// <remarks>
        /// Están aquí y no en la vista porque la validación también las necesita:
        /// el formulario manda el índice de la sesión elegida y el servidor lo
        /// traduce a fecha con esta lista. Así nadie puede inscribirse a una
        /// fecha inventada manipulando la petición.
        /// </remarks>
        public static readonly DateTime[] Sesiones =
        {
            new DateTime(2026, 10, 23, 19, 0, 0),
            new DateTime(2026, 10, 24, 19, 0, 0)
        };

        /// <summary>
        /// Traduce el índice que manda el formulario a la fecha de la sesión.
        /// </summary>
        /// <returns>La fecha, o null si el índice no corresponde a ninguna sesión.</returns>
        public static DateTime? SesionPorIndice(int indice)
        {
            if (indice < 0 || indice >= Sesiones.Length) return null;
            return Sesiones[indice];
        }

        /// <summary>
        /// Valida y guarda una inscripción, y avisa por correo.
        /// </summary>
        /// <param name="obj">Datos del formulario, con fecha_sesion ya resuelta.</param>
        /// <param name="mensaje">Motivo del rechazo, o el aviso a mostrar si todo fue bien.</param>
        /// <returns>true si la inscripción quedó registrada (o ya lo estaba).</returns>
        public bool Inscribir(WebinarInscripcion obj, out string mensaje)
        {
            mensaje = string.Empty;

            // Normalizar antes de validar: así "  Ana " y "ANA@X.COM " no entran
            // con espacios ni impiden detectar un correo repetido por mayúsculas.
            obj.nombre         = Limpiar(obj.nombre);
            obj.apellido       = Limpiar(obj.apellido);
            obj.telefono       = Limpiar(obj.telefono);
            obj.correo         = Limpiar(obj.correo)?.ToLowerInvariant();
            obj.nombre_iglesia = Limpiar(obj.nombre_iglesia);
            obj.denominacion   = Limpiar(obj.denominacion);
            obj.pregunta       = Limpiar(obj.pregunta);

            if (string.IsNullOrEmpty(obj.nombre))
            {
                mensaje = "El nombre es obligatorio.";
                return false;
            }

            if (string.IsNullOrEmpty(obj.apellido))
            {
                mensaje = "El apellido es obligatorio.";
                return false;
            }

            if (string.IsNullOrEmpty(obj.correo) || !CorreoValido(obj.correo))
            {
                mensaje = "El correo electrónico no es válido.";
                return false;
            }

            if (string.IsNullOrEmpty(obj.telefono))
            {
                mensaje = "El teléfono es obligatorio.";
                return false;
            }

            if (string.IsNullOrEmpty(obj.nombre_iglesia))
            {
                mensaje = "El nombre de la iglesia es obligatorio.";
                return false;
            }

            // La fecha la resuelve el controlador a partir del índice; si llega
            // vacía o cambiada es que la petición no viene del formulario.
            if (!Sesiones.Contains(obj.fecha_sesion))
            {
                mensaje = "Selecciona una de las fechas disponibles.";
                return false;
            }

            // Una pregunta muy larga suele ser un pegado accidental o spam, y la
            // columna tiene un límite: se recorta en lugar de fallar al guardar.
            if (obj.pregunta != null && obj.pregunta.Length > 1000)
                obj.pregunta = obj.pregunta.Substring(0, 1000);

            // Reenvío del mismo formulario: se da por buena la inscripción que ya
            // existe y no se vuelve a escribir ni a avisar, para que nadie reciba
            // dos confirmaciones ni aparezca dos veces en la lista de asistentes.
            var yaInscrito = _capaDatos.BuscarPorCorreoYSesion(obj.correo, obj.fecha_sesion);
            if (yaInscrito != null)
            {
                mensaje = "Ya estabas inscrito a esta sesión con este correo.";
                return true;
            }

            obj.fecha_alta = DateTime.Now;
            obj.enlace_enviado = false;

            int id = _capaDatos.Registrar(obj, out string mensajeDatos);
            if (id == 0)
            {
                mensaje = mensajeDatos;
                return false;
            }

            // Los correos van después de guardar y sin condicionar el resultado:
            // si el SMTP falla, la inscripción ya está en la base de datos y se
            // puede recuperar, mientras que devolver error haría que la persona
            // volviese a rellenar el formulario para nada.
            EnviarAvisoInterno(obj);
            EnviarConfirmacion(obj);

            return true;
        }

        /// <summary>
        /// Lista las inscripciones de una sesión, o todas si no se indica.
        /// </summary>
        public List<WebinarInscripcion> Listar(DateTime? fechaSesion = null) => _capaDatos.Listar(fechaSesion);

        /// <summary>
        /// Número de inscritos por sesión, en el mismo orden que <see cref="Sesiones"/>.
        /// </summary>
        public int[] ContarPorSesion() => Sesiones.Select(f => _capaDatos.Contar(f)).ToArray();

        private static string? Limpiar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            return valor.Trim();
        }

        /// <summary>
        /// Comprueba que el correo tenga forma de correo.
        /// </summary>
        /// <remarks>
        /// Se usa MailAddress en vez de una expresión regular propia: aquí solo
        /// interesa descartar erratas evidentes, porque la dirección real se
        /// confirma sola cuando llegue (o no llegue) el correo de confirmación.
        /// </remarks>
        private static bool CorreoValido(string correo)
        {
            try
            {
                var direccion = new System.Net.Mail.MailAddress(correo);
                return direccion.Address == correo && correo.Contains('.');
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Texto de la sesión para los correos (siempre en español).
        /// </summary>
        private static string TextoSesion(DateTime fecha)
        {
            var es = new CultureInfo("es-ES");
            // Capitalizar el día: en español sale en minúscula y en medio de una
            // frase de correo queda mal ("el viernes" sí, "Viernes 23" también).
            string texto = fecha.ToString("dddd d 'de' MMMM 'de' yyyy, HH:mm", es);
            return char.ToUpper(texto[0], es) + texto.Substring(1) + "h (hora de España)";
        }

        private void EnviarAvisoInterno(WebinarInscripcion obj)
        {
            string asunto = $"Webinar – nueva inscripción: {obj.nombre} {obj.apellido} ({obj.nombre_iglesia})";

            // Se escapa todo lo que escribe el usuario: el cuerpo es HTML y un
            // nombre con "<" rompería el correo, además de ser una vía para
            // colar etiquetas en el buzón de soporte.
            string cuerpo = $@"
                <h3>Nueva inscripción al webinar</h3>
                <p><strong>Sesión:</strong> {E(TextoSesion(obj.fecha_sesion))}</p>
                <p><strong>Nombre:</strong> {E(obj.nombre)} {E(obj.apellido)}</p>
                <p><strong>Correo:</strong> {E(obj.correo)}</p>
                <p><strong>Teléfono:</strong> {E(obj.telefono)}</p>
                <p><strong>Iglesia:</strong> {E(obj.nombre_iglesia)}</p>
                <p><strong>Denominación:</strong> {E(obj.denominacion ?? "—")}</p>
                <p><strong>Idioma del formulario:</strong> {E(obj.idioma ?? "—")}</p>
                <hr/>
                <p><strong>Pregunta que trae:</strong></p>
                <p>{E(obj.pregunta ?? "— (no ha dejado pregunta)")}</p>";

            CN_Recursos.EnviarCorreo(CorreoSoporte, asunto, cuerpo);
        }

        private void EnviarConfirmacion(WebinarInscripcion obj)
        {
            string asunto = "Inscripción confirmada – Webinar de Congrega CRM";

            string cuerpo = $@"
                <p>Hola {E(obj.nombre)},</p>
                <p>Tu plaza para el webinar de <strong>Congrega CRM</strong> está reservada.</p>
                <p><strong>Fecha:</strong> {E(TextoSesion(obj.fecha_sesion))}<br/>
                   <strong>Duración:</strong> máximo 2 horas</p>
                <p>Veremos la aplicación en funcionamiento, explicaremos cómo se usa cada
                   módulo y dejaremos un espacio final para preguntas y respuestas.</p>
                <p><strong>Unos días antes del webinar te enviaremos a este mismo correo
                   el enlace para entrar a la reunión de Microsoft Teams.</strong>
                   No necesitas tener Teams instalado: se puede entrar desde el navegador.</p>
                <p>Si no puedes asistir o quieres cambiar de fecha, respóndenos a este correo.</p>
                <p>Un saludo,<br/>El equipo de Congrega CRM<br/>soporte@congrega.es</p>";

            CN_Recursos.EnviarCorreo(obj.correo!, asunto, cuerpo);
        }

        /// <summary>Escapa un texto para insertarlo en el HTML de un correo.</summary>
        private static string E(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);
    }
}
