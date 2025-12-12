using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class CN_Concepto
    {
        private readonly CD_Concepto _capaDatos;

        public CN_Concepto(CD_Concepto capaDatos)
        {
            _capaDatos = capaDatos;
        }

        // 1. Añadir sedeID al método de Listar
        /// <summary>
        /// Lista los conceptos, filtrando por sede. Soporta sedeID=0 para Administrador Global.
        /// </summary>
        public List<Concepto> ListarConceptos(int sedeID)
        {
            // 🌟 Paso 1 CN: Pasar sedeID a la Capa de Datos.
            return _capaDatos.ListarConceptos(sedeID);
        }

        /// <summary>
        /// Registra un concepto. Asigna el ID de la sede antes de la validación y registro.
        /// </summary>
        public int RegistrarConceptos(Concepto obj, int? sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            // 🌟 ASIGNACIÓN CRÍTICA CN: Asignar el ID de la sede del usuario al objeto de la entidad
            // SOLUCIÓN: Usamos .GetValueOrDefault() para convertir el int? (nullable) a int (no nullable).
            // Si sedeID es null, asigna 0 (valor predeterminado de int).
            obj.ID_sede = sedeID.GetValueOrDefault();

            // Validación de la capa de negocio
            if (string.IsNullOrWhiteSpace(obj.nombre_concepto))
            {
                mensaje = "El nombre del concepto no puede ser vacío";
                return 0;
            }

            // Si la validación pasa, delegar a la capa de datos
            // Asumo que el método en CD_Concepto se llama RegistrarConcepto (singular).
            return _capaDatos.RegistrarConcepto(obj, out mensaje);
        }

        /// <summary>
        /// Edita un concepto. Asigna el ID de la sede para que la CD pueda validar la pertenencia.
        /// </summary>
        public bool EditarConcepto(Concepto obj, int? sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            // 🌟 CORRECCIÓN CRÍTICA: Convertir int? a int usando GetValueOrDefault() o ?? 0.
            // Esto asegura que obj.ID_sede (que es INT) reciba 0 si sedeID es NULL (Súper Admin).
            obj.ID_sede = sedeID.GetValueOrDefault(); // Opcional: obj.ID_sede = sedeID ?? 0;

            // Validación de la capa de negocio
            if (string.IsNullOrWhiteSpace(obj.nombre_concepto))
            {
                mensaje = "El nombre del concepto no puede ser vacío";
                return false;
            }

            // Si la validación pasa, delegar a la capa de datos
            return _capaDatos.EditarConcepto(obj, out mensaje);
        }

        /// <summary>
        /// Elimina un concepto. Pasa el sedeID para la validación de seguridad y Admin Global.
        /// </summary>
        public bool EliminarConcepto(int id, int? sedeID, out string mensaje)
        {
            // 🌟 Paso 2 CN: Pasar sedeID a la Capa de Datos.
            return _capaDatos.EliminarConcepto(id, sedeID, out mensaje);
        }
    }
}