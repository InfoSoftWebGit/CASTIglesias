using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapaEntidad
{
    /// <summary>
    /// Calendario de servicio ya generado y guardado, para poder consultarlo después
    /// sin volver a generarlo y para poder cambiar a una persona por otra.
    /// </summary>
    /// <remarks>
    /// Se guarda con fecha de caducidad (el fin del periodo más unos días de cortesía):
    /// un calendario pasado no le sirve a nadie y no tiene sentido acumularlos.
    /// </remarks>
    [Table("calendario_servicio")]
    public class CalendarioServicio : ITieneIglesia
    {
        [Key]
        public int ID { get; set; }
        public int ID_iglesia { get; set; }
        public int ID_sede { get; set; }
        public int id_culto { get; set; }

        // Copia del nombre del culto en el momento de generarlo: si luego se renombra
        // el culto, el calendario guardado sigue diciendo lo que decía entonces.
        public string? nombre_culto { get; set; }

        public int tipo_calendario { get; set; }          // 1 Seguridad, 2 Alabanza, 3 Audiovisuales
        public string periodicidad { get; set; } = "mensual";
        public DateTime fecha_inicio { get; set; }
        public DateTime fecha_fin { get; set; }
        public DateTime fecha_caducidad { get; set; }
        public DateTime creado_en { get; set; }
        public int? creado_por { get; set; }
        public DateTime? actualizado_en { get; set; }
        public int? actualizado_por { get; set; }
    }

    /// <summary>Una persona en un rol y una fecha concretos del calendario.</summary>
    [Table("calendario_servicio_asignacion")]
    public class CalendarioServicioAsignacion : ITieneIglesia
    {
        [Key]
        public int ID { get; set; }
        public int ID_iglesia { get; set; }
        public int ID_calendario { get; set; }
        public DateTime fecha { get; set; }
        public string rol_nombre { get; set; } = "";

        // Varias personas pueden cubrir el mismo rol el mismo día (2 de bienvenida,
        // 3 voces...): orden las distingue y permite sustituir solo a una de ellas.
        public short orden { get; set; } = 1;

        // El generador trabaja con nombres, no con ID. Se rellena cuando la persona
        // se elige a mano en la pantalla, que es cuando sí se conoce el congregante.
        public int? ID_miembro { get; set; }

        // Nombre guardado tal cual, como en diezmo o seguimiento: el calendario es una
        // foto de lo acordado. Vacío = hueco sin cubrir.
        public string nombre_miembro { get; set; } = "";
    }

    public class CalendarioRequest
    {
        public int IdCulto { get; set; }
        public string Periodicidad { get; set; } = "mensual"; // semanal, mensual, trimestral
        public int TipoCalendario { get; set; } = 1;          // 1=Seguridad, 2=Alabanza, 3=Audiovisuales
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        // Si viene con valor, se exporta ese calendario guardado en vez de generar uno
        // nuevo: es la única forma de que el Excel salga con las sustituciones hechas.
        public int IdGuardado { get; set; }
    }

    public class CalendarioServicioDTO
    {
        // 0 = calendario recién generado, todavía sin guardar
        public int Id { get; set; }
        public int IdCulto { get; set; }
        public string NombreCulto { get; set; } = "";
        public int TipoCalendario { get; set; }
        public string Periodicidad { get; set; } = "";
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<EntradaCalendario> Entradas { get; set; } = new();
    }

    public class EntradaCalendario
    {
        public DateTime Fecha { get; set; }
        public string DiaSemana { get; set; } = "";
        // clave = rol_nombre, valor = lista de nombres asignados
        public Dictionary<string, List<string>> Asignaciones { get; set; } = new();

        // Solo en los calendarios guardados: el ID de cada asignación, en el mismo
        // orden que los nombres, para poder sustituir a una persona concreta.
        public Dictionary<string, List<int>> IdsAsignacion { get; set; } = new();
    }

    /// <summary>Cambio de una persona por otra en una asignación ya guardada.</summary>
    public class CambioServidorRequest
    {
        public int IdAsignacion { get; set; }
        public int IdMiembro { get; set; }      // 0 = dejar el hueco sin cubrir
    }

    public class CalendarioAgrupadoRequest
    {
        public List<int> IdsCultos { get; set; } = new();
        public string Periodicidad { get; set; } = "mensual";
        public int TipoCalendario { get; set; } = 3;
        public DateTime FechaInicio { get; set; } = DateTime.Today;
    }
}
