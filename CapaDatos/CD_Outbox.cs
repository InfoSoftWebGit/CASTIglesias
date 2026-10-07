using CapaEntidad.Financiero;
using Microsoft.EntityFrameworkCore;

namespace CapaDatos
{
    /// <summary>
    /// Bandeja de salida de eventos financieros (tabla <c>outbox_messages</c>).
    /// </summary>
    /// <remarks>
    /// Qué es y por qué existe: cuando haya que avisar a otro sistema de que se ha
    /// contabilizado algo (un portal de la asesoría, un envío de correo, una
    /// integración futura), ese aviso NO se puede mandar en el momento de contabilizar.
    /// Si el envío falla o tarda, la contabilización se caería con él, y lo importante
    /// es el asiento, no el aviso.
    ///
    /// La solución es escribir el aviso en esta tabla DENTRO de la misma transacción
    /// del asiento, y mandarlo después. Así o se guardan las dos cosas o ninguna, y el
    /// envío se puede reintentar sin tocar la contabilidad.
    ///
    /// Hoy nadie consume esta bandeja: no hay ninguna integración. Se escribe desde ya
    /// porque los eventos que no se registraron en su momento no se pueden recuperar
    /// después, y porque hacerlo ahora cuesta tres líneas y hacerlo luego obligaría a
    /// tocar el motor contable otra vez.
    /// </remarks>
    public class CD_Outbox
    {
        private readonly AppDbContext _context;

        public CD_Outbox(AppDbContext context) => _context = context;

        /// <summary>
        /// Añade un evento. NO guarda: se guarda con la transacción que lo genera.
        /// </summary>
        /// <remarks>
        /// Que no guarde es lo que hace que esto funcione. Llamar a SaveChanges aquí
        /// rompería la garantía de que el evento y el hecho van juntos.
        /// </remarks>
        public void Registrar(int idIglesia, string tipoEvento, string tipoAgregado,
                              int idAgregado, string? datosJson)
        {
            _context.OutboxMessages.Add(new OutboxMessage
            {
                ID_iglesia = idIglesia,
                event_type = tipoEvento,
                aggregate_type = tipoAgregado,
                aggregate_id = idAgregado,
                payload_json = datosJson ?? "{}",
                occurred_at = DateTime.UtcNow,
                attempts = 0
            });
        }

        /// <summary>Eventos que todavía no se han enviado.</summary>
        public List<OutboxMessage> Pendientes(int tope = 100)
            => _context.OutboxMessages.AsNoTracking()
                .Where(m => m.published_at == null)
                .OrderBy(m => m.id)
                .Take(tope)
                .ToList();

        public int ContarPendientes()
            => _context.OutboxMessages.Count(m => m.published_at == null);

        /// <summary>Marca un evento como ya enviado.</summary>
        public void MarcarEnviado(long id)
        {
            var mensaje = _context.OutboxMessages.FirstOrDefault(m => m.id == id);
            if (mensaje == null) return;

            mensaje.published_at = DateTime.UtcNow;
            _context.SaveChanges();
        }

        /// <summary>Anota que un intento de envío falló.</summary>
        public void AnotarFallo(long id)
        {
            var mensaje = _context.OutboxMessages.FirstOrDefault(m => m.id == id);
            if (mensaje == null) return;

            mensaje.attempts++;
            _context.SaveChanges();
        }
    }
}
