using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>
    /// Certificados anuales de aportaciones.
    /// </summary>
    /// <remarks>
    /// Decisión D8. Dos cosas que conviene no perder de vista:
    ///
    /// 1. Los importes individuales son información sensible. Quién puede verlos se
    ///    decide en el controlador con los roles, no aquí, pero esta clase existe
    ///    para que ese punto de control esté en un solo sitio y no repartido.
    ///
    /// 2. El certificado NO tiene efectos fiscales. La fase 1 emite un documento que
    ///    dice "esta persona aportó esto durante el año", y nada más: sin
    ///    certificación fiscal, sin deducciones y sin integración con Hacienda. El
    ///    texto del documento lo dice explícitamente para que nadie lo presente
    ///    creyendo que desgrava.
    /// </remarks>
    public class CN_Certificados
    {
        private readonly CD_Certificados _cdCertificados;

        public CN_Certificados(CD_Certificados cdCertificados)
            => _cdCertificados = cdCertificados;

        /// <summary>Todo lo que lleva el certificado de una persona.</summary>
        public class CertificadoDTO
        {
            public Party? Donante { get; set; }
            public int Anio { get; set; }
            public List<CD_Certificados.LineaCertificadoDTO> Lineas { get; set; } = new();

            public decimal Total => Lineas.Sum(l => l.total);
            public int Aportaciones => Lineas.Sum(l => l.veces);
            public bool TieneAportaciones => Lineas.Count > 0;
        }

        public List<int> AniosConAportaciones()
        {
            var anios = _cdCertificados.AniosConAportaciones();
            // Si todavía no hay nada contabilizado, al menos que el desplegable ofrezca
            // el año en curso en vez de quedarse vacío.
            if (anios.Count == 0) anios.Add(DateTime.Today.Year);
            return anios;
        }

        public List<CD_Certificados.ResumenDonanteDTO> ResumenDelAnio(int anio, int? sedeID)
            => _cdCertificados.ResumenDelAnio(anio, sedeID);

        /// <summary>
        /// Prepara el certificado de una persona. Devuelve null si el tercero no
        /// existe.
        /// </summary>
        public CertificadoDTO? Certificado(int idTercero, int anio, int? sedeID)
        {
            var donante = _cdCertificados.Tercero(idTercero);
            if (donante == null) return null;

            return new CertificadoDTO
            {
                Donante = donante,
                Anio = anio,
                Lineas = _cdCertificados.DetalleDonante(idTercero, anio, sedeID)
            };
        }
    }
}
