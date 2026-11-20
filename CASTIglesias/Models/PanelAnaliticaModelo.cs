namespace CASTIglesias.Models
{
    /// <summary>
    /// Configuración de un panel genérico de informes (ver Views/Shared/_PanelAnalitica.cshtml).
    /// </summary>
    /// <param name="Prefijo">Prefijo único de los id de la página (p. ej. "sg").</param>
    /// <param name="Accion">Acción de AnaliticaController que da los datos.</param>
    /// <param name="Titulo">Título del panel ya traducido.</param>
    /// <param name="Tipo">Parámetro tipo para Pertenencia (zona, grupo, ministerio, lider).</param>
    public record PanelAnaliticaModelo(string Prefijo, string Accion, string Titulo, string? Tipo = null);
}
