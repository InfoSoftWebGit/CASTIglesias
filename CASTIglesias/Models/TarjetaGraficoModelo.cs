namespace CASTIglesias.Models
{
    /// <summary>Datos de una tarjeta de gráfico (ver Views/Shared/_TarjetaGrafico.cshtml).</summary>
    public record TarjetaGraficoModelo(string IdCanvas, string Titulo, string? Subtitulo = null, bool Bajo = false, bool Ancha = false);
}
