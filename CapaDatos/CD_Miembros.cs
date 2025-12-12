using CapaEntidad;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Tsp;

namespace CapaDatos
{
    public class CD_Miembros
    {
        private readonly AppDbContext _context;

        public CD_Miembros(AppDbContext context)
        {
            _context = context;
        }
        #region MÉTODOS COMUNES
        /// <summary>
        /// Lista los miembros. Si sedeID es 1000 (Admin Global), devuelve todos. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para todos).</param>
        // En CapaDatos

        public List<MiembroDetalleDTO> ListarMiembros(int sedeID)
        {
            try
            {
                var consultaMiembros = _context.Miembros
                 .AsQueryable()
                 .Where(m => m.Estado == "Miembro");
                if (sedeID != 1000)
                {
                    consultaMiembros = consultaMiembros.Where(m => m.ID_sede == sedeID);
                }

                    var miembrosConUbicacion = (
                            from m in consultaMiembros
                            join p in _context.Provincia on m.idProvincia equals p.idProvincia into provGroup
                            from p in provGroup.DefaultIfEmpty()
                            join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into munGroup
                            from mun in munGroup.DefaultIfEmpty()

                            select new MiembroDetalleDTO
                            {
                                ID_miembro = m.ID_miembro,
                                Diezmo_individual = m.Diezmo_individual,
                                Diezmo_familiar = m.Diezmo_familiar,
                                Numero_miembro = m.Numero_miembro,
                                Nombre_miembro = m.Nombre_miembro,
                                Apellidos_miembro = m.Apellidos_miembro,
                                Edad = m.Edad,
                                sexo = m.sexo,
                                Telefono_fijo = m.Telefono_fijo,
                                Telefono_movil = m.Telefono_movil,
                                Correo_electronico = m.Correo_electronico,
                                Direccion = m.Direccion,
                                codigo_Postal = m.codigo_Postal,
                                idProvincia = m.idProvincia,
                                idMunicipio = m.idMunicipio,
                                ID_sede = m.ID_sede,
                                Estado = m.Estado,
                                pais_nacimiento = m.pais_nacimiento,
                                estado_Civil = m.estado_Civil,
                                combinar_diezmo = m.combinar_diezmo,
                                excluir_directorio = m.excluir_directorio,
                                fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                                fecha_baja = m.fecha_baja,
                                Bautizado = m.Bautizado,
                                fecha_bautismo = m.fecha_bautismo,
                                Lugar_bautismo = m.Lugar_bautismo,
                                fecha_cumpleanios = m.fecha_cumpleanios,
                                Fecha_fallecido = m.Fecha_fallecido,
                                fecha_boda = m.fecha_boda,
                                acepta_LOPD = m.acepta_LOPD,
                                observaciones = m.observaciones,
                                Numero_hijos = m.Numero_hijos,
                                alumno_VyF = m.alumno_VyF,
                                curso_acabado = m.curso_acabado,
                                persona_cargo = m.persona_cargo,
                                ID_role = m.ID_role,
                                ID_usuario = m.ID_usuario,

                                Nombre_Provincia = p.nombre_provincia, // Nombre traído del JOIN
                                Nombre_Municipio = mun.nombre_municipio, // Nombre traído del JOIN

                                // Inicializar campo calculado (se rellena en el Controller)
                                nombre_sede = string.Empty
                            }
                        ).ToList();

                return miembrosConUbicacion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ListarMiembros EF Core: {ex.Message}");
                return new List<MiembroDetalleDTO>();
            }
        }
        /// <summary>
        /// Cuenta los miembros. Si sedeID es 1000 (Admin Global), cuenta todos. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para todos).</param>
        public int ContadorMiembros(int sedeID)
        {
            try
            {
                var consultaBase = _context.Miembros.AsQueryable();

                if (sedeID != 1000)
                {
                    consultaBase = consultaBase.Where(m => m.ID_sede == sedeID);
                }

                return consultaBase.Count();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ContadorMiembros EF Core: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// REGISTRAR MIEMBROS CON ZONAS, GRUPOS Y MINISTERIOS
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="zonasGrupos"></param>
        /// <param name="mensaje"></param>
        /// <returns></returns>
        public int RegistrarMiembro(MiembroDetalleDTO obj, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                // Validaciones
                if (_context.Miembros.Any(m => m.Correo_electronico == obj.Correo_electronico && m.ID_sede == obj.ID_sede))
                {
                    mensaje = "El correo ya existe en esta sede.";
                    return 0;
                }

                if (_context.Miembros.Any(m => m.Numero_miembro == obj.Numero_miembro && m.ID_sede == obj.ID_sede))
                {
                    mensaje = "El número de miembro ya pertenece a otro miembro en esta sede.";
                    return 0;
                }

                Miembro nuevoMiembro;

                if (obj.ID_miembro == 0)
                {
                    // Crear nuevo miembro
                    nuevoMiembro = new Miembro
                    {
                        Numero_miembro = obj.Numero_miembro,
                        Nombre_miembro = obj.Nombre_miembro,
                        Apellidos_miembro = obj.Apellidos_miembro,
                        Edad = obj.Edad,
                        sexo = obj.sexo,
                        Telefono_fijo = obj.Telefono_fijo,
                        Telefono_movil = obj.Telefono_movil,
                        Correo_electronico = obj.Correo_electronico,
                        Direccion = obj.Direccion,
                        codigo_Postal = obj.codigo_Postal,
                        idProvincia = obj.idProvincia,
                        idMunicipio = obj.idMunicipio,
                        ID_sede = obj.ID_sede,
                        pais_nacimiento = obj.pais_nacimiento,
                        estado_Civil = obj.estado_Civil,
                        combinar_diezmo = obj.combinar_diezmo,
                        excluir_directorio = obj.excluir_directorio,
                        fecha_llegada_iglesia = obj.fecha_llegada_iglesia,
                        Bautizado = obj.Bautizado,
                        fecha_bautismo = obj.fecha_bautismo,
                        Lugar_bautismo = obj.Lugar_bautismo,
                        fecha_cumpleanios = obj.fecha_cumpleanios,
                        fecha_boda = obj.fecha_boda,
                        fecha_baja = obj.fecha_baja,
                        Fecha_fallecido = obj.Fecha_fallecido,
                        observaciones = obj.observaciones,
                        persona_cargo = obj.persona_cargo,
                        alumno_VyF = obj.alumno_VyF,
                        curso_acabado = obj.curso_acabado,
                        acepta_LOPD = obj.acepta_LOPD,
                        Estado = obj.Estado,
                        Fallecido = obj.Fallecido,
                        Diezmo_individual = obj.Diezmo_individual,
                        Diezmo_familiar = obj.Diezmo_familiar,
                        ID_usuario = obj.ID_usuario,
                        ID_role = obj.ID_role,
                        Numero_hijos = obj.Numero_hijos
                    };


                    _context.Miembros.Add(nuevoMiembro);
                    _context.SaveChanges();
                }
                else
                {
                    // Editar miembro existente
                    nuevoMiembro = _context.Miembros.First(m => m.ID_miembro == obj.ID_miembro);

                   nuevoMiembro.Numero_miembro        = obj.Numero_miembro;
                   nuevoMiembro.Nombre_miembro        = obj.Nombre_miembro;
                   nuevoMiembro.Diezmo_individual     = obj.Diezmo_individual;
                   nuevoMiembro.Diezmo_familiar       = obj.Diezmo_familiar;
                   nuevoMiembro.Apellidos_miembro     = obj.Apellidos_miembro;
                   nuevoMiembro.Correo_electronico    = obj.Correo_electronico;
                   nuevoMiembro.Telefono_movil        = obj.Telefono_movil;
                   nuevoMiembro.Telefono_fijo         = obj.Telefono_fijo;
                   nuevoMiembro.Edad                  = obj.Edad;
                   nuevoMiembro.sexo                  = obj.sexo;
                   nuevoMiembro.Direccion             = obj.Direccion;
                   nuevoMiembro.codigo_Postal         = obj.codigo_Postal;
                   nuevoMiembro.idProvincia           = obj.idProvincia;
                   nuevoMiembro.idMunicipio           = obj.idMunicipio;
                   nuevoMiembro.persona_cargo         = obj.persona_cargo;
                   nuevoMiembro.observaciones         = obj.observaciones;
                   nuevoMiembro.Bautizado             = obj.Bautizado;
                   nuevoMiembro.Fecha_fallecido       = obj.Fecha_fallecido;
                   nuevoMiembro.fecha_bautismo        = obj.fecha_bautismo;
                   nuevoMiembro.Lugar_bautismo        = obj.Lugar_bautismo;
                   nuevoMiembro.fecha_cumpleanios     = obj.fecha_cumpleanios;
                   nuevoMiembro.fecha_boda            = obj.fecha_boda;
                   nuevoMiembro.fecha_baja            = obj.fecha_baja;
                   nuevoMiembro.fecha_llegada_iglesia = obj.fecha_llegada_iglesia;
                   nuevoMiembro.excluir_directorio    = obj.excluir_directorio;
                   nuevoMiembro.pais_nacimiento       = obj.pais_nacimiento;
                   nuevoMiembro.estado_Civil          = obj.estado_Civil;
                   nuevoMiembro.Estado                = obj.Estado;
                   nuevoMiembro.Fallecido             = obj.Fallecido;
                   nuevoMiembro.alumno_VyF            = obj.alumno_VyF;
                   nuevoMiembro.Numero_hijos          = obj.Numero_hijos;
                   nuevoMiembro.curso_acabado         = obj.curso_acabado;
                   nuevoMiembro.acepta_LOPD           = obj.acepta_LOPD;

                    _context.SaveChanges();
                }

                mensaje = "Miembro guardado correctamente.";
                return nuevoMiembro.ID_miembro;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al registrar miembro: {ex.Message}";
                return 0;
            }
        }
        /// <summary>
        /// EDITAR MIEMBRO CON ZONAS, GRUPOS Y MINISTERIOS
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="zonasGrupos"></param>
        /// <param name="sedeID"></param>
        /// <param name="mensaje"></param>
        /// <returns></returns>
        public bool EditarMiembro(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                var miembro = _context.Miembros.FirstOrDefault(m => m.ID_miembro == obj.ID_miembro);

                if (miembro == null)
                {
                    mensaje = "Miembro no encontrado.";
                    return false;
                }

                if (sedeID != 1000 && miembro.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El miembro no pertenece a tu sede.";
                    return false;
                }

                // Validaciones
                if (_context.Miembros.Any(m => m.Correo_electronico == obj.Correo_electronico &&
                                               m.ID_miembro != obj.ID_miembro &&
                                               m.ID_sede == miembro.ID_sede))
                {
                    mensaje = "El correo ya está en uso por otro miembro en esta sede.";
                    return false;
                }

                if (_context.Miembros.Any(m => m.Numero_miembro == obj.Numero_miembro &&
                                               m.ID_miembro != obj.ID_miembro &&
                                               m.ID_sede == miembro.ID_sede))
                {
                    mensaje = "El número de miembro ya pertenece a otro miembro.";
                    return false;
                }

                // Mapeo
                miembro.Numero_miembro = obj.Numero_miembro;
                miembro.Nombre_miembro = obj.Nombre_miembro;
                miembro.Diezmo_individual = obj.Diezmo_individual;
                miembro.Diezmo_familiar = obj.Diezmo_familiar;
                miembro.Apellidos_miembro = obj.Apellidos_miembro;
                miembro.Correo_electronico = obj.Correo_electronico;
                miembro.Telefono_movil = obj.Telefono_movil;
                miembro.Telefono_fijo = obj.Telefono_fijo;
                miembro.Edad = obj.Edad;
                miembro.sexo = obj.sexo;
                miembro.Direccion = obj.Direccion;
                miembro.codigo_Postal = obj.codigo_Postal;
                miembro.idProvincia = obj.idProvincia;
                miembro.idMunicipio = obj.idMunicipio;
                miembro.persona_cargo = obj.persona_cargo;
                miembro.observaciones = obj.observaciones;
                miembro.Bautizado = obj.Bautizado;
                miembro.Fecha_fallecido = obj.Fecha_fallecido;
                miembro.fecha_bautismo = obj.fecha_bautismo;
                miembro.Lugar_bautismo = obj.Lugar_bautismo;
                miembro.fecha_cumpleanios = obj.fecha_cumpleanios;
                miembro.fecha_boda = obj.fecha_boda;
                miembro.fecha_baja = obj.fecha_baja;
                miembro.fecha_llegada_iglesia = obj.fecha_llegada_iglesia;
                miembro.excluir_directorio = obj.excluir_directorio;
                miembro.pais_nacimiento = obj.pais_nacimiento;
                miembro.estado_Civil = obj.estado_Civil;
                miembro.Estado = obj.Estado;
                miembro.Fallecido = obj.Fallecido;
                miembro.alumno_VyF = obj.alumno_VyF;
                miembro.Numero_hijos = obj.Numero_hijos;
                miembro.curso_acabado = obj.curso_acabado;
                miembro.acepta_LOPD = obj.acepta_LOPD;
               

                _context.SaveChanges();

                mensaje = "Miembro actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al actualizar miembro: {ex.Message}";
                return false;
            }
        }


        /// <summary>
        /// Elimina un miembro. Si sedeID es 1000 (Admin Global), puede eliminar de cualquier sede. Si es != 1000, solo de su sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para Admin Global).</param>
        public bool EliminarMiembro(int id, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var miembro = _context.Miembros.FirstOrDefault(m => m.ID_miembro == id);

                if (miembro == null)
                {
                    mensaje = "Miembro no encontrado.";
                    return false;
                }

                // 🌟 CORRECCIÓN CLAVE: Usar 1000 para el Admin Global
                if (sedeID != 1000 && miembro.ID_sede != sedeID)
                {
                    mensaje = "Acción denegada. El miembro no pertenece a tu sede.";
                    return false;
                }
                // Si sedeID es 1000, el AdminGlobal puede eliminar.

                _context.Miembros.Remove(miembro);
                _context.SaveChanges();
                mensaje = "Miembro eliminado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al eliminar miembro: {ex.Message}";
                return false;
            }
        }
        #endregion


        #region CONTADORES PARA ZONAS
        /// <summary>
        /// CONTADOR DE MIEMBROS HOMBRES
        /// </summary>
        /// <returns></returns>
        public int ContadorMiembrosHombres(int ID_sede) {
            int totalMiembrosHombres;
            try { 
                totalMiembrosHombres = _context.Miembros.Count(m => m.sexo == "Masculino" && m.ID_sede == ID_sede);
            } catch (Exception ex)
            {
                Console.WriteLine($"Error ContadorMiembrosHombres EF Core: {ex.Message}");
                return 0;
            }
            return totalMiembrosHombres;
        }
        /// <summary>
        /// CONTADOR DE MIEMBROS MUJERES
        /// </summary>
        /// <returns></returns>
        public int ContadorMiembrosMujeres(int ID_sede)
        {
            int totalMiembrosMujeres;
            try
            {
                totalMiembrosMujeres = _context.Miembros.Count(m => m.sexo == "Mujer" && m.ID_sede == ID_sede);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ContadorMiembrosHombres EF Core: {ex.Message}");
                return 0;
            }
            return totalMiembrosMujeres;
        }
        /// <summary>
        /// CONTADOR DE MIEMBROS MUJERES
        /// </summary>
        /// <returns></returns>
        public int ContadorMiembrosJUVEC(int ID_sede)
        {
            int totalMiembrosJovenes;
            try
            {
                // Aplicamos la cláusula Where antes de Count()
                totalMiembrosJovenes = _context.Miembros_Zona_Grupo_Ministerio
                                               .Where(m => m.ID_zona == 3 && m.ID_sede == ID_sede)
                                               .Count();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ContadorMiembrosJUVEC EF Core: {ex.Message}");
                return 0;
            }
            return totalMiembrosJovenes;
        }
        #endregion

        #region BÚSQUEDAS POR PARÁMETROS
        /// <summary>
        /// Busca un miembro por su número. Si sedeID es 1000 (Admin Global), busca en todas. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para todas).</param>
        public List<Miembro> BuscarMiembroPorID(int sedeID, int numeroMiembro)
        {
            try
            {
                var miembro = _context.Miembros
                    // 🌟 CORRECCIÓN CLAVE: Usar 1000 para evitar el filtro
                    .Where(m => m.Numero_miembro == numeroMiembro && (sedeID == 1000 || m.ID_sede == sedeID))
                    .FirstOrDefault();

                return miembro != null ? new List<Miembro> { miembro } : new List<Miembro>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error BuscarMiembroPorID EF Core: {ex.Message}");
                return new List<Miembro>();
            }
        }
        /// <summary>
        /// Busca miembros por nombre o apellido. Si sedeID es 1000 (Admin Global), busca en todas. Si es != 1000, filtra por sede.
        /// </summary>
        /// <param name="sedeID">ID de la sede del usuario logueado (1000 para todas).</param>
        public List<Miembro> BuscarMiembrosPorTexto(int sedeID, string busqueda) 
        {
            try
            {
                string termino = busqueda.Trim().ToLower();

                var miembros = _context.Miembros
                    .Where(m => sedeID == 1000 || m.ID_sede == sedeID)
                    .Where(m => m.Nombre_miembro.ToLower().Contains(termino) ||
                                 m.Apellidos_miembro.ToLower().Contains(termino))
                    .Take(10)
                    .ToList();

                return miembros;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error BuscarMiembrosPorTexto EF Core: {ex.Message}");
                return new List<Miembro>();
            }
        }
        /// <summary>
        /// OBTENER MIEMBROS POR EL ID
        /// </summary>
        /// <param name="id_miembro"></param>
        /// <returns></returns>
        public Miembro ObtenerMiembroPorId(int id_miembro)
        {
            try
            {
                var miembro = _context.Miembros.FirstOrDefault(m => m.ID_miembro == id_miembro);
                return miembro;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ObtenerMiembroPorId EF Core: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region ZONAS, GRUPOS Y MINISTERIOS
        /// <summary>
        /// OBTENER ZONAS GRUPOS Y MINISTERIOS DE UN MIEMBRO
        /// </summary>
        /// <param name="iD_miembro"></param>
        /// <returns></returns>
        public List<MiembroZGMDTO> ObtenerZonasGruposMinisterioMiembro(int iD_miembro)
        {
            var resultado = (from mzgm in _context.Miembros_Zona_Grupo_Ministerio
                             where mzgm.ID_miembro == iD_miembro
                             join z in _context.Zona on mzgm.ID_zona equals z.ID_zona into zonasJoin
                             from z in zonasJoin.DefaultIfEmpty()
                             join g in _context.Grupos on mzgm.ID_grupo equals g.ID_grupo into gruposJoin
                             from g in gruposJoin.DefaultIfEmpty()
                             join m in _context.Ministerios on mzgm.ID_ministerio equals m.ID into ministeriosJoin
                             from m in ministeriosJoin.DefaultIfEmpty()
                             select new MiembroZGMDTO
                             {
                                 iD_zona = mzgm.ID_zona,
                                 nombre_zona = z != null ? z.nombre_zona : "",
                                 iD_grupo = mzgm.ID_grupo,
                                 descripcion_grupo = g != null ? g.Descripcion : "",
                                 id_ministerio = mzgm.ID_ministerio,
                                 descripcion_ministerio = m != null ? m.Descripcion : ""
                             }).ToList();

            return resultado;
        }


        /// <summary>
        /// ASIGNAR ZONAS, GRUPOS Y MINISTERIOS A UN MIEMBRO
        /// </summary>
        /// <param name="idMiembro"></param>
        /// <param name="lista"></param>
        /// <param name="mensaje"></param>
        /// <returns></returns>
        public int GuardarZGMLista(List<Miembro_zona_grupo_ministerio> lista, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                foreach (var item in lista)
                {
                    // evitar nulls si llegan
                    item.ID_zona = item.ID_zona == 0 ? 0 : item.ID_zona;
                    item.ID_grupo = item.ID_grupo == 0 ? 0 : item.ID_grupo;
                    item.ID_ministerio = item.ID_ministerio == 0 ? 0 : item.ID_ministerio;

                    bool existe = _context.Miembros_Zona_Grupo_Ministerio.Any(x =>
                        x.ID_miembro == item.ID_miembro &&
                        x.ID_zona == item.ID_zona &&
                        x.ID_grupo == item.ID_grupo &&
                        x.ID_ministerio == item.ID_ministerio
                    );

                    if (!existe)
                    {
                        _context.Miembros_Zona_Grupo_Ministerio.Add(item);
                    }
                }

                _context.SaveChanges();
                mensaje = "Registros guardados correctamente.";
                return 1;
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return 0;
            }
        }



        /// <summary>
        /// QUITAR ZONA, GRUPO Y MINISTERIO DE UN MIEMBRO
        /// </summary>
        /// <param name="id"></param>
        /// <param name="tipo"></param>
        /// <param name="mensaje"></param>
        /// <returns></returns>
        /// 
        public int EditarGZMLista(List<Miembro_zona_grupo_ministerio> lista, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                foreach (var item in lista)
                {
                    var existente = _context.Miembros_Zona_Grupo_Ministerio
                        .FirstOrDefault(x => x.ID_miembro == item.ID_miembro);

                    if (existente != null)
                    {
                        // actualizar solo los campos enviados
                        existente.ID_zona = item.ID_zona;
                        existente.ID_grupo = item.ID_grupo;
                        existente.ID_ministerio = item.ID_ministerio;
                        existente.ID_sede = item.ID_sede;
                    }
                    else
                    {
                        // si no existe, agregar nuevo
                        _context.Miembros_Zona_Grupo_Ministerio.Add(item);
                    }
                }

                _context.SaveChanges();
                mensaje = "Registros actualizados correctamente.";
                return 1;
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return 0;
            }
        }
        public int EliminarGZMLista(List<Miembro_zona_grupo_ministerio> lista, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                foreach (var item in lista)
                {
                    var existente = _context.Miembros_Zona_Grupo_Ministerio
                        .FirstOrDefault(x => x.ID_miembro == item.ID_miembro);

                    if (existente != null)
                    {
                        if (item.ID_zona == 0) existente.ID_zona = 0;
                        if (item.ID_grupo == 0) existente.ID_grupo = 0;
                        if (item.ID_ministerio == 0) existente.ID_ministerio = 0;
                    }
                }

                _context.SaveChanges();
                mensaje = "Registros eliminados correctamente.";
                return 1;
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return 0;
            }
        }

        #endregion


        #region Visitantes
        public List<MiembroDetalleDTO> ListarVisitantes(int sedeID)
        {
            try
            {
                var consultaBase = _context.Miembros
                    .AsQueryable()
                    .Where(m => m.Estado == "Visitante");

                if (sedeID != 1000)
                {
                    consultaBase = consultaBase.Where(m => m.ID_sede == sedeID);
                }

                var visitantesDetalle = (
                    from m in consultaBase
                    join p in _context.Provincia on m.idProvincia equals p.idProvincia into provincias
                    from p_join in provincias.DefaultIfEmpty()
                    join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into municipios
                    from mun_join in municipios.DefaultIfEmpty()

                    select new MiembroDetalleDTO
                    {
                        ID_miembro = m.ID_miembro,
                        Nombre_miembro = m.Nombre_miembro,
                        Apellidos_miembro = m.Apellidos_miembro,
                        Correo_electronico = m.Correo_electronico,
                        Telefono_movil = m.Telefono_movil,
                        Edad = m.Edad,
                        sexo = m.sexo,
                        Direccion = m.Direccion,
                        codigo_Postal = m.codigo_Postal,
                        idProvincia = m.idProvincia,
                        idMunicipio = m.idMunicipio,
                        fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                        acepta_LOPD = m.acepta_LOPD,
                        ID_sede = m.ID_sede,
                        Nombre_Provincia = p_join == null ? string.Empty : p_join.nombre_provincia,
                        Nombre_Municipio = mun_join == null ? string.Empty : mun_join.nombre_municipio,
                        nombre_sede = string.Empty
                    }
                ).ToList();

                return visitantesDetalle;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ListarVisitantes EF Core: {ex.Message}");
                return new List<MiembroDetalleDTO>();
            }
        }
        public int RegistrarMiembroVisitante(MiembroDetalleDTO obj, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {

                Miembro nuevoVisitante;
                if (obj.ID_miembro == 0)
                {
                    nuevoVisitante = new Miembro
                    {
                        Numero_miembro = obj.Numero_miembro,
                        Nombre_miembro = obj.Nombre_miembro,
                        Apellidos_miembro = obj.Apellidos_miembro,
                        ID_sede = obj.ID_sede,
                        Estado = "Visitante",
                        Edad = obj.Edad,
                        sexo = obj.sexo,
                        Telefono_movil = obj.Telefono_movil,
                        Correo_electronico = obj.Correo_electronico,
                        Direccion = obj.Direccion,
                        codigo_Postal = obj.codigo_Postal,
                        idProvincia = obj.idProvincia,
                        idMunicipio = obj.idMunicipio,
                        acepta_LOPD = obj.acepta_LOPD,
                    };

                    _context.Miembros.Add(nuevoVisitante);
                    _context.SaveChanges();
                }
                else
                {
                    nuevoVisitante = _context.Miembros.First(m => m.ID_miembro == obj.ID_miembro);

                    nuevoVisitante.Numero_miembro = obj.Numero_miembro;
                    nuevoVisitante.Nombre_miembro = obj.Nombre_miembro;
                    nuevoVisitante.Apellidos_miembro = obj.Apellidos_miembro;
                    nuevoVisitante.Edad = obj.Edad;
                    nuevoVisitante.sexo = obj.sexo;
                    nuevoVisitante.Telefono_movil = obj.Telefono_movil;

                    _context.SaveChanges();
                }

                mensaje = "Visitante registrado correctamente.";
                return nuevoVisitante.ID_miembro;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al registrar visitante: {ex.Message}";
                return 0;
            }
        }
        public bool EditarMiembroVisitante(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                var miembroExistente = _context.Miembros.FirstOrDefault(m => m.ID_miembro == obj.ID_miembro);
                if (miembroExistente == null)
                {
                    mensaje = "No se encontró el miembro especificado.";
                    return false;
                }

                // Actualizar propiedades
                miembroExistente.Nombre_miembro = obj.Nombre_miembro;
                miembroExistente.Apellidos_miembro = obj.Apellidos_miembro;
                miembroExistente.ID_sede = sedeID;
                miembroExistente.Edad = obj.Edad;
                miembroExistente.sexo = obj.sexo;
                miembroExistente.Estado = "Visitante";
                miembroExistente.Telefono_movil = obj.Telefono_movil;
                miembroExistente.Correo_electronico = obj.Correo_electronico;
                miembroExistente.Direccion = obj.Direccion;
                miembroExistente.codigo_Postal = obj.codigo_Postal;
                miembroExistente.idProvincia = obj.idProvincia;
                miembroExistente.idMunicipio = obj.idMunicipio;
                miembroExistente.acepta_LOPD = obj.acepta_LOPD;

                // Forzar a EF que es una entidad modificada
                _context.Entry(miembroExistente).State = EntityState.Modified;

                _context.SaveChanges();

                mensaje = "Miembro actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al editar miembro: {ex.Message}";
                return false;
            }
        }

        #endregion Visitates

        #region Simpatizantes
        public List<MiembroDetalleDTO> ListarSimpatizantes(int sedeID)
        {
            try
            {
                var consultaBase = _context.Miembros
                    .AsQueryable()
                    .Where(m => m.Estado == "Simpatizante");

                if (sedeID != 1000)
                {
                    consultaBase = consultaBase.Where(m => m.ID_sede == sedeID);
                }

                var visitantesDetalle = (
                    from m in consultaBase
                    join p in _context.Provincia on m.idProvincia equals p.idProvincia into provincias
                    from p_join in provincias.DefaultIfEmpty()
                    join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into municipios
                    from mun_join in municipios.DefaultIfEmpty()

                    select new MiembroDetalleDTO
                    {
                        ID_miembro = m.ID_miembro,
                        Nombre_miembro = m.Nombre_miembro,
                        Apellidos_miembro = m.Apellidos_miembro,
                        Correo_electronico = m.Correo_electronico,
                        Telefono_movil = m.Telefono_movil,
                        Telefono_fijo = m.Telefono_fijo,
                        Edad = m.Edad,
                        sexo = m.sexo,
                        Direccion = m.Direccion,
                        codigo_Postal = m.codigo_Postal,
                        idProvincia = m.idProvincia,
                        idMunicipio = m.idMunicipio,
                        fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                        fecha_cumpleanios = m.fecha_cumpleanios,
                        fecha_boda = m.fecha_boda,
                        fecha_bautismo = m.fecha_bautismo,
                        Lugar_bautismo = m.Lugar_bautismo,
                        estado_Civil = m.estado_Civil,
                        pais_nacimiento = m.pais_nacimiento,
                        Bautizado = m.Bautizado,
                        Numero_hijos = m.Numero_hijos,
                        acepta_LOPD = m.acepta_LOPD,
                        ID_sede = m.ID_sede,
                        Nombre_Provincia = p_join == null ? string.Empty : p_join.nombre_provincia,
                        Nombre_Municipio = mun_join == null ? string.Empty : mun_join.nombre_municipio,
                        nombre_sede = string.Empty
                    }
                ).ToList();

                return visitantesDetalle;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ListarVisitantes EF Core: {ex.Message}");
                return new List<MiembroDetalleDTO>();
            }
        }
        public int RegistrarMiembroSimpatizante(MiembroDetalleDTO obj, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {

                Miembro nuevoVisitante;
                if (obj.ID_miembro == 0)
                {
                    nuevoVisitante = new Miembro
                    {
                        Numero_miembro = obj.Numero_miembro,
                        Nombre_miembro = obj.Nombre_miembro,
                        Apellidos_miembro = obj.Apellidos_miembro,
                        ID_sede = obj.ID_sede,
                        Estado = "Simpatizante",
                        Edad = obj.Edad,
                        sexo = obj.sexo,
                        Telefono_movil = obj.Telefono_movil,
                        Correo_electronico = obj.Correo_electronico,
                        Direccion = obj.Direccion,
                        codigo_Postal = obj.codigo_Postal,
                        idProvincia = obj.idProvincia,
                        idMunicipio = obj.idMunicipio,
                        pais_nacimiento = obj.pais_nacimiento,
                        estado_Civil = obj.estado_Civil,
                        Bautizado = obj.Bautizado,
                        Lugar_bautismo = obj.Lugar_bautismo,
                        fecha_bautismo = obj.fecha_bautismo,
                        persona_cargo = obj.persona_cargo,
                        Numero_hijos = obj.Numero_hijos,
                        acepta_LOPD = obj.acepta_LOPD,
                    };

                    _context.Miembros.Add(nuevoVisitante);
                    _context.SaveChanges();
                }
                else
                {
                    nuevoVisitante = _context.Miembros.First(m => m.ID_miembro == obj.ID_miembro);

                    nuevoVisitante.Numero_miembro = obj.Numero_miembro;
                    nuevoVisitante.Nombre_miembro = obj.Nombre_miembro;
                    nuevoVisitante.Apellidos_miembro = obj.Apellidos_miembro;
                    nuevoVisitante.Edad = obj.Edad;
                    nuevoVisitante.sexo = obj.sexo;
                    nuevoVisitante.Telefono_movil = obj.Telefono_movil;
                    nuevoVisitante.Correo_electronico = obj.Correo_electronico;
                    nuevoVisitante.Direccion = obj.Direccion;
                    nuevoVisitante.codigo_Postal = obj.codigo_Postal;
                    nuevoVisitante.idProvincia = obj.idProvincia;
                    nuevoVisitante.idMunicipio = obj.idMunicipio;
                    nuevoVisitante.pais_nacimiento = obj.pais_nacimiento;
                    nuevoVisitante.estado_Civil = obj.estado_Civil;
                    nuevoVisitante.Bautizado = obj.Bautizado;
                    nuevoVisitante.Lugar_bautismo = obj.Lugar_bautismo;
                    nuevoVisitante.fecha_bautismo = obj.fecha_bautismo;
                    nuevoVisitante.persona_cargo = obj.persona_cargo;
                    nuevoVisitante.Numero_hijos = obj.Numero_hijos;
                    nuevoVisitante.acepta_LOPD = obj.acepta_LOPD;


                    _context.SaveChanges();
                }

                mensaje = "Simpatizante registrado correctamente.";
                return nuevoVisitante.ID_miembro;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al registrar visitante: {ex.Message}";
                return 0;
            }
        }

        public bool EditarMiembroSimpatizante(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                var miembroExistente = _context.Miembros.FirstOrDefault(m => m.ID_miembro == obj.ID_miembro);
                if (miembroExistente == null)
                {
                    mensaje = "No se encontró el miembro especificado.";
                    return false;
                }

                // Actualizar propiedades
                miembroExistente.Numero_miembro = obj.Numero_miembro;
                miembroExistente.Nombre_miembro = obj.Nombre_miembro;
                miembroExistente.Apellidos_miembro = obj.Apellidos_miembro;
                miembroExistente.Edad = obj.Edad;
                miembroExistente.sexo = obj.sexo;
                miembroExistente.Telefono_movil = obj.Telefono_movil;
                miembroExistente.Correo_electronico = obj.Correo_electronico;
                miembroExistente.Direccion = obj.Direccion;
                miembroExistente.codigo_Postal = obj.codigo_Postal;
                miembroExistente.idProvincia = obj.idProvincia;
                miembroExistente.idMunicipio = obj.idMunicipio;
                miembroExistente.pais_nacimiento = obj.pais_nacimiento;
                miembroExistente.estado_Civil = obj.estado_Civil;
                miembroExistente.Bautizado = obj.Bautizado;
                miembroExistente.Lugar_bautismo = obj.Lugar_bautismo;
                miembroExistente.fecha_bautismo = obj.fecha_bautismo;
                miembroExistente.persona_cargo = obj.persona_cargo;
                miembroExistente.Numero_hijos = obj.Numero_hijos;
                miembroExistente.acepta_LOPD = obj.acepta_LOPD;

                // Forzar a EF que es una entidad modificada
                _context.Entry(miembroExistente).State = EntityState.Modified;

                _context.SaveChanges();

                mensaje = "Miembro actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al editar miembro: {ex.Message}";
                return false;
            }
        }
        #endregion Simpatizantes

        #region Proceso
        public List<MiembroDetalleDTO> ListarMiembrosProceso(int sedeID)
        {
            try
            {
                var consultaMiembros = _context.Miembros
                 .AsQueryable()
                 .Where(m => m.Estado == "Proceso");
                if (sedeID != 1000)
                {
                    consultaMiembros = consultaMiembros.Where(m => m.ID_sede == sedeID);
                }

                var miembrosConUbicacion = (
                        from m in consultaMiembros
                        join p in _context.Provincia on m.idProvincia equals p.idProvincia into provGroup
                        from p in provGroup.DefaultIfEmpty()
                        join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into munGroup
                        from mun in munGroup.DefaultIfEmpty()

                        select new MiembroDetalleDTO
                        {
                            ID_miembro = m.ID_miembro,
                            Diezmo_individual = m.Diezmo_individual,
                            Diezmo_familiar = m.Diezmo_familiar,
                            Numero_miembro = m.Numero_miembro,
                            Nombre_miembro = m.Nombre_miembro,
                            Apellidos_miembro = m.Apellidos_miembro,
                            Edad = m.Edad,
                            sexo = m.sexo,
                            Telefono_fijo = m.Telefono_fijo,
                            Telefono_movil = m.Telefono_movil,
                            Correo_electronico = m.Correo_electronico,
                            Direccion = m.Direccion,
                            codigo_Postal = m.codigo_Postal,
                            idProvincia = m.idProvincia,
                            idMunicipio = m.idMunicipio,
                            ID_sede = m.ID_sede,
                            Estado = m.Estado,
                            pais_nacimiento = m.pais_nacimiento,
                            estado_Civil = m.estado_Civil,
                            combinar_diezmo = m.combinar_diezmo,
                            excluir_directorio = m.excluir_directorio,
                            fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                            fecha_baja = m.fecha_baja,
                            Bautizado = m.Bautizado,
                            fecha_bautismo = m.fecha_bautismo,
                            Lugar_bautismo = m.Lugar_bautismo,
                            fecha_cumpleanios = m.fecha_cumpleanios,
                            Fecha_fallecido = m.Fecha_fallecido,
                            fecha_boda = m.fecha_boda,
                            acepta_LOPD = m.acepta_LOPD,
                            observaciones = m.observaciones,
                            Numero_hijos = m.Numero_hijos,
                            alumno_VyF = m.alumno_VyF,
                            curso_acabado = m.curso_acabado,
                            persona_cargo = m.persona_cargo,
                            ID_role = m.ID_role,
                            ID_usuario = m.ID_usuario,

                            Nombre_Provincia = p.nombre_provincia, // Nombre traído del JOIN
                            Nombre_Municipio = mun.nombre_municipio, // Nombre traído del JOIN

                            // Inicializar campo calculado (se rellena en el Controller)
                            nombre_sede = string.Empty
                        }
                    ).ToList();

                return miembrosConUbicacion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ListarMiembrosSimpatizantes EF Core: {ex.Message}");
                return new List<MiembroDetalleDTO>();
            }
        }

        public int RegistrarMiembroProceso(MiembroDetalleDTO obj, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {

                Miembro nuevoVisitante;
                if (obj.ID_miembro == 0)
                {
                    nuevoVisitante = new Miembro
                    {
                        Numero_miembro = obj.Numero_miembro,
                        Nombre_miembro = obj.Nombre_miembro,
                        Apellidos_miembro = obj.Apellidos_miembro,
                        ID_sede = obj.ID_sede,
                        Estado = "Proceso",
                        Edad = obj.Edad,
                        sexo = obj.sexo,
                        Telefono_movil = obj.Telefono_movil,
                        Correo_electronico = obj.Correo_electronico,
                        Direccion = obj.Direccion,
                        codigo_Postal = obj.codigo_Postal,
                        idProvincia = obj.idProvincia,
                        idMunicipio = obj.idMunicipio,
                        acepta_LOPD = obj.acepta_LOPD,
                    };

                    _context.Miembros.Add(nuevoVisitante);
                    _context.SaveChanges();
                }
                else
                {
                    nuevoVisitante = _context.Miembros.First(m => m.ID_miembro == obj.ID_miembro);

                    nuevoVisitante.Numero_miembro = obj.Numero_miembro;
                    nuevoVisitante.Nombre_miembro = obj.Nombre_miembro;
                    nuevoVisitante.Apellidos_miembro = obj.Apellidos_miembro;
                    nuevoVisitante.Edad = obj.Edad;
                    nuevoVisitante.sexo = obj.sexo;
                    nuevoVisitante.Telefono_movil = obj.Telefono_movil;


                    _context.SaveChanges();
                }

                mensaje = "Miembro en proceso registrado correctamente.";
                return nuevoVisitante.ID_miembro;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al registrar visitante: {ex.Message}";
                return 0;
            }
        }

        public bool EditarMiembroProceso(MiembroDetalleDTO obj, int sedeID, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                var miembroExistente = _context.Miembros.FirstOrDefault(m => m.ID_miembro == obj.ID_miembro);
                if (miembroExistente == null)
                {
                    mensaje = "No se encontró el miembro especificado.";
                    return false;
                }

                // Actualizar propiedades
                miembroExistente.Nombre_miembro = obj.Nombre_miembro;
                miembroExistente.Apellidos_miembro = obj.Apellidos_miembro;
                miembroExistente.ID_sede = sedeID;
                miembroExistente.Edad = obj.Edad;
                miembroExistente.sexo = obj.sexo;
                miembroExistente.Estado = "Proceso";
                miembroExistente.Telefono_movil = obj.Telefono_movil;
                miembroExistente.Correo_electronico = obj.Correo_electronico;
                miembroExistente.Direccion = obj.Direccion;
                miembroExistente.codigo_Postal = obj.codigo_Postal;
                miembroExistente.idProvincia = obj.idProvincia;
                miembroExistente.idMunicipio = obj.idMunicipio;
                miembroExistente.acepta_LOPD = obj.acepta_LOPD;

                // Forzar a EF que es una entidad modificada
                _context.Entry(miembroExistente).State = EntityState.Modified;

                _context.SaveChanges();

                mensaje = "Miembro actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al editar miembro: {ex.Message}";
                return false;
            }
        }

        #endregion Proceso

        #region CambiarEstados
        public MiembroDetalleDTO AvanzarEstado(int ID_sede, int ID_miembro)
        {
            try
            {

                // Buscar miembro existente
                var miembro = _context.Miembros.FirstOrDefault(m => m.ID_miembro == ID_miembro);

                if (miembro == null)
                    return null;

                if (ID_sede != 1000 && miembro.ID_sede != ID_sede)
                    throw new UnauthorizedAccessException("No puedes modificar miembros de otra sede.");

                // Avanzar estado según flujo Visitante → Simpatizante → Proceso → Miembro
                switch (miembro.Estado)
                {
                    case "Visitante":
                        miembro.Estado = "Simpatizante";
                        break;

                    case "Simpatizante":
                        miembro.Estado = "Proceso";
                        break;

                    case "Proceso":
                        miembro.Estado = "Miembro";
                        break;

                    case "Miembro":
                        return ObtenerMiembroPorID(ID_sede, ID_miembro); // ya en estado final
                }

                _context.SaveChanges();

                return ObtenerMiembroPorID(ID_sede, ID_miembro);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR avanzarEstado: {ex.Message}");
                return null;
            }
        }


        public MiembroDetalleDTO RetrocederEstado(int ID_sede, int ID_miembro)
        {
            try
            {
                var miembro = _context.Miembros.FirstOrDefault(m => m.ID_miembro == ID_miembro);

                if (miembro == null)
                    return null;

                // Validar sede
                if (ID_sede != 1000 && miembro.ID_sede != ID_sede)
                    throw new UnauthorizedAccessException("No puedes modificar miembros de otra sede.");

                switch (miembro.Estado)
                {
                    case "Miembro":
                        miembro.Estado = "Proceso";
                        break;

                    case "Proceso":
                        miembro.Estado = "Simpatizante";
                        break;

                    case "Simpatizante":
                        miembro.Estado = "Visitante";
                        break;

                    case "Visitante":
                        // No retrocede más
                        return ObtenerMiembroPorID(ID_sede, ID_miembro);
                }

                _context.SaveChanges();

                return ObtenerMiembroPorID(ID_sede, ID_miembro);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR retrocederEstado: {ex.Message}");
                return null;
            }
        }

        public MiembroDetalleDTO ObtenerMiembroPorID(int sedeID, int ID_miembro)
        {
            try
            {
                var consulta = _context.Miembros
                    .AsQueryable()
                    .Where(m => m.ID_miembro == ID_miembro);

                // Filtrar por sede si no es superadmin
                if (sedeID != 1000)
                {
                    consulta = consulta.Where(m => m.ID_sede == sedeID);
                }

                var miembro = (
                    from m in consulta
                    join p in _context.Provincia on m.idProvincia equals p.idProvincia into provGroup
                    from p in provGroup.DefaultIfEmpty()

                    join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into munGroup
                    from mun in munGroup.DefaultIfEmpty()

                    select new MiembroDetalleDTO
                    {
                        ID_miembro = m.ID_miembro,
                        Diezmo_individual = m.Diezmo_individual,
                        Diezmo_familiar = m.Diezmo_familiar,
                        Numero_miembro = m.Numero_miembro,
                        Nombre_miembro = m.Nombre_miembro,
                        Apellidos_miembro = m.Apellidos_miembro,
                        Edad = m.Edad,
                        sexo = m.sexo,
                        Telefono_fijo = m.Telefono_fijo,
                        Telefono_movil = m.Telefono_movil,
                        Correo_electronico = m.Correo_electronico,
                        Direccion = m.Direccion,
                        codigo_Postal = m.codigo_Postal,
                        idProvincia = m.idProvincia,
                        idMunicipio = m.idMunicipio,
                        ID_sede = m.ID_sede,
                        Estado = m.Estado,
                        pais_nacimiento = m.pais_nacimiento,
                        estado_Civil = m.estado_Civil,
                        combinar_diezmo = m.combinar_diezmo,
                        excluir_directorio = m.excluir_directorio,
                        fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                        fecha_baja = m.fecha_baja,
                        Bautizado = m.Bautizado,
                        fecha_bautismo = m.fecha_bautismo,
                        Lugar_bautismo = m.Lugar_bautismo,
                        fecha_cumpleanios = m.fecha_cumpleanios,
                        Fecha_fallecido = m.Fecha_fallecido,
                        fecha_boda = m.fecha_boda,
                        acepta_LOPD = m.acepta_LOPD,
                        observaciones = m.observaciones,
                        Numero_hijos = m.Numero_hijos,
                        alumno_VyF = m.alumno_VyF,
                        curso_acabado = m.curso_acabado,
                        persona_cargo = m.persona_cargo,
                        ID_role = m.ID_role,
                        ID_usuario = m.ID_usuario,

                        Nombre_Provincia = p != null ? p.nombre_provincia : string.Empty,
                        Nombre_Municipio = mun != null ? mun.nombre_municipio : string.Empty,

                        // Controlador rellena luego este campo
                        nombre_sede = string.Empty
                    }
                ).FirstOrDefault();

                return miembro;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ObtenerMiembroPorID EF Core: {ex.Message}");
                return null;
            }
        }



        #endregion CambiarEstados
    }
}