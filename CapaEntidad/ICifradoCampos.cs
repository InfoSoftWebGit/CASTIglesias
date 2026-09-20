namespace CapaEntidad
{
    /// <summary>
    /// Cifra y descifra los campos sensibles que se guardan en la BBDD (NIF,
    /// correo y teléfono de un donante, IBAN de una cuenta bancaria).
    /// </summary>
    /// <remarks>
    /// La interfaz está en CapaEntidad porque la usa CapaDatos, y la
    /// implementación vive en la capa web, que es la que tiene DataProtection de
    /// ASP.NET. Así ninguna capa de abajo depende del armazón web.
    ///
    /// AVISO IMPORTANTE: lo cifrado solo se puede leer con las MISMAS claves de
    /// DataProtection. Si esas claves se pierden, los datos son irrecuperables:
    /// no hay contraseña maestra ni forma de recuperarlos.
    /// Por eso, antes de usar esto en producción es OBLIGATORIO tener
    /// configurada la ruta de claves en disco (ver Pendiente-en-Produccion.txt,
    /// apartado 1). Sin esa ruta, las claves viven en memoria y CADA REINICIO
    /// del servidor deja ilegible todo lo que se haya cifrado antes.
    /// </remarks>
    public interface ICifradoCampos
    {
        /// <summary>Cifra un texto. Devuelve null si el texto viene vacío.</summary>
        byte[]? Cifrar(string? texto);

        /// <summary>
        /// Descifra lo guardado. Devuelve null si no hay dato y una cadena vacía
        /// si el dato existe pero no se puede descifrar (claves cambiadas), para
        /// que una pantalla nunca reviente por esto.
        /// </summary>
        string? Descifrar(byte[]? datos);
    }
}
