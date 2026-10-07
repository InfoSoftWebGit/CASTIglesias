using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CapaDatos
{
    /// <summary>
    /// Evita que la misma petición se ejecute dos veces (tabla <c>idempotency_keys</c>).
    /// </summary>
    /// <remarks>
    /// El caso real: el tesorero pulsa "Guardar" y, como tarda un segundo, vuelve a
    /// pulsar. Sin esto quedan dos gastos idénticos con dos números distintos, y el
    /// segundo hay que descubrirlo y borrarlo a mano. Con dinero, un duplicado no es
    /// una molestia: descuadra la caja.
    ///
    /// Cómo funciona: el formulario genera una clave al abrirse y la manda con cada
    /// intento de guardado. La primera vez se graba la clave junto con el resultado; si
    /// vuelve a llegar la misma clave, se devuelve aquel resultado sin volver a
    /// ejecutar nada.
    ///
    /// Quien de verdad garantiza que no se cuelen dos a la vez es el índice ÚNICO de la
    /// tabla, no la comprobación previa: dos peticiones simultáneas pueden pasar las
    /// dos por el "¿existe?" antes de que ninguna haya insertado. Por eso el insert va
    /// dentro de un try y el choque se trata como "ya la hizo otro".
    /// </remarks>
    public class CD_Idempotencia
    {
        private readonly AppDbContext _context;

        public CD_Idempotencia(AppDbContext context) => _context = context;

        /// <summary>Cuánto se recuerda una clave. Pasado ese plazo se puede reutilizar.</summary>
        /// <remarks>
        /// 24 horas cubre de sobra el doble clic y el "me he quedado sin conexión y lo
        /// he vuelto a intentar", sin hacer crecer la tabla para siempre.
        /// </remarks>
        public static readonly TimeSpan Duracion = TimeSpan.FromHours(24);

        /// <summary>Qué pasó al intentar reservar una clave.</summary>
        public class ReservaDTO
        {
            /// <summary>true si es la primera vez: hay que ejecutar la operación.</summary>
            public bool EsNueva { get; set; }
            /// <summary>Resultado guardado la primera vez, si ya se había hecho.</summary>
            public string? RespuestaAnterior { get; set; }
        }

        /// <summary>
        /// Reserva una clave. Si ya estaba, devuelve lo que se respondió entonces.
        /// </summary>
        public ReservaDTO Reservar(string clave, string nombreOperacion, string contenidoPeticion)
        {
            string huella = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(contenidoPeticion ?? ""))).ToLowerInvariant();

            var existente = _context.IdempotencyKeys.AsNoTracking()
                .FirstOrDefault(k => k.idempotency_key == clave
                                  && k.operation_name == nombreOperacion
                                  && k.expires_at > DateTime.UtcNow);

            if (existente != null)
            {
                return new ReservaDTO
                {
                    EsNueva = false,
                    RespuestaAnterior = existente.response_body_encrypted == null
                        ? null : Encoding.UTF8.GetString(existente.response_body_encrypted)
                };
            }

            try
            {
                _context.IdempotencyKeys.Add(new IdempotencyKey
                {
                    idempotency_key = clave,
                    operation_name = nombreOperacion,
                    request_hash = huella,
                    created_at = DateTime.UtcNow,
                    expires_at = DateTime.UtcNow.Add(Duracion)
                });
                _context.SaveChanges();

                return new ReservaDTO { EsNueva = true };
            }
            catch (DbUpdateException)
            {
                // Choque con el índice único: otra petición idéntica se adelantó entre
                // la comprobación de arriba y este insert. No es un error, es
                // exactamente lo que esta tabla existe para detectar.
                var deOtro = _context.IdempotencyKeys.AsNoTracking()
                    .FirstOrDefault(k => k.idempotency_key == clave
                                      && k.operation_name == nombreOperacion);

                return new ReservaDTO
                {
                    EsNueva = false,
                    RespuestaAnterior = deOtro?.response_body_encrypted == null
                        ? null : Encoding.UTF8.GetString(deOtro.response_body_encrypted)
                };
            }
        }

        /// <summary>Guarda qué se respondió, para poder repetirlo si vuelve la clave.</summary>
        public void GuardarRespuesta(string clave, string nombreOperacion, string respuesta)
        {
            try
            {
                var fila = _context.IdempotencyKeys
                    .FirstOrDefault(k => k.idempotency_key == clave
                                      && k.operation_name == nombreOperacion);
                if (fila == null) return;

                fila.response_body_encrypted = Encoding.UTF8.GetBytes(respuesta);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                // No poder recordar la respuesta no debe tumbar una operación que ya se
                // ha hecho bien. Lo peor que pasa es que un reintento muy raro vuelva a
                // intentarlo, y entonces lo para la numeración.
            }
        }

        /// <summary>
        /// Borra las claves caducadas.
        /// </summary>
        /// <remarks>
        /// Se llama de vez en cuando desde el guardado, no con una tarea programada: la
        /// aplicación no tiene ninguna y montarla para esto sería desproporcionado.
        /// </remarks>
        public void LimpiarCaducadas()
        {
            try
            {
                var viejas = _context.IdempotencyKeys
                    .Where(k => k.expires_at < DateTime.UtcNow)
                    .Take(500)   // a bocados, para no bloquear la tabla
                    .ToList();

                if (viejas.Count == 0) return;

                _context.IdempotencyKeys.RemoveRange(viejas);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                // Limpiar es mantenimiento: si falla, no pasa nada hoy.
            }
        }
    }
}
