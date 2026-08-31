using System.Globalization;

namespace CASTIglesias.Models
{
    /// <summary>
    /// Punto único donde se declaran los idiomas de interfaz disponibles.
    ///
    /// Existe para que la configuración de Program.cs y la acción que cambia el
    /// idioma (AccesoController.CambiarIdioma) no puedan desincronizarse: si una
    /// aceptase un código que la otra no reconoce, el usuario podría guardar una
    /// preferencia que el middleware descarta silenciosamente.
    ///
    /// Para añadir un idioma nuevo basta con sumar su código aquí y crear el
    /// archivo Resources/SharedResource.[codigo].resx correspondiente.
    /// </summary>
    public static class IdiomasSoportados
    {
        /// <summary>
        /// Idioma con el que se sirve la aplicación mientras el usuario no elija otro.
        /// Es también el idioma en el que están escritas las claves del .resx, de modo
        /// que las vistas aún sin traducir se ven correctamente.
        /// </summary>
        public const string PorDefecto = "es";

        /// <summary>
        /// Códigos ISO aceptados. Cualquier valor fuera de esta lista se rechaza
        /// antes de escribir la cookie de idioma.
        /// </summary>
        public static readonly string[] Codigos = { "es", "en" };

        /// <summary>
        /// Nombre de cada idioma en su propio idioma, para mostrarlo en el selector.
        /// Se escribe literal a propósito: un desplegable de idiomas no debe
        /// traducirse, porque quien no entiende la interfaz actual necesita
        /// reconocer el suyo de un vistazo.
        /// </summary>
        public static readonly Dictionary<string, string> Nombres = new()
        {
            ["es"] = "Español",
            ["en"] = "English"
        };

        /// <summary>
        /// Culturas de interfaz que se pasan a SupportedUICultures en Program.cs.
        /// </summary>
        public static readonly CultureInfo[] Culturas =
            Codigos.Select(c => new CultureInfo(c)).ToArray();

        /// <summary>
        /// Comprueba que un código recibido por querystring o formulario es uno de
        /// los soportados. Se valida contra lista blanca porque el valor acaba
        /// escrito en una cookie y usado para construir un CultureInfo.
        /// </summary>
        /// <param name="codigo">Código de idioma a validar (por ejemplo "en").</param>
        /// <returns>true si el código está soportado.</returns>
        public static bool EsValido(string? codigo) =>
            !string.IsNullOrWhiteSpace(codigo) && Codigos.Contains(codigo);
    }
}
