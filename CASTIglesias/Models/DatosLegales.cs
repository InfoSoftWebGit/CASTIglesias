namespace CASTIglesias.Models
{
    /// <summary>
    /// Datos del titular de la web que exige la LSSI (art. 10) y el RGPD (art. 13).
    /// </summary>
    /// <remarks>
    /// Único sitio donde se escriben: el aviso legal, la política de privacidad y
    /// la de cookies los leen de aquí. Si cambia algo (alta como autónomo,
    /// cambio de domicilio...), se cambia aquí y se actualiza
    /// <see cref="FechaActualizacion"/>.
    /// </remarks>
    public static class DatosLegales
    {
        /// <summary>Marca de dato sin rellenar. Mientras aparezca, las páginas legales muestran un aviso.</summary>
        public const string Pendiente = "[PENDIENTE]";

        public const string Titular   = "Tiago Manuel Lezcano Barrios";
        public const string NombreComercial = "Congrega Software (Congrega CRM)";
        public const string Nif       = "Y5568974K";
        public const string Domicilio = "Calle de Soria, 2, 2º A, 28941 Fuenlabrada (Madrid), España";
        public const string Email     = "soporte@congrega.es";
        public const string SitioWeb  = "www.congrega.es";

        /// <summary>Fecha de la última revisión de los textos legales.</summary>
        public static readonly DateTime FechaActualizacion = new(2026, 11, 11);

        public static bool Completos =>
            !Nif.Contains(Pendiente) && !Domicilio.Contains(Pendiente);
    }
}
