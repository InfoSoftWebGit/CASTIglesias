using CapaDatos;
using CapaEntidad.Financiero;

namespace CapaNegocio
{
    /// <summary>Reglas de los fondos.</summary>
    public class CN_Fondos
    {
        private readonly CD_Fondos _cdFondos;

        public CN_Fondos(CD_Fondos cdFondos) => _cdFondos = cdFondos;

        /// <summary>Alcance del fondo. Coincide con el ENUM de la BBDD.</summary>
        /// <remarks>
        /// site = de una sede concreta; shared = compartido por varias;
        /// corporate = de toda la iglesia.
        /// </remarks>
        public static readonly string[] Alcances = { "site", "shared", "corporate" };

        /// <summary>Qué hacer si se intenta gastar más de lo que tiene el fondo.</summary>
        public static readonly string[] PoliticasSobregiro = { "allow", "warn", "block", "require_approval" };

        public List<Fund> Listar() => _cdFondos.Listar();
        public List<Fund> ListarActivos() => _cdFondos.ListarActivos();
        public Fund? Obtener(int id) => _cdFondos.Obtener(id);
        public List<int> ListarSedesDelFondo(int idFondo) => _cdFondos.ListarSedesDelFondo(idFondo);

        public int Guardar(Fund fondo, List<int>? sedes, int idIglesia, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(fondo.code))
            {
                mensaje = "El código del fondo es obligatorio.";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(fondo.name))
            {
                mensaje = "El nombre del fondo es obligatorio.";
                return 0;
            }

            fondo.code = fondo.code.Trim();
            fondo.name = fondo.name.Trim();

            if (!Alcances.Contains(fondo.scope_type))
            {
                mensaje = "El alcance del fondo no es válido.";
                return 0;
            }
            if (!PoliticasSobregiro.Contains(fondo.overdraw_policy))
            {
                mensaje = "La política de sobregiro no es válida.";
                return 0;
            }

            if (_cdFondos.ExisteCodigo(fondo.code, fondo.id))
            {
                mensaje = "Ya existe un fondo con ese código.";
                return 0;
            }

            // Un fondo de una sola sede necesita saber cuál; si no, no se puede
            // decidir quién puede gastar de él.
            if (fondo.scope_type == "site" && (fondo.owner_site_id == null || fondo.owner_site_id == 0))
            {
                mensaje = "Un fondo de sede tiene que indicar de qué sede es.";
                return 0;
            }
            if (fondo.scope_type != "site")
            {
                fondo.owner_site_id = null;
            }

            if (fondo.end_date.HasValue && fondo.end_date.Value <= fondo.start_date)
            {
                mensaje = "La fecha de fin debe ser posterior a la de inicio.";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(fondo.status))
                fondo.status = CD_Fondos.Activo;

            int id;
            if (fondo.id == 0)
            {
                id = _cdFondos.Registrar(fondo, out mensaje);
                if (id == 0) return 0;
            }
            else
            {
                if (!_cdFondos.Editar(fondo, out mensaje)) return 0;
                id = fondo.id;
            }

            // Las sedes solo tienen sentido en un fondo compartido: en los demás
            // el alcance ya lo dice todo.
            if (fondo.scope_type == "shared")
            {
                if (!_cdFondos.SincronizarSedes(id, idIglesia, sedes ?? new List<int>(), out string mensajeSedes))
                {
                    // El fondo ya está guardado: se avisa en lugar de dar todo por fallido
                    mensaje += " " + mensajeSedes;
                }
            }
            else
            {
                _cdFondos.SincronizarSedes(id, idIglesia, new List<int>(), out _);
            }

            return id;
        }

        public bool Eliminar(int id, out string mensaje)
        {
            mensaje = string.Empty;

            if (_cdFondos.TieneMovimientos(id))
            {
                mensaje = "El fondo ya tiene movimientos y no se puede eliminar. Puedes marcarlo como inactivo.";
                return false;
            }

            if (_cdFondos.UsadoPorConceptos(id))
            {
                mensaje = "Hay conceptos que usan este fondo por defecto. Cámbialos antes de eliminarlo.";
                return false;
            }

            return _cdFondos.Eliminar(id, out mensaje);
        }
    }
}
