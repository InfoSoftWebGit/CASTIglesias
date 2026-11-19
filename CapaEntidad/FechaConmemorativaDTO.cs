using System;

namespace CapaEntidad
{
    /// <summary>
    /// Una fecha conmemorativa de un congregante (cumpleaños, bautismo, boda o
    /// llegada a la iglesia) ya preparada para el listado.
    /// </summary>
    /// <remarks>
    /// Hay una fila por fecha y no por congregante: quien tiene cumpleaños y boda
    /// aparece dos veces. Así la vista ordena y filtra por proximidad sin tener que
    /// recorrer cuatro columnas distintas en JavaScript.
    ///
    /// El cálculo de "cuándo toca" se hace en el servidor porque la fecha del
    /// navegador la pone el usuario y no es fiable para decidir qué se celebra hoy.
    /// </remarks>
    public class FechaConmemorativaDTO
    {
        // Tipos de fecha. Son las claves en español que usa el .resx (ver la
        // convención de SharedResource.en.resx), de modo que la vista traduce el
        // valor que llega del servidor sin tener que mapear códigos.
        public const string TipoCumpleanios = "Cumpleaños";
        public const string TipoBautismo = "Aniversario de bautismo";
        public const string TipoBoda = "Aniversario de boda";
        public const string TipoLlegada = "Aniversario en la iglesia";

        public int id_miembro { get; set; }

        public string? nombre_miembro { get; set; }

        public string? apellidos_miembro { get; set; }

        /// <summary>Tipo de fecha. Uno de los Tipo* de esta clase.</summary>
        public string tipo { get; set; } = string.Empty;

        /// <summary>Fecha original tal cual está en la ficha del congregante.</summary>
        public DateTime fecha { get; set; }

        /// <summary>Día del aniversario que viene (o el de hoy, si se celebra hoy).</summary>
        public DateTime proxima_celebracion { get; set; }

        /// <summary>
        /// Días que faltan para la próxima celebración. 0 es hoy, y es lo que usa la
        /// vista tanto para abrir el modal en las fechas del día como para avisar de
        /// las que están a menos de tres días.
        /// </summary>
        public int dias_restantes { get; set; }

        /// <summary>
        /// Años que se cumplen en la próxima celebración, o null si la fecha guardada
        /// no trae un año utilizable (fichas antiguas con el año a 1900 o similar).
        /// </summary>
        public int? aniversario { get; set; }

        public string? telefono_movil { get; set; }

        public string? correo_electronico { get; set; }

        public int id_sede { get; set; }

        /// <summary>Lo rellena el controlador, igual que en MiembroDetalleDTO.</summary>
        public string nombre_sede { get; set; } = string.Empty;
    }
}
