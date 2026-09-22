using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("sedes")]
    public class Sedes : ITieneIglesia
    {
        /// <summary>
        /// ID reservado de la fila marcador "Todas las sedes". No es una sede real:
        /// nunca se muestra como sede de una iglesia ni se filtra por él.
        /// </summary>
        public const int TodasLasSedes = 1000;

        public int ID { get; set; }

        // Implementa ITieneIglesia: el selector de sedes solo ve las de la iglesia activa
        public int ID_iglesia { get; set; }

        public string? nombre_sede { get; set; }

        // Código corto de la sede (FUE, TOL...). Va delante de los números de las
        // operaciones financieras para que no se repitan entre sedes de la misma
        // iglesia, que es donde la BBDD exige que sean únicos.
        public string? codigo { get; set; }

        public int? MaxUsuarios { get; set; }
    }
}
