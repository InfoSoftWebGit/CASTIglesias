using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class MiembroDetalleDTO
    {
        // *** CAMPOS BASE DE LA TABLA MIEMBROS ***
        [Key]
        public int ID_miembro { get; set; }

        public int? Diezmo_individual { get; set; }
        public int? Diezmo_familiar { get; set; }

        [Column("Numero_miembro")]
        public int Numero_miembro { get; set; }

        [Column("Nombre_miembro")]
        public string? Nombre_miembro { get; set; }

        [Column("Apellidos_miembro")]
        public string? Apellidos_miembro { get; set; }

        [Column("Edad")]
        public int? Edad { get; set; }

        [Column("Sexo")]
        public string? sexo { get; set; } = string.Empty;

        [Column("Telefono_fijo")]
        public int? Telefono_fijo { get; set; }

        [Column("Telefono_movil")]
        public int? Telefono_movil { get; set; }

        [JsonPropertyName("correo_electronico")]
        public string? Correo_electronico { get; set; }

        [Column("Direccion")]
        public string? Direccion { get; set; }

        [Column("CP")]
        public int? codigo_Postal { get; set; }

        [Column("idProvincia")]
        public int? idProvincia { get; set; }

        [Column("idMunicipio")]
        public int? idMunicipio { get; set; }

        [Column("ID_sede")]
        public int ID_sede { get; set; }

        [Column("Pais_nacimiento")]
        public string? pais_nacimiento { get; set; } = string.Empty;

        [Column("Estado")]
        public string? Estado { get; set; }

        [Column("Estado_Civil")]
        public string? estado_Civil { get; set; }

        [Column("Combinar_diezmo")]
        public bool? combinar_diezmo { get; set; } = false;

        [Column("Excluir_directorio")]
        public bool? excluir_directorio { get; set; }

        public DateTime? fecha_llegada_iglesia { get; set; }

        [Column("Bautizado")]
        public bool? Bautizado { get; set; } = false;

        public DateTime? fecha_bautismo { get; set; }

        [Column("Lugar_bautismo")]
        public string? Lugar_bautismo { get; set; } = string.Empty;

        public DateTime? fecha_cumpleanios { get; set; }

        public DateTime? fecha_boda { get; set; }

        public DateTime? fecha_baja { get; set; }

        [Column("Fecha_fallecido")]
        public DateTime? Fecha_fallecido { get; set; }

        [Column("Observaciones")]
        public string? observaciones { get; set; }

        [Column("Alumno_vyf")]
        public bool? alumno_VyF { get; set; } = false;

        [Column("Curso_acabado")]
        public bool? curso_acabado { get; set; } = false;

        [Column("Fallecido")]
        public bool? Fallecido { get; set; } = false;

        [Column("Persona_cargo")]
        public string? persona_cargo { get; set; }

        [Column("Acepta_LOPD")]
        public bool? acepta_LOPD { get; set; } = false;

        [Column("ID_usuario")]
        public int? ID_usuario { get; set; }

        [Column("ID_role")]
        public int? ID_role { get; set; }
        [Column("Numero_hijos")]
        public int? Numero_hijos { get; set; }

        public string Nombre_Provincia { get; set; } = string.Empty;
        public string Nombre_Municipio { get; set; } = string.Empty;
        public string nombre_sede { get; set; } = string.Empty;

    }
}