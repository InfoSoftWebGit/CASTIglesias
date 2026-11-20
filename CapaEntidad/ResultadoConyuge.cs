namespace CapaEntidad
{
    /// <summary>
    /// Lo que ha pasado con la pareja al guardar una ficha (ver CD_Miembros.VincularConyuge).
    /// La pantalla lo usa para abrir la ficha del cónyuge recién vinculado y avisar
    /// de que el diezmo familiar se ha compartido.
    /// </summary>
    public class ResultadoConyuge
    {
        /// <summary>
        /// true si la pareja se acaba de vincular (o ha cambiado): hay que abrir la
        /// ficha del cónyuge para que se revise y se guarde.
        /// </summary>
        public bool abrir_ficha { get; set; }

        public int? id_conyuge { get; set; }
        public string? nombre_conyuge { get; set; }

        /// <summary>Estado del cónyuge (Visitante, Simpatizante, Proceso o Miembro): decide en qué pantalla se abre su ficha.</summary>
        public string? estado_conyuge { get; set; }

        /// <summary>true si en este guardado se ha copiado el diezmo familiar al cónyuge.</summary>
        public bool diezmo_compartido { get; set; }
        public string? diezmo_familiar { get; set; }

        /// <summary>Si la ficha se guardó pero el vínculo falló, el motivo; si no, null.</summary>
        public string? aviso { get; set; }
    }
}
