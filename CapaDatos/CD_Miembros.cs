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
                 .Where(m => m.estado == "Miembro");
                if (sedeID != 1000)
                {
                    consultaMiembros = consultaMiembros.Where(m => m.id_sede == sedeID);
                }

                    var miembrosConUbicacion = (
                            from m in consultaMiembros
                            join p in _context.Provincia on m.idProvincia equals p.idProvincia into provGroup
                            from p in provGroup.DefaultIfEmpty()
                            join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into munGroup
                            from mun in munGroup.DefaultIfEmpty()

                            select new MiembroDetalleDTO
                            {
                                id_miembro = m.id_miembro,
                                diezmo_individual = m.diezmo_individual,
                                diezmo_familiar = m.diezmo_familiar,
                                numero_miembro = m.numero_miembro,
                                nombre_miembro = m.nombre_miembro,
                                apellidos_miembro = m.apellidos_miembro,
                                edad = m.edad,
                                esLider = m.esLider,
                                sexo = m.sexo,
                                telefono_fijo = m.telefono_fijo,
                                telefono_movil = m.telefono_movil,
                                correo_electronico = m.correo_electronico,
                                direccion = m.direccion,
                                codigo_Postal = m.codigo_Postal,
                                idProvincia = m.idProvincia,
                                idMunicipio = m.idMunicipio,
                                id_sede = m.id_sede,
                                estado = m.estado,
                                pais_nacimiento = m.pais_nacimiento,
                                estado_Civil = m.estado_Civil,
                                combinar_diezmo = m.combinar_diezmo,
                                excluir_directorio = m.excluir_directorio,
                                fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                                fecha_baja = m.fecha_baja,
                                bautizado = m.bautizado,
                                fecha_bautismo = m.fecha_bautismo,
                                lugar_bautismo = m.lugar_bautismo,
                                fecha_cumpleanios = m.fecha_cumpleanios,
                                fecha_fallecido = m.fecha_fallecido,
                                fecha_boda = m.fecha_boda,
                                acepta_LOPD = m.acepta_LOPD,
                                observaciones = m.observaciones,
                                numero_hijos = m.numero_hijos,
                                alumno_VyF = m.alumno_VyF,
                                curso_acabado = m.curso_acabado,
                                responsable = m.responsable,
                                id_role = m.id_role,
                                id_usuario = m.id_usuario,
                                relacion_con = m.relacion_con,
                                grupo_familiar = m.grupo_familiar,

                                nombre_Provincia = p.nombre_provincia, // Nombre traído del JOIN
                                nombre_Municipio = mun.nombre_municipio, // Nombre traído del JOIN

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
                    consultaBase = consultaBase.Where(m => m.id_sede == sedeID);
                }

                return consultaBase.Count();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ContadorMiembros EF Core: {ex.Message}");
                return 0;
            }
        }

        public int ContadorPorEstado(int sedeID, string estado)
        {
            try
            {
                var consultaBase = _context.Miembros.Where(m => m.estado == estado);
                if (sedeID != 1000)
                    consultaBase = consultaBase.Where(m => m.id_sede == sedeID);
                return consultaBase.Count();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ContadorPorEstado({estado}) EF Core: {ex.Message}");
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
                if (_context.Miembros.Any(m => m.correo_electronico == obj.correo_electronico && m.id_sede == obj.id_sede && m.id_miembro != obj.id_miembro))
                {
                    mensaje = "El correo ya existe en esta sede.";
                    return 0;
                }

                if (_context.Miembros.Any(m => m.numero_miembro == obj.numero_miembro && m.id_sede == obj.id_sede && m.id_miembro != obj.id_miembro))
                {
                    mensaje = "El número de miembro ya pertenece a otro miembro en esta sede.";
                    return 0;
                }

                if (!string.IsNullOrEmpty(obj.diezmo_individual) &&
                    _context.Miembros.Any(m => m.diezmo_individual == obj.diezmo_individual && m.id_sede == obj.id_sede && m.id_miembro != obj.id_miembro))
                {
                    mensaje = "El número de diezmo individual ya está asignado a otro miembro en esta sede.";
                    return 0;
                }

                if (!string.IsNullOrEmpty(obj.diezmo_familiar) &&
                    _context.Miembros.Any(m => m.diezmo_familiar == obj.diezmo_familiar && m.id_sede == obj.id_sede && m.id_miembro != obj.id_miembro))
                {
                    mensaje = "El número de diezmo familiar ya está asignado a otro miembro en esta sede.";
                    return 0;
                }

                Miembro nuevoMiembro;

                if (obj.id_miembro == 0)
                {
                    // Crear nuevo miembro

                    nuevoMiembro = new Miembro
                    {
                        numero_miembro = obj.numero_miembro,
                        nombre_miembro = obj.nombre_miembro,
                        apellidos_miembro = obj.apellidos_miembro,
                        edad = obj.edad,
                        esLider = obj.esLider,
                        sexo = obj.sexo,
                        telefono_fijo = obj.telefono_fijo,
                        telefono_movil = obj.telefono_movil,
                        correo_electronico = obj.correo_electronico,
                        direccion = obj.direccion,
                        codigo_Postal = obj.codigo_Postal,
                        idProvincia = obj.idProvincia,
                        idMunicipio = obj.idMunicipio,
                        id_sede = obj.id_sede,
                        pais_nacimiento = obj.pais_nacimiento,
                        estado_Civil = obj.estado_Civil,
                        combinar_diezmo = obj.combinar_diezmo,
                        excluir_directorio = obj.excluir_directorio,
                        fecha_llegada_iglesia = obj.fecha_llegada_iglesia,
                        bautizado = obj.bautizado,
                        fecha_bautismo = obj.fecha_bautismo,
                        lugar_bautismo = obj.lugar_bautismo,
                        fecha_cumpleanios = obj.fecha_cumpleanios,
                        fecha_boda = obj.fecha_boda,
                        fecha_baja = obj.fecha_baja,
                        fecha_fallecido = obj.fecha_fallecido,
                        observaciones = obj.observaciones,
                        responsable = obj.responsable,
                        alumno_VyF = obj.alumno_VyF,
                        curso_acabado = obj.curso_acabado,
                        acepta_LOPD = obj.acepta_LOPD,
                        estado = obj.estado,
                        fallecido = obj.fallecido,
                        diezmo_individual = obj.diezmo_individual,
                        diezmo_familiar = obj.diezmo_familiar,
                        id_usuario = obj.id_usuario,
                        id_role = obj.id_role,
                        numero_hijos = obj.numero_hijos
                    };


                    _context.Miembros.Add(nuevoMiembro);
                    _context.SaveChanges();
                }
                else
                {
                    // Editar miembro existente
                    nuevoMiembro = _context.Miembros.First(m => m.id_miembro == obj.id_miembro);

                   nuevoMiembro.numero_miembro        = obj.numero_miembro;
                   nuevoMiembro.nombre_miembro        = obj.nombre_miembro;
                   nuevoMiembro.diezmo_individual     = obj.diezmo_individual;
                   nuevoMiembro.diezmo_familiar       = obj.diezmo_familiar;
                   nuevoMiembro.apellidos_miembro     = obj.apellidos_miembro;
                   nuevoMiembro.correo_electronico    = obj.correo_electronico;
                   nuevoMiembro.telefono_movil        = obj.telefono_movil;
                   nuevoMiembro.telefono_fijo         = obj.telefono_fijo;
                   nuevoMiembro.edad                  = obj.edad;
                   nuevoMiembro.sexo                  = obj.sexo;
                   nuevoMiembro.direccion             = obj.direccion;
                   nuevoMiembro.codigo_Postal         = obj.codigo_Postal;
                   nuevoMiembro.idProvincia           = obj.idProvincia;
                   nuevoMiembro.idMunicipio           = obj.idMunicipio;
                   nuevoMiembro.responsable         = obj.responsable;
                   nuevoMiembro.observaciones         = obj.observaciones;
                   nuevoMiembro.bautizado             = obj.bautizado;
                   nuevoMiembro.fecha_fallecido       = obj.fecha_fallecido;
                   nuevoMiembro.fecha_bautismo        = obj.fecha_bautismo;
                   nuevoMiembro.lugar_bautismo        = obj.lugar_bautismo;
                   nuevoMiembro.fecha_cumpleanios     = obj.fecha_cumpleanios;
                   nuevoMiembro.fecha_boda            = obj.fecha_boda;
                   nuevoMiembro.fecha_baja            = obj.fecha_baja;
                   nuevoMiembro.fecha_llegada_iglesia = obj.fecha_llegada_iglesia;
                   nuevoMiembro.excluir_directorio    = obj.excluir_directorio;
                   nuevoMiembro.pais_nacimiento       = obj.pais_nacimiento;
                   nuevoMiembro.estado_Civil          = obj.estado_Civil;
                   nuevoMiembro.estado                = obj.estado;
                   nuevoMiembro.fallecido             = obj.fallecido;
                   nuevoMiembro.alumno_VyF            = obj.alumno_VyF;
                   nuevoMiembro.numero_hijos          = obj.numero_hijos;
                   nuevoMiembro.curso_acabado         = obj.curso_acabado;
                   nuevoMiembro.acepta_LOPD           = obj.acepta_LOPD;

                    _context.SaveChanges();
                }

                mensaje = "Miembro guardado correctamente.";
                return nuevoMiembro.id_miembro;
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
                var miembro = _context.Miembros.FirstOrDefault(m => m.id_miembro == obj.id_miembro);

                if (miembro == null)
                {
                    mensaje = "Miembro no encontrado.";
                    return false;
                }

                if (sedeID != 1000 && miembro.id_sede != sedeID)
                {
                    mensaje = "Acción denegada. El miembro no pertenece a tu sede.";
                    return false;
                }

                // Validaciones
                if (_context.Miembros.Any(m => m.correo_electronico == obj.correo_electronico &&
                                               m.id_miembro != obj.id_miembro &&
                                               m.id_sede == miembro.id_sede))
                {
                    mensaje = "El correo ya está en uso por otro miembro en esta sede.";
                    return false;
                }

                if (_context.Miembros.Any(m => m.numero_miembro == obj.numero_miembro &&
                                               m.id_miembro != obj.id_miembro &&
                                               m.id_sede == miembro.id_sede))
                {
                    mensaje = "El número de miembro ya pertenece a otro miembro.";
                    return false;
                }

                if (!string.IsNullOrEmpty(obj.diezmo_individual) &&
                    _context.Miembros.Any(m => m.diezmo_individual == obj.diezmo_individual &&
                                               m.id_miembro != obj.id_miembro &&
                                               m.id_sede == miembro.id_sede))
                {
                    mensaje = "El número de diezmo individual ya está asignado a otro miembro en esta sede.";
                    return false;
                }

                if (!string.IsNullOrEmpty(obj.diezmo_familiar) &&
                    _context.Miembros.Any(m => m.diezmo_familiar == obj.diezmo_familiar &&
                                               m.id_miembro != obj.id_miembro &&
                                               m.id_sede == miembro.id_sede))
                {
                    mensaje = "El número de diezmo familiar ya está asignado a otro miembro en esta sede.";
                    return false;
                }

                // Mapeo
                miembro.numero_miembro = obj.numero_miembro;
                miembro.nombre_miembro = obj.nombre_miembro;
                miembro.diezmo_individual = obj.diezmo_individual;
                miembro.diezmo_familiar = obj.diezmo_familiar;
                miembro.apellidos_miembro = obj.apellidos_miembro;
                miembro.correo_electronico = obj.correo_electronico;
                miembro.telefono_movil = obj.telefono_movil;
                miembro.telefono_fijo = obj.telefono_fijo;
                miembro.edad = obj.edad;
                miembro.sexo = obj.sexo;
                miembro.direccion = obj.direccion;
                miembro.codigo_Postal = obj.codigo_Postal;
                miembro.idProvincia = obj.idProvincia;
                miembro.idMunicipio = obj.idMunicipio;
                miembro.responsable = obj.responsable;
                miembro.observaciones = obj.observaciones;
                miembro.bautizado = obj.bautizado;
                miembro.fecha_fallecido = obj.fecha_fallecido;
                miembro.fecha_bautismo = obj.fecha_bautismo;
                miembro.lugar_bautismo = obj.lugar_bautismo;
                miembro.fecha_cumpleanios = obj.fecha_cumpleanios;
                miembro.fecha_boda = obj.fecha_boda;
                miembro.fecha_baja = obj.fecha_baja;
                miembro.fecha_llegada_iglesia = obj.fecha_llegada_iglesia;
                miembro.excluir_directorio = obj.excluir_directorio;
                miembro.pais_nacimiento = obj.pais_nacimiento;
                miembro.estado_Civil = obj.estado_Civil;
                // estado solo se modifica via AvanzarEstado / RetrocederEstado
                miembro.fallecido = obj.fallecido;
                miembro.alumno_VyF = obj.alumno_VyF;
                miembro.numero_hijos = obj.numero_hijos;
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
                var miembro = _context.Miembros.FirstOrDefault(m => m.id_miembro == id);

                if (miembro == null)
                {
                    mensaje = "Miembro no encontrado.";
                    return false;
                }

                // 🌟 CORRECCIÓN CLAVE: Usar 1000 para el Admin Global
                if (sedeID != 1000 && miembro.id_sede != sedeID)
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
                totalMiembrosHombres = _context.Miembros.Count(m => m.sexo == "Masculino" && m.id_sede == ID_sede);
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
                totalMiembrosMujeres = _context.Miembros.Count(m => m.sexo == "Mujer" && m.id_sede == ID_sede);
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
                    .Where(m => m.numero_miembro == numeroMiembro && (sedeID == 1000 || m.id_sede == sedeID))
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
                    .Where(m => sedeID == 1000 || m.id_sede == sedeID)
                    .Where(m => m.nombre_miembro.ToLower().Contains(termino) ||
                                 m.apellidos_miembro.ToLower().Contains(termino))
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
                var miembro = _context.Miembros.FirstOrDefault(m => m.id_miembro == id_miembro);
                return miembro;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ObtenerMiembroPorId EF Core: {ex.Message}");
                return null;
            }
        }

        public int ObtenerMaxNumeroMiembro(int sedeID)
        {
            try
            {
                var query = _context.Miembros.AsQueryable();
                if (sedeID != 1000)
                    query = query.Where(m => m.id_sede == sedeID);
                return query.Max(m => (int?)m.numero_miembro) ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ObtenerMaxNumeroMiembro: {ex.Message}");
                return 0;
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
        public bool SincronizarZGM(int idMiembro, int idSede, List<Miembro_zona_grupo_ministerio> lista, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                var existentes = _context.Miembros_Zona_Grupo_Ministerio
                    .Where(x => x.ID_miembro == idMiembro).ToList();
                _context.Miembros_Zona_Grupo_Ministerio.RemoveRange(existentes);

                foreach (var item in lista)
                {
                    item.ID_miembro = idMiembro;
                    item.ID_sede = idSede;
                    _context.Miembros_Zona_Grupo_Ministerio.Add(item);
                }

                _context.SaveChanges();
                mensaje = "ZGM sincronizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
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
                    .Where(m => m.estado == "Visitante");

                if (sedeID != 1000)
                {
                    consultaBase = consultaBase.Where(m => m.id_sede == sedeID);
                }

                var visitantesDetalle = (
                    from m in consultaBase
                    join p in _context.Provincia on m.idProvincia equals p.idProvincia into provincias
                    from p_join in provincias.DefaultIfEmpty()
                    join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into municipios
                    from mun_join in municipios.DefaultIfEmpty()

                    select new MiembroDetalleDTO
                    {
                        id_miembro = m.id_miembro,
                        nombre_miembro = m.nombre_miembro,
                        apellidos_miembro = m.apellidos_miembro,
                        correo_electronico = m.correo_electronico,
                        telefono_movil = m.telefono_movil,
                        edad = m.edad,
                        sexo = m.sexo,
                        direccion = m.direccion,
                        codigo_Postal = m.codigo_Postal,
                        idProvincia = m.idProvincia,
                        idMunicipio = m.idMunicipio,
                        fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                        pais_nacimiento = m.pais_nacimiento,
                        acepta_LOPD = m.acepta_LOPD,
                        id_sede = m.id_sede,
                        nombre_Provincia = p_join == null ? string.Empty : p_join.nombre_provincia,
                        nombre_Municipio = mun_join == null ? string.Empty : mun_join.nombre_municipio,
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
                if (obj.id_miembro == 0)
                {
                    nuevoVisitante = new Miembro
                    {
                        numero_miembro = obj.numero_miembro,
                        nombre_miembro = obj.nombre_miembro,
                        apellidos_miembro = obj.apellidos_miembro,
                        id_sede = obj.id_sede,
                        estado = "Visitante",
                        edad = obj.edad,
                        sexo = obj.sexo,
                        telefono_movil = obj.telefono_movil,
                        correo_electronico = obj.correo_electronico,
                        direccion = obj.direccion,
                        codigo_Postal = obj.codigo_Postal,
                        idProvincia = obj.idProvincia,
                        idMunicipio = obj.idMunicipio,
                        fecha_llegada_iglesia = obj.fecha_llegada_iglesia,
                        pais_nacimiento = obj.pais_nacimiento,
                        acepta_LOPD = obj.acepta_LOPD,
                    };

                    _context.Miembros.Add(nuevoVisitante);
                    _context.SaveChanges();
                }
                else
                {
                    nuevoVisitante = _context.Miembros.First(m => m.id_miembro == obj.id_miembro);

                    nuevoVisitante.numero_miembro = obj.numero_miembro;
                    nuevoVisitante.nombre_miembro = obj.nombre_miembro;
                    nuevoVisitante.apellidos_miembro = obj.apellidos_miembro;
                    nuevoVisitante.edad = obj.edad;
                    nuevoVisitante.sexo = obj.sexo;
                    nuevoVisitante.telefono_movil = obj.telefono_movil;
                    nuevoVisitante.correo_electronico = obj.correo_electronico;
                    nuevoVisitante.direccion = obj.direccion;
                    nuevoVisitante.codigo_Postal = obj.codigo_Postal;
                    nuevoVisitante.idProvincia = obj.idProvincia;
                    nuevoVisitante.idMunicipio = obj.idMunicipio;
                    nuevoVisitante.fecha_llegada_iglesia = obj.fecha_llegada_iglesia;
                    nuevoVisitante.pais_nacimiento = obj.pais_nacimiento;
                    nuevoVisitante.acepta_LOPD = obj.acepta_LOPD;

                    _context.SaveChanges();
                }

                mensaje = "Visitante registrado correctamente.";
                return nuevoVisitante.id_miembro;
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
                var miembroExistente = _context.Miembros.FirstOrDefault(m => m.id_miembro == obj.id_miembro);
                if (miembroExistente == null)
                {
                    mensaje = "No se encontró el miembro especificado.";
                    return false;
                }

                // Actualizar propiedades
                miembroExistente.nombre_miembro = obj.nombre_miembro;
                miembroExistente.apellidos_miembro = obj.apellidos_miembro;
                miembroExistente.id_sede = sedeID;
                miembroExistente.edad = obj.edad;
                miembroExistente.sexo = obj.sexo;
                miembroExistente.estado = "Visitante";
                miembroExistente.telefono_movil = obj.telefono_movil;
                miembroExistente.correo_electronico = obj.correo_electronico;
                miembroExistente.direccion = obj.direccion;
                miembroExistente.codigo_Postal = obj.codigo_Postal;
                miembroExistente.idProvincia = obj.idProvincia;
                miembroExistente.idMunicipio = obj.idMunicipio;
                miembroExistente.fecha_llegada_iglesia = obj.fecha_llegada_iglesia;
                miembroExistente.pais_nacimiento = obj.pais_nacimiento;
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
                    .Where(m => m.estado == "Simpatizante");

                if (sedeID != 1000)
                {
                    consultaBase = consultaBase.Where(m => m.id_sede == sedeID);
                }

                var visitantesDetalle = (
                    from m in consultaBase
                    join p in _context.Provincia on m.idProvincia equals p.idProvincia into provincias
                    from p_join in provincias.DefaultIfEmpty()
                    join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into municipios
                    from mun_join in municipios.DefaultIfEmpty()

                    select new MiembroDetalleDTO
                    {
                        id_miembro = m.id_miembro,
                        nombre_miembro = m.nombre_miembro,
                        apellidos_miembro = m.apellidos_miembro,
                        correo_electronico = m.correo_electronico,
                        telefono_movil = m.telefono_movil,
                        telefono_fijo = m.telefono_fijo,
                        edad = m.edad,
                        sexo = m.sexo,
                        direccion = m.direccion,
                        codigo_Postal = m.codigo_Postal,
                        idProvincia = m.idProvincia,
                        idMunicipio = m.idMunicipio,
                        fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                        fecha_cumpleanios = m.fecha_cumpleanios,
                        fecha_boda = m.fecha_boda,
                        fecha_bautismo = m.fecha_bautismo,
                        lugar_bautismo = m.lugar_bautismo,
                        estado_Civil = m.estado_Civil,
                        pais_nacimiento = m.pais_nacimiento,
                        bautizado = m.bautizado,
                        numero_hijos = m.numero_hijos,
                        acepta_LOPD = m.acepta_LOPD,
                        responsable = m.responsable,
                        observaciones = m.observaciones,
                        id_sede = m.id_sede,
                        nombre_Provincia = p_join == null ? string.Empty : p_join.nombre_provincia,
                        nombre_Municipio = mun_join == null ? string.Empty : mun_join.nombre_municipio,
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
                if (obj.id_miembro == 0)
                {
                    nuevoVisitante = new Miembro
                    {
                        numero_miembro = obj.numero_miembro,
                        nombre_miembro = obj.nombre_miembro,
                        apellidos_miembro = obj.apellidos_miembro,
                        id_sede = obj.id_sede,
                        estado = "Simpatizante",
                        edad = obj.edad,
                        sexo = obj.sexo,
                        telefono_movil = obj.telefono_movil,
                        telefono_fijo = obj.telefono_fijo,
                        correo_electronico = obj.correo_electronico,
                        direccion = obj.direccion,
                        codigo_Postal = obj.codigo_Postal,
                        idProvincia = obj.idProvincia,
                        idMunicipio = obj.idMunicipio,
                        pais_nacimiento = obj.pais_nacimiento,
                        estado_Civil = obj.estado_Civil,
                        bautizado = obj.bautizado,
                        lugar_bautismo = obj.lugar_bautismo,
                        fecha_bautismo = obj.fecha_bautismo,
                        fecha_cumpleanios = obj.fecha_cumpleanios,
                        fecha_boda = obj.fecha_boda,
                        fecha_llegada_iglesia = obj.fecha_llegada_iglesia,
                        responsable = obj.responsable,
                        observaciones = obj.observaciones,
                        numero_hijos = obj.numero_hijos,
                        acepta_LOPD = obj.acepta_LOPD,
                    };

                    _context.Miembros.Add(nuevoVisitante);
                    _context.SaveChanges();
                }
                else
                {
                    nuevoVisitante = _context.Miembros.First(m => m.id_miembro == obj.id_miembro);

                    nuevoVisitante.numero_miembro = obj.numero_miembro;
                    nuevoVisitante.nombre_miembro = obj.nombre_miembro;
                    nuevoVisitante.apellidos_miembro = obj.apellidos_miembro;
                    nuevoVisitante.edad = obj.edad;
                    nuevoVisitante.sexo = obj.sexo;
                    nuevoVisitante.telefono_movil = obj.telefono_movil;
                    nuevoVisitante.telefono_fijo = obj.telefono_fijo;
                    nuevoVisitante.correo_electronico = obj.correo_electronico;
                    nuevoVisitante.direccion = obj.direccion;
                    nuevoVisitante.codigo_Postal = obj.codigo_Postal;
                    nuevoVisitante.idProvincia = obj.idProvincia;
                    nuevoVisitante.idMunicipio = obj.idMunicipio;
                    nuevoVisitante.pais_nacimiento = obj.pais_nacimiento;
                    nuevoVisitante.estado_Civil = obj.estado_Civil;
                    nuevoVisitante.bautizado = obj.bautizado;
                    nuevoVisitante.lugar_bautismo = obj.lugar_bautismo;
                    nuevoVisitante.fecha_bautismo = obj.fecha_bautismo;
                    nuevoVisitante.fecha_cumpleanios = obj.fecha_cumpleanios;
                    nuevoVisitante.fecha_boda = obj.fecha_boda;
                    nuevoVisitante.fecha_llegada_iglesia = obj.fecha_llegada_iglesia;
                    nuevoVisitante.responsable = obj.responsable;
                    nuevoVisitante.observaciones = obj.observaciones;
                    nuevoVisitante.numero_hijos = obj.numero_hijos;
                    nuevoVisitante.acepta_LOPD = obj.acepta_LOPD;

                    _context.SaveChanges();
                }

                mensaje = "Simpatizante registrado correctamente.";
                return nuevoVisitante.id_miembro;
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
                var miembroExistente = _context.Miembros.FirstOrDefault(m => m.id_miembro == obj.id_miembro);
                if (miembroExistente == null)
                {
                    mensaje = "No se encontró el miembro especificado.";
                    return false;
                }

                // Actualizar propiedades
                miembroExistente.numero_miembro = obj.numero_miembro;
                miembroExistente.nombre_miembro = obj.nombre_miembro;
                miembroExistente.apellidos_miembro = obj.apellidos_miembro;
                miembroExistente.edad = obj.edad;
                miembroExistente.sexo = obj.sexo;
                miembroExistente.telefono_movil = obj.telefono_movil;
                miembroExistente.correo_electronico = obj.correo_electronico;
                miembroExistente.direccion = obj.direccion;
                miembroExistente.codigo_Postal = obj.codigo_Postal;
                miembroExistente.idProvincia = obj.idProvincia;
                miembroExistente.idMunicipio = obj.idMunicipio;
                miembroExistente.pais_nacimiento = obj.pais_nacimiento;
                miembroExistente.estado_Civil = obj.estado_Civil;
                miembroExistente.bautizado = obj.bautizado;
                miembroExistente.lugar_bautismo = obj.lugar_bautismo;
                miembroExistente.fecha_bautismo = obj.fecha_bautismo;
                miembroExistente.fecha_cumpleanios = obj.fecha_cumpleanios;
                miembroExistente.fecha_boda = obj.fecha_boda;
                miembroExistente.fecha_llegada_iglesia = obj.fecha_llegada_iglesia;
                miembroExistente.telefono_fijo = obj.telefono_fijo;
                miembroExistente.observaciones = obj.observaciones;
                miembroExistente.responsable = obj.responsable;
                miembroExistente.numero_hijos = obj.numero_hijos;
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
                 .Where(m => m.estado == "Proceso");
                if (sedeID != 1000)
                {
                    consultaMiembros = consultaMiembros.Where(m => m.id_sede == sedeID);
                }

                var miembrosConUbicacion = (
                        from m in consultaMiembros
                        join p in _context.Provincia on m.idProvincia equals p.idProvincia into provGroup
                        from p in provGroup.DefaultIfEmpty()
                        join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into munGroup
                        from mun in munGroup.DefaultIfEmpty()

                        select new MiembroDetalleDTO
                        {
                            id_miembro = m.id_miembro,
                            diezmo_individual = m.diezmo_individual,
                            diezmo_familiar = m.diezmo_familiar,
                            numero_miembro = m.numero_miembro,
                            nombre_miembro = m.nombre_miembro,
                            apellidos_miembro = m.apellidos_miembro,
                            edad = m.edad,
                            sexo = m.sexo,
                            telefono_fijo = m.telefono_fijo,
                            telefono_movil = m.telefono_movil,
                            correo_electronico = m.correo_electronico,
                            direccion = m.direccion,
                            codigo_Postal = m.codigo_Postal,
                            idProvincia = m.idProvincia,
                            idMunicipio = m.idMunicipio,
                            id_sede = m.id_sede,
                            estado = m.estado,
                            pais_nacimiento = m.pais_nacimiento,
                            estado_Civil = m.estado_Civil,
                            combinar_diezmo = m.combinar_diezmo,
                            excluir_directorio = m.excluir_directorio,
                            fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                            fecha_baja = m.fecha_baja,
                            bautizado = m.bautizado,
                            fecha_bautismo = m.fecha_bautismo,
                            lugar_bautismo = m.lugar_bautismo,
                            fecha_cumpleanios = m.fecha_cumpleanios,
                            fecha_fallecido = m.fecha_fallecido,
                            fecha_boda = m.fecha_boda,
                            acepta_LOPD = m.acepta_LOPD,
                            observaciones = m.observaciones,
                            numero_hijos = m.numero_hijos,
                            alumno_VyF = m.alumno_VyF,
                            curso_acabado = m.curso_acabado,
                            responsable = m.responsable,
                            id_role = m.id_role,
                            id_usuario = m.id_usuario,

                            nombre_Provincia = p.nombre_provincia, // Nombre traído del JOIN
                            nombre_Municipio = mun.nombre_municipio, // Nombre traído del JOIN

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
                if (obj.id_miembro == 0)
                {
                    nuevoVisitante = new Miembro
                    {
                        numero_miembro = obj.numero_miembro,
                        nombre_miembro = obj.nombre_miembro,
                        apellidos_miembro = obj.apellidos_miembro,
                        id_sede = obj.id_sede,
                        estado = "Proceso",
                        edad = obj.edad,
                        sexo = obj.sexo,
                        telefono_movil = obj.telefono_movil,
                        correo_electronico = obj.correo_electronico,
                        direccion = obj.direccion,
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
                    nuevoVisitante = _context.Miembros.First(m => m.id_miembro == obj.id_miembro);

                    nuevoVisitante.numero_miembro = obj.numero_miembro;
                    nuevoVisitante.nombre_miembro = obj.nombre_miembro;
                    nuevoVisitante.apellidos_miembro = obj.apellidos_miembro;
                    nuevoVisitante.edad = obj.edad;
                    nuevoVisitante.sexo = obj.sexo;
                    nuevoVisitante.telefono_movil = obj.telefono_movil;


                    _context.SaveChanges();
                }

                mensaje = "Miembro en proceso registrado correctamente.";
                return nuevoVisitante.id_miembro;
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
                var miembroExistente = _context.Miembros.FirstOrDefault(m => m.id_miembro == obj.id_miembro);
                if (miembroExistente == null)
                {
                    mensaje = "No se encontró el miembro especificado.";
                    return false;
                }

                // Actualizar propiedades
                miembroExistente.nombre_miembro = obj.nombre_miembro;
                miembroExistente.apellidos_miembro = obj.apellidos_miembro;
                miembroExistente.id_sede = sedeID;
                miembroExistente.edad = obj.edad;
                miembroExistente.sexo = obj.sexo;
                miembroExistente.estado = "Proceso";
                miembroExistente.telefono_movil = obj.telefono_movil;
                miembroExistente.telefono_fijo = obj.telefono_fijo;
                miembroExistente.correo_electronico = obj.correo_electronico;
                miembroExistente.direccion = obj.direccion;
                miembroExistente.codigo_Postal = obj.codigo_Postal;
                miembroExistente.idProvincia = obj.idProvincia;
                miembroExistente.idMunicipio = obj.idMunicipio;
                miembroExistente.pais_nacimiento = obj.pais_nacimiento;
                miembroExistente.estado_Civil = obj.estado_Civil;
                miembroExistente.bautizado = obj.bautizado;
                miembroExistente.fecha_bautismo = obj.fecha_bautismo;
                miembroExistente.lugar_bautismo = obj.lugar_bautismo;
                miembroExistente.fecha_llegada_iglesia = obj.fecha_llegada_iglesia;
                miembroExistente.fecha_cumpleanios = obj.fecha_cumpleanios;
                miembroExistente.fecha_boda = obj.fecha_boda;
                miembroExistente.fecha_baja = obj.fecha_baja;
                miembroExistente.fecha_fallecido = obj.fecha_fallecido;
                miembroExistente.responsable = obj.responsable;
                miembroExistente.observaciones = obj.observaciones;
                miembroExistente.numero_hijos = obj.numero_hijos;
                miembroExistente.alumno_VyF = obj.alumno_VyF;
                miembroExistente.curso_acabado = obj.curso_acabado;
                miembroExistente.excluir_directorio = obj.excluir_directorio;
                miembroExistente.diezmo_individual = obj.diezmo_individual;
                miembroExistente.diezmo_familiar = obj.diezmo_familiar;
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
                var miembro = _context.Miembros.FirstOrDefault(m => m.id_miembro == ID_miembro);

                if (miembro == null)
                    return null;

                if (ID_sede != 1000 && miembro.id_sede != ID_sede)
                    throw new UnauthorizedAccessException("No puedes modificar miembros de otra sede.");

                // Avanzar estado según flujo Visitante → Simpatizante → Proceso → Miembro
                switch (miembro.estado)
                {
                    case "Visitante":
                        miembro.estado = "Simpatizante";
                        break;

                    case "Simpatizante":
                        miembro.estado = "Proceso";
                        break;

                    case "Proceso":
                        miembro.estado = "Miembro";
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
                var miembro = _context.Miembros.FirstOrDefault(m => m.id_miembro == ID_miembro);

                if (miembro == null)
                    return null;

                // Validar sede
                if (ID_sede != 1000 && miembro.id_sede != ID_sede)
                    throw new UnauthorizedAccessException("No puedes modificar miembros de otra sede.");

                switch (miembro.estado)
                {
                    case "Miembro":
                        miembro.estado = "Proceso";
                        break;

                    case "Proceso":
                        miembro.estado = "Simpatizante";
                        break;

                    case "Simpatizante":
                        miembro.estado = "Visitante";
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
                    .Where(m => m.id_miembro == ID_miembro);

                // Filtrar por sede si no es superadmin
                if (sedeID != 1000)
                {
                    consulta = consulta.Where(m => m.id_sede == sedeID);
                }

                var miembro = (
                    from m in consulta
                    join p in _context.Provincia on m.idProvincia equals p.idProvincia into provGroup
                    from p in provGroup.DefaultIfEmpty()

                    join mun in _context.Municipio on m.idMunicipio equals mun.idMunicipio into munGroup
                    from mun in munGroup.DefaultIfEmpty()

                    select new MiembroDetalleDTO
                    {
                        id_miembro = m.id_miembro,
                        diezmo_individual = m.diezmo_individual,
                        diezmo_familiar = m.diezmo_familiar,
                        numero_miembro = m.numero_miembro,
                        nombre_miembro = m.nombre_miembro,
                        apellidos_miembro = m.apellidos_miembro,
                        edad = m.edad,
                        sexo = m.sexo,
                        telefono_fijo = m.telefono_fijo,
                        telefono_movil = m.telefono_movil,
                        correo_electronico = m.correo_electronico,
                        direccion = m.direccion,
                        codigo_Postal = m.codigo_Postal,
                        idProvincia = m.idProvincia,
                        idMunicipio = m.idMunicipio,
                        id_sede = m.id_sede,
                        estado = m.estado,
                        pais_nacimiento = m.pais_nacimiento,
                        estado_Civil = m.estado_Civil,
                        combinar_diezmo = m.combinar_diezmo,
                        excluir_directorio = m.excluir_directorio,
                        fecha_llegada_iglesia = m.fecha_llegada_iglesia,
                        fecha_baja = m.fecha_baja,
                        bautizado = m.bautizado,
                        fecha_bautismo = m.fecha_bautismo,
                        lugar_bautismo = m.lugar_bautismo,
                        fecha_cumpleanios = m.fecha_cumpleanios,
                        fecha_fallecido = m.fecha_fallecido,
                        fecha_boda = m.fecha_boda,
                        acepta_LOPD = m.acepta_LOPD,
                        observaciones = m.observaciones,
                        numero_hijos = m.numero_hijos,
                        alumno_VyF = m.alumno_VyF,
                        curso_acabado = m.curso_acabado,
                        responsable = m.responsable,
                        id_role = m.id_role,
                        id_usuario = m.id_usuario,

                        nombre_Provincia = p != null ? p.nombre_provincia : string.Empty,
                        nombre_Municipio = mun != null ? mun.nombre_municipio : string.Empty,

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