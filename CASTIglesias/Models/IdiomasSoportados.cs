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

        /// <summary>
        /// Código del idioma de interfaz activo en la petición actual ("es" o "en").
        /// Atajo para no repetir CultureInfo.CurrentUICulture en cada vista.
        /// </summary>
        public static string Actual => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        /// <summary>
        /// URL del paquete de traducciones de DataTables para el idioma activo.
        ///
        /// DataTables trae sus textos ("Search:", "Showing X of Y", "No data
        /// available") embebidos en inglés, así que en inglés se devuelve cadena
        /// vacía y la vista debe omitir la opción "language" por completo. Cargar
        /// en-GB.json funcionaría, pero es una petición de red innecesaria.
        /// </summary>
        /// <returns>URL del JSON de idioma, o cadena vacía si no hace falta ninguno.</returns>
        public static string UrlIdiomaDataTables() =>
            Actual == "es"
                ? "https://cdn.datatables.net/plug-ins/2.3.0/i18n/es-ES.json"
                : string.Empty;
    }
}
