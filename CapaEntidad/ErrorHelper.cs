using System;

namespace CapaEntidad
{
    /// <summary>
    /// Utilidades para convertir excepciones (especialmente de EF Core / MySQL) en
    /// mensajes claros para el usuario.
    ///
    /// EF Core envuelve el error real de la base de datos dentro de InnerException,
    /// por lo que <c>ex.Message</c> suele devolver el genérico:
    /// "An error occurred while saving the changes. See the inner exception for more details.".
    /// Este helper recorre las InnerException hasta el detalle real y, cuando reconoce
    /// el patrón, lo traduce a un mensaje comprensible en español.
    /// </summary>
    public static class ErrorHelper
    {
        /// <summary>
        /// Devuelve un mensaje claro a partir de una excepción: extrae el error más
        /// interno (donde MySQL deja el detalle real) y lo traduce cuando es posible.
        /// </summary>
        public static string Mensaje(Exception ex)
        {
            if (ex == null) return "Se produjo un error desconocido.";

            // Recorrer hasta la excepción más interna (el detalle real de la BBDD).
            Exception interna = ex;
            while (interna.InnerException != null)
                interna = interna.InnerException;

            string detalle = interna.Message ?? string.Empty;
            string amigable = TraducirError(detalle);

            return !string.IsNullOrEmpty(amigable) ? amigable : detalle;
        }

        /// <summary>
        /// Igual que <see cref="Mensaje(Exception)"/> pero anteponiendo un contexto,
        /// p.ej. <c>ErrorHelper.Mensaje(ex, "No se pudo guardar el culto")</c>.
        /// </summary>
        public static string Mensaje(Exception ex, string contexto)
        {
            string m = Mensaje(ex);
            return string.IsNullOrWhiteSpace(contexto) ? m : $"{contexto}: {m}";
        }

        /// <summary>
        /// Traduce los errores más habituales de MySQL/EF a mensajes claros.
        /// Devuelve cadena vacía si no reconoce el patrón (se usará el detalle real).
        /// </summary>
        private static string TraducirError(string detalle)
        {
            if (string.IsNullOrEmpty(detalle)) return string.Empty;
            string d = detalle.ToLowerInvariant();

            // Restricción de clave foránea.
            if (d.Contains("foreign key constraint fails") || d.Contains("a foreign key constraint"))
            {
                if (d.Contains("cannot delete") || d.Contains("update a parent row"))
                    return "No se puede eliminar este registro porque está siendo utilizado por otros registros relacionados.";
                if (d.Contains("cannot add") || d.Contains("child row"))
                    return "No se puede guardar porque hace referencia a otro registro que no existe.";
                return "Operación no permitida por una restricción de integridad de los datos.";
            }

            // Entrada duplicada (índice único).
            if (d.Contains("duplicate entry"))
                return "Ya existe un registro con ese valor. Revisa los campos que deben ser únicos.";

            // Campo obligatorio sin valor.
            if (d.Contains("cannot be null"))
                return "Falta rellenar un campo obligatorio.";

            // Valor demasiado largo para la columna.
            if (d.Contains("data too long"))
                return "Uno de los valores introducidos es demasiado largo para el campo.";

            // Valor con formato incorrecto (número, fecha, etc.).
            if (d.Contains("incorrect ") && d.Contains(" value"))
                return "Uno de los valores introducidos no tiene el formato correcto.";

            // Fuera de rango.
            if (d.Contains("out of range"))
                return "Uno de los valores introducidos está fuera del rango permitido.";

            // Problemas de conexión con la base de datos.
            if (d.Contains("unable to connect") || d.Contains("timeout") ||
                d.Contains("could not connect") || d.Contains("server has gone away"))
                return "No se pudo conectar con la base de datos. Inténtalo de nuevo en unos momentos.";

            return string.Empty; // Sin traducción específica: se usará el detalle real.
        }
    }
}
