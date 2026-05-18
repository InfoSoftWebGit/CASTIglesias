using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CapaEntidad
{
    [Table("miembros")]
    public class Miembro
    {
        [Key]
        public int id_miembro { get; set; }

        public int? diezmo_individual { get; set; }
        public int? diezmo_familiar { get; set; }

        [Column("Numero_miembro")]
        public int numero_miembro { get; set; }

        [Column("Nombre_miembro")]
        public string? nombre_miembro { get; set; }

        [Column("Apellidos_miembro")]
        public string? apellidos_miembro { get; set; }

        [Column("Edad")]
        public int? edad { get; set; }
        [Column("EsLider")]
        public string? esLider { get; set; }

        [Column("Sexo")]
        public string? sexo { get; set; } = string.Empty;

        [Column("Telefono_fijo")]
        public string? telefono_fijo { get; set; }

        [Column("Telefono_movil")]
        public string? telefono_movil { get; set; }

        [JsonPropertyName("correo_electronico")]
        public string? correo_electronico { get; set; }

        [Column("Direccion")]
        public string? direccion { get; set; }

        [Column("CP")]
        public string? codigo_Postal { get; set; }

        [Column("idProvincia")]
        public int? idProvincia { get; set; }

        [Column("idMunicipio")]
        public int? idMunicipio { get; set; }

        [Column("ID_sede")]
        public int id_sede { get; set; }

        [Column("Pais_nacimiento")]
        public string? pais_nacimiento { get; set; } = string.Empty;

        [Column("Estado")]
        public string? estado { get; set; } = string.Empty;

        [Column("Estado_Civil")]
        public string? estado_Civil { get; set; }

        [Column("Combinar_diezmo")]
        public bool? combinar_diezmo { get; set; } = false;

        [Column("Excluir_directorio")]
        public bool? excluir_directorio { get; set; }

        public DateTime? fecha_llegada_iglesia { get; set; }

        [Column("Bautizado")]
        public bool? bautizado { get; set; } = false;

        public DateTime? fecha_bautismo { get; set; }

        [Column("Lugar_bautismo")]
        public string? lugar_bautismo { get; set; } = string.Empty;

        public DateTime? fecha_cumpleanios { get; set; }

        public DateTime? fecha_boda { get; set; }

        public DateTime? fecha_baja { get; set; }

        [Column("Fecha_fallecido")]
        public DateTime? fecha_fallecido { get; set; }

        [Column("Observaciones")]
        public string? observaciones { get; set; }

        [Column("Alumno_vyf")]
        public bool? alumno_VyF { get; set; } = false;

        [Column("Curso_acabado")]
        public bool? curso_acabado { get; set; } = false;

        [Column("Fallecido")]
        public bool? fallecido { get; set; } = false;

        [Column("Responsable")]
        public string? responsable { get; set; }

        [Column("Acepta_LOPD")]
        public bool? acepta_LOPD { get; set; } = false;

        [Column("ID_usuario")]
        public int? id_usuario { get; set; }

        [Column("ID_role")]
        public int? id_role { get; set; }
        [Column("Numero_hijos")]
        public int? numero_hijos { get; set; }
        [Column("Grupo_familiar")]
        public string? grupo_familiar { get; set; }

        [Column("Relacion_con")]
        public string? relacion_con { get; set; }
    }
}
