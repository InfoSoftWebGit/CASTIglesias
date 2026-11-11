using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    [Table("permisos")]
    public class Permisos : ITieneIglesia
    {
        // Iglesia dueña del registro. La rellena AppDbContext al guardar y alimenta
        // el filtro global por iglesia (ver ITieneIglesia).
        public int ID_iglesia { get; set; }


        [Key]
        public int ID_permiso { get; set; }

        [ForeignKey("Usuario")]
        public int ID_usuario { get; set; }
        public int ID_sede { get; set; }
        public bool Usuarios { get; set; }
        public bool UsuariosCrearEditar { get; set; }
        public bool UsuariosEliminar { get; set; }

        public bool Miembros { get; set; }
        public bool MiembrosCrearEditar { get; set; }
        public bool MiembrosEliminar { get; set; }

        public bool Familias { get; set; }
        public bool FamiliasCrearEditar { get; set; }
        public bool FamiliasEliminar { get; set; }

        public bool Grupos { get; set; }
        public bool GruposCrearEditar { get; set; }
        public bool GruposEliminar { get; set; }

        public bool Zonas { get; set; }
        public bool ZonasCrearEditar { get; set; }
        public bool ZonasEliminar { get; set; }

        public bool Diezmos { get; set; }
        public bool DiezmosCrearEditar { get; set; }
        public bool DiezmosEliminar { get; set; }

        public bool Conceptos { get; set; }
        public bool ConceptosCrearEditar { get; set; }
        public bool ConceptosEliminar { get; set; }

        public bool Asistencia { get; set; }
        public bool AsistenciaCrearEditar { get; set; }
        public bool AsistenciaEliminar { get; set; }

        public bool Ministerio { get; set; }
        public bool MinisterioCrearEditar { get; set; }
        public bool MinisterioEliminar { get; set; }

        public bool Visitantes { get; set; }
        public bool VisitantesCrearEditar { get; set; }
        public bool VisitantesEliminar { get; set; }

        public bool Simpatizantes { get; set; }
        public bool SimpatizantesCrearEditar { get; set; }
        public bool SimpatizantesEliminar { get; set; }

        public bool Proceso { get; set; }
        public bool ProcesoCrearEditar { get; set; }
        public bool ProcesoEliminar { get; set; }

        public bool Matrimonios { get; set; }
        public bool MatrimoniosCrearEditar { get; set; }
        public bool MatrimoniosEliminar { get; set; }

        public bool Jovenes { get; set; }
        public bool JovenesCrearEditar { get; set; }
        public bool JovenesEliminar { get; set; }

        public bool Hombres { get; set; }
        public bool HombresCrearEditar { get; set; }
        public bool HombresEliminar { get; set; }

        public bool Mujeres { get; set; }
        public bool MujeresCrearEditar { get; set; }
        public bool MujeresEliminar { get; set; }

        public bool Ninos { get; set; }
        public bool NinosCrearEditar { get; set; }
        public bool NinosEliminar { get; set; }

        public bool Gastos { get; set; }
        public bool GastosCrearEditar { get; set; }
        public bool GastosEliminar { get; set; }

        public bool GastosMiembros { get; set; }
        public bool GastosMiembrosCrearEditar { get; set; }
        public bool GastosMiembrosEliminar { get; set; }

        public bool Ajustes { get; set; }
        public bool AjustesCrearEditar { get; set; }
        public bool AjustesEliminar { get; set; }

        // Pantallas que antes colgaban del permiso de otro módulo (Líderes y los
        // seguimientos de Miembros; cultos, calendarios y salas de Ajustes) y que por
        // eso no se podían conceder por separado. El script
        // Queries/PermisosPorVista.sql copia el valor del módulo del que dependían,
        // para que nadie pierda ni gane acceso al desplegarlo.
        public bool Lideres { get; set; }
        public bool LideresCrearEditar { get; set; }
        public bool LideresEliminar { get; set; }

        public bool Seguimiento { get; set; }
        public bool SeguimientoCrearEditar { get; set; }
        public bool SeguimientoEliminar { get; set; }

        public bool DetalleSeguimiento { get; set; }
        public bool DetalleSeguimientoCrearEditar { get; set; }
        public bool DetalleSeguimientoEliminar { get; set; }

        public bool Cultos { get; set; }
        public bool CultosCrearEditar { get; set; }
        public bool CultosEliminar { get; set; }

        public bool Calendario { get; set; }
        public bool CalendarioCrearEditar { get; set; }
        public bool CalendarioEliminar { get; set; }

        public bool CalendarioAnual { get; set; }
        public bool CalendarioAnualCrearEditar { get; set; }
        public bool CalendarioAnualEliminar { get; set; }

        public bool Salas { get; set; }
        public bool SalasCrearEditar { get; set; }
        public bool SalasEliminar { get; set; }

        // ── Área financiera ──────────────────────────────────────────────────
        // No siguen el patrón Ver/CrearEditar/Eliminar de los módulos de arriba, y es
        // a propósito: aquí lo que hay que separar no son esas tres cosas, sino quién
        // puede apuntar, quién puede meterlo en la contabilidad y quién puede ver lo
        // que ha dado cada persona. Un permiso "eliminar" para asientos no existiría:
        // lo contabilizado no se borra, se revierte.
        //
        // Los roles con acceso total los reciben solos (ObtenerPermisosTotales los
        // pone todos a true por reflexión), así que añadirlos no cierra la puerta a
        // nadie que hoy entre.

        /// <summary>Entrar al área financiera y ver el panel y los informes.</summary>
        public bool FinanzasVer { get; set; }

        /// <summary>Registrar y editar ingresos, aportaciones y gastos.</summary>
        public bool FinanzasOperacionesCrearEditar { get; set; }

        /// <summary>Borrar operaciones que aún no se han contabilizado.</summary>
        public bool FinanzasOperacionesEliminar { get; set; }

        /// <summary>
        /// Llevar una operación a la contabilidad. Es la frontera que importa: desde
        /// ese momento el apunte ya no se puede editar ni borrar.
        /// </summary>
        public bool FinanzasContabilizar { get; set; }

        /// <summary>Revertir un asiento ya contabilizado.</summary>
        public bool FinanzasRevertir { get; set; }

        /// <summary>
        /// Tocar la configuración: plan de cuentas, cajas, fondos, conceptos, reglas de
        /// contabilización, numeraciones, plantillas y ejercicios.
        /// </summary>
        public bool FinanzasConfiguracion { get; set; }

        /// <summary>Aprobar o rechazar operaciones pendientes.</summary>
        public bool FinanzasAprobar { get; set; }

        /// <summary>
        /// Ver qué ha aportado cada persona con nombre y apellidos, y sus certificados.
        /// </summary>
        /// <remarks>
        /// Es el permiso más delicado del módulo: es información personal sensible de
        /// los miembros, no un dato contable más.
        /// </remarks>
        public bool FinanzasDonantes { get; set; }

        // ── Área financiera: pantallas ───────────────────────────────────────
        // Qué pantallas del área puede abrir el usuario. Solo abren la pantalla: lo
        // que se puede HACER dentro lo siguen decidiendo los permisos de arriba
        // (contabilizar, aprobar, configurar...). FinanzasVer sigue siendo la puerta
        // del área y da el panel financiero.
        //
        // La relación pantalla → permiso está en AreaFinancieraController
        // (PermisoPorPantalla) y la usa también el menú financiero.

        // Operaciones
        public bool FinanzasVerIngresos { get; set; }
        public bool FinanzasVerGastos { get; set; }
        public bool FinanzasVerTransferencias { get; set; }
        public bool FinanzasVerPagos { get; set; }
        public bool FinanzasVerArqueos { get; set; }

        // Contabilidad
        public bool FinanzasVerDiario { get; set; }
        public bool FinanzasVerMayor { get; set; }
        public bool FinanzasVerSumasYSaldos { get; set; }
        public bool FinanzasVerBalance { get; set; }
        public bool FinanzasVerResultados { get; set; }
        public bool FinanzasVerCierre { get; set; }

        // Gestión
        public bool FinanzasVerPresupuestos { get; set; }
        public bool FinanzasVerCertificados { get; set; }
        public bool FinanzasVerAprobaciones { get; set; }
        public bool FinanzasVerConciliacion { get; set; }
        public bool FinanzasVerAuditoria { get; set; }

        // Configuración
        public bool FinanzasVerConceptos { get; set; }
        public bool FinanzasVerTesoreria { get; set; }
        public bool FinanzasVerFondos { get; set; }
        public bool FinanzasVerPlanCuentas { get; set; }
        public bool FinanzasVerPlantillas { get; set; }
        public bool FinanzasVerEjercicios { get; set; }
        public bool FinanzasVerTerceros { get; set; }
        public bool FinanzasVerProyectos { get; set; }
        public bool FinanzasVerReglas { get; set; }
        public bool FinanzasVerNumeraciones { get; set; }
        public bool FinanzasVerApertura { get; set; }
    }
}
