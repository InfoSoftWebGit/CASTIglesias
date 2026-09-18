-- NOTA: Ese UPDATE falla porque la columna ID_iglesia todavía no existe en permisos. El 4.1 solo genera los ALTER, y hay que ejecutar lo que devuelve; además, las variables @pastorales se pierden si Workbench se reconecta. Para evitar ese lío, ahora que conozco tus 23 tablas, te paso el paso 4 completo con las sentencias escritas, sin generadores.

-- NOTA: Uso los nombres exactos de tus tablas (Lideres con mayúscula, porque en la NAS importan las mayúsculas).


-- =====================================================================
-- PASO 4 · IGLESIA EN LAS TABLAS PASTORALES (23 tablas)
-- =====================================================================
SET SQL_SAFE_UPDATES = 0;

-- ---------------------------------------------------------------------
-- 4.1 Añadir la columna. DEFAULT 0 = "sin iglesia": los triggers del 4.6
--     rechazan ese valor, así que nunca quedará una fila sin iglesia.
-- ---------------------------------------------------------------------
ALTER TABLE asistencia_culto              ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE bloque_culto                  ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE concepto                      ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE config_diezmo                 ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE config_jovenes                ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE culto                         ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE detalle_pago                  ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE detalle_seguimiento           ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE diezmo                        ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE eventos_calendario            ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE familias                      ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE gastos                        ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE grupos                        ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE Lideres                       ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE matrimonios                   ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE miembros                      ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE miembro_zona_grupo_ministerio ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE ministerio                    ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE permisos                      ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE requerimiento_culto           ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE salas                         ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE seguimiento                   ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;
ALTER TABLE zonas                         ADD COLUMN ID_iglesia INT NOT NULL DEFAULT 0;

-- ---------------------------------------------------------------------
-- 4.2 Rellenar la iglesia a partir de la sede (la 1000 se trata en el 4.4)
-- ---------------------------------------------------------------------
UPDATE asistencia_culto t              JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE bloque_culto t                  JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE concepto t                      JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE config_diezmo t                 JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE config_jovenes t                JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE culto t                         JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE detalle_pago t                  JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE detalle_seguimiento t           JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE diezmo t                        JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE eventos_calendario t            JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE familias t                      JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE gastos t                        JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE grupos t                        JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE Lideres t                       JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE matrimonios t                   JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE miembros t                      JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE miembro_zona_grupo_ministerio t JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE ministerio t                    JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE requerimiento_culto t           JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE salas t                         JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE seguimiento t                   JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;
UPDATE zonas t                         JOIN sedes s ON s.ID = t.ID_sede SET t.ID_iglesia = s.ID_iglesia WHERE t.ID_iglesia = 0 AND t.ID_sede <> 1000;

-- ---------------------------------------------------------------------
-- 4.3 permisos: su ID_sede suele ser 1000, así que la iglesia sale del usuario
--     (los permisos de los 2 administradores quedan en la iglesia interna)
-- ---------------------------------------------------------------------
UPDATE permisos p JOIN usuarios u ON u.ID_usuario = p.ID_usuario
SET p.ID_iglesia = u.ID_iglesia
WHERE p.ID_iglesia = 0;

-- ---------------------------------------------------------------------
-- 4.4 Filas que siguen sin iglesia (sede 1000 o sede inexistente)
-- ---------------------------------------------------------------------
-- 4.4.a Ver cuántas hay
SELECT 'asistencia_culto' tabla, COUNT(*) filas FROM asistencia_culto WHERE ID_iglesia = 0 UNION ALL
SELECT 'bloque_culto', COUNT(*) FROM bloque_culto WHERE ID_iglesia = 0 UNION ALL
SELECT 'concepto', COUNT(*) FROM concepto WHERE ID_iglesia = 0 UNION ALL
SELECT 'config_diezmo', COUNT(*) FROM config_diezmo WHERE ID_iglesia = 0 UNION ALL
SELECT 'config_jovenes', COUNT(*) FROM config_jovenes WHERE ID_iglesia = 0 UNION ALL
SELECT 'culto', COUNT(*) FROM culto WHERE ID_iglesia = 0 UNION ALL
SELECT 'detalle_pago', COUNT(*) FROM detalle_pago WHERE ID_iglesia = 0 UNION ALL
SELECT 'detalle_seguimiento', COUNT(*) FROM detalle_seguimiento WHERE ID_iglesia = 0 UNION ALL
SELECT 'diezmo', COUNT(*) FROM diezmo WHERE ID_iglesia = 0 UNION ALL
SELECT 'eventos_calendario', COUNT(*) FROM eventos_calendario WHERE ID_iglesia = 0 UNION ALL
SELECT 'familias', COUNT(*) FROM familias WHERE ID_iglesia = 0 UNION ALL
SELECT 'gastos', COUNT(*) FROM gastos WHERE ID_iglesia = 0 UNION ALL
SELECT 'grupos', COUNT(*) FROM grupos WHERE ID_iglesia = 0 UNION ALL
SELECT 'Lideres', COUNT(*) FROM Lideres WHERE ID_iglesia = 0 UNION ALL
SELECT 'matrimonios', COUNT(*) FROM matrimonios WHERE ID_iglesia = 0 UNION ALL
SELECT 'miembros', COUNT(*) FROM miembros WHERE ID_iglesia = 0 UNION ALL
SELECT 'miembro_zona_grupo_ministerio', COUNT(*) FROM miembro_zona_grupo_ministerio WHERE ID_iglesia = 0 UNION ALL
SELECT 'ministerio', COUNT(*) FROM ministerio WHERE ID_iglesia = 0 UNION ALL
SELECT 'permisos', COUNT(*) FROM permisos WHERE ID_iglesia = 0 UNION ALL
SELECT 'requerimiento_culto', COUNT(*) FROM requerimiento_culto WHERE ID_iglesia = 0 UNION ALL
SELECT 'salas', COUNT(*) FROM salas WHERE ID_iglesia = 0 UNION ALL
SELECT 'seguimiento', COUNT(*) FROM seguimiento WHERE ID_iglesia = 0 UNION ALL
SELECT 'zonas', COUNT(*) FROM zonas WHERE ID_iglesia = 0;

-- 4.4.b Solo hay una iglesia real (Vida y Familia, ID 1), así que todo lo que
--       quede sin iglesia es suyo. Sin esto, el paso 4.5 fallaría por la FK.
UPDATE asistencia_culto              SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE bloque_culto                  SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE concepto                      SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE config_diezmo                 SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE config_jovenes                SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE culto                         SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE detalle_pago                  SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE detalle_seguimiento           SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE diezmo                        SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE eventos_calendario            SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE familias                      SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE gastos                        SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE grupos                        SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE Lideres                       SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE matrimonios                   SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE miembros                      SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE miembro_zona_grupo_ministerio SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE ministerio                    SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE permisos                      SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE requerimiento_culto           SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE salas                         SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE seguimiento                   SET ID_iglesia = 1 WHERE ID_iglesia = 0;
UPDATE zonas                         SET ID_iglesia = 1 WHERE ID_iglesia = 0;

-- ---------------------------------------------------------------------
-- 4.5 Índice (iglesia, sede) para las consultas filtradas + FK a iglesias
-- ---------------------------------------------------------------------
ALTER TABLE asistencia_culto              ADD INDEX ix_asistencia_culto_igl_sede (ID_iglesia, ID_sede),              ADD CONSTRAINT fk_asistencia_culto_iglesia              FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE bloque_culto                  ADD INDEX ix_bloque_culto_igl_sede (ID_iglesia, ID_sede),                  ADD CONSTRAINT fk_bloque_culto_iglesia                  FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE concepto                      ADD INDEX ix_concepto_igl_sede (ID_iglesia, ID_sede),                      ADD CONSTRAINT fk_concepto_iglesia                      FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE config_diezmo                 ADD INDEX ix_config_diezmo_igl_sede (ID_iglesia, ID_sede),                 ADD CONSTRAINT fk_config_diezmo_iglesia                 FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE config_jovenes                ADD INDEX ix_config_jovenes_igl_sede (ID_iglesia, ID_sede),                ADD CONSTRAINT fk_config_jovenes_iglesia                FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE culto                         ADD INDEX ix_culto_igl_sede (ID_iglesia, ID_sede),                         ADD CONSTRAINT fk_culto_iglesia                         FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE detalle_pago                  ADD INDEX ix_detalle_pago_igl_sede (ID_iglesia, ID_sede),                  ADD CONSTRAINT fk_detalle_pago_iglesia                  FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE detalle_seguimiento           ADD INDEX ix_detalle_seguimiento_igl_sede (ID_iglesia, ID_sede),           ADD CONSTRAINT fk_detalle_seguimiento_iglesia           FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE diezmo                        ADD INDEX ix_diezmo_igl_sede (ID_iglesia, ID_sede),                        ADD CONSTRAINT fk_diezmo_iglesia                        FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE eventos_calendario            ADD INDEX ix_eventos_calendario_igl_sede (ID_iglesia, ID_sede),            ADD CONSTRAINT fk_eventos_calendario_iglesia            FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE familias                      ADD INDEX ix_familias_igl_sede (ID_iglesia, ID_sede),                      ADD CONSTRAINT fk_familias_iglesia                      FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE gastos                        ADD INDEX ix_gastos_igl_sede (ID_iglesia, ID_sede),                        ADD CONSTRAINT fk_gastos_iglesia                        FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE grupos                        ADD INDEX ix_grupos_igl_sede (ID_iglesia, ID_sede),                        ADD CONSTRAINT fk_grupos_iglesia                        FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE Lideres                       ADD INDEX ix_lideres_igl_sede (ID_iglesia, ID_sede),                       ADD CONSTRAINT fk_lideres_iglesia                       FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE matrimonios                   ADD INDEX ix_matrimonios_igl_sede (ID_iglesia, ID_sede),                   ADD CONSTRAINT fk_matrimonios_iglesia                   FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE miembros                      ADD INDEX ix_miembros_igl_sede (ID_iglesia, ID_sede),                      ADD CONSTRAINT fk_miembros_iglesia                      FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE miembro_zona_grupo_ministerio ADD INDEX ix_mzgm_igl_sede (ID_iglesia, ID_sede),                          ADD CONSTRAINT fk_mzgm_iglesia                          FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE ministerio                    ADD INDEX ix_ministerio_igl_sede (ID_iglesia, ID_sede),                    ADD CONSTRAINT fk_ministerio_iglesia                    FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE permisos                      ADD INDEX ix_permisos_igl_sede (ID_iglesia, ID_sede),                      ADD CONSTRAINT fk_permisos_iglesia                      FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE requerimiento_culto           ADD INDEX ix_requerimiento_culto_igl_sede (ID_iglesia, ID_sede),           ADD CONSTRAINT fk_requerimiento_culto_iglesia           FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE salas                         ADD INDEX ix_salas_igl_sede (ID_iglesia, ID_sede),                         ADD CONSTRAINT fk_salas_iglesia                         FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE seguimiento                   ADD INDEX ix_seguimiento_igl_sede (ID_iglesia, ID_sede),                   ADD CONSTRAINT fk_seguimiento_iglesia                   FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
ALTER TABLE zonas                         ADD INDEX ix_zonas_igl_sede (ID_iglesia, ID_sede),                         ADD CONSTRAINT fk_zonas_iglesia                         FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);
4.6 Triggers de validación
Hay dos por tabla, y en todas hacen lo mismo:

Al insertar: si el registro llega sin iglesia, la saca de la sede. Si llega con iglesia, la sede tiene que ser de esa iglesia. La 1000 («todas») exige que el código traiga la iglesia.
Al editar: la iglesia no cambia nunca, y una sede nueva tiene que ser de la misma iglesia.
Ejecútalo como un único bloque, con los DELIMITER incluidos:


DELIMITER $$

-- asistencia_culto
DROP TRIGGER IF EXISTS trg_asistencia_culto_igl_bi$$
CREATE TRIGGER trg_asistencia_culto_igl_bi BEFORE INSERT ON asistencia_culto FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_asistencia_culto_igl_bu$$
CREATE TRIGGER trg_asistencia_culto_igl_bu BEFORE UPDATE ON asistencia_culto FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- bloque_culto
DROP TRIGGER IF EXISTS trg_bloque_culto_igl_bi$$
CREATE TRIGGER trg_bloque_culto_igl_bi BEFORE INSERT ON bloque_culto FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_bloque_culto_igl_bu$$
CREATE TRIGGER trg_bloque_culto_igl_bu BEFORE UPDATE ON bloque_culto FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- concepto
DROP TRIGGER IF EXISTS trg_concepto_igl_bi$$
CREATE TRIGGER trg_concepto_igl_bi BEFORE INSERT ON concepto FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_concepto_igl_bu$$
CREATE TRIGGER trg_concepto_igl_bu BEFORE UPDATE ON concepto FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- config_diezmo
DROP TRIGGER IF EXISTS trg_config_diezmo_igl_bi$$
CREATE TRIGGER trg_config_diezmo_igl_bi BEFORE INSERT ON config_diezmo FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_config_diezmo_igl_bu$$
CREATE TRIGGER trg_config_diezmo_igl_bu BEFORE UPDATE ON config_diezmo FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- config_jovenes
DROP TRIGGER IF EXISTS trg_config_jovenes_igl_bi$$
CREATE TRIGGER trg_config_jovenes_igl_bi BEFORE INSERT ON config_jovenes FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_config_jovenes_igl_bu$$
CREATE TRIGGER trg_config_jovenes_igl_bu BEFORE UPDATE ON config_jovenes FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- culto
DROP TRIGGER IF EXISTS trg_culto_igl_bi$$
CREATE TRIGGER trg_culto_igl_bi BEFORE INSERT ON culto FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_culto_igl_bu$$
CREATE TRIGGER trg_culto_igl_bu BEFORE UPDATE ON culto FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- detalle_pago
DROP TRIGGER IF EXISTS trg_detalle_pago_igl_bi$$
CREATE TRIGGER trg_detalle_pago_igl_bi BEFORE INSERT ON detalle_pago FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_detalle_pago_igl_bu$$
CREATE TRIGGER trg_detalle_pago_igl_bu BEFORE UPDATE ON detalle_pago FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- detalle_seguimiento
DROP TRIGGER IF EXISTS trg_detalle_seguimiento_igl_bi$$
CREATE TRIGGER trg_detalle_seguimiento_igl_bi BEFORE INSERT ON detalle_seguimiento FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_detalle_seguimiento_igl_bu$$
CREATE TRIGGER trg_detalle_seguimiento_igl_bu BEFORE UPDATE ON detalle_seguimiento FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- diezmo
DROP TRIGGER IF EXISTS trg_diezmo_igl_bi$$
CREATE TRIGGER trg_diezmo_igl_bi BEFORE INSERT ON diezmo FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_diezmo_igl_bu$$
CREATE TRIGGER trg_diezmo_igl_bu BEFORE UPDATE ON diezmo FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- eventos_calendario
DROP TRIGGER IF EXISTS trg_eventos_calendario_igl_bi$$
CREATE TRIGGER trg_eventos_calendario_igl_bi BEFORE INSERT ON eventos_calendario FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_eventos_calendario_igl_bu$$
CREATE TRIGGER trg_eventos_calendario_igl_bu BEFORE UPDATE ON eventos_calendario FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- familias
DROP TRIGGER IF EXISTS trg_familias_igl_bi$$
CREATE TRIGGER trg_familias_igl_bi BEFORE INSERT ON familias FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_familias_igl_bu$$
CREATE TRIGGER trg_familias_igl_bu BEFORE UPDATE ON familias FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- gastos (convive con trg_gastos_numero_pago)
DROP TRIGGER IF EXISTS trg_gastos_igl_bi$$
CREATE TRIGGER trg_gastos_igl_bi BEFORE INSERT ON gastos FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_gastos_igl_bu$$
CREATE TRIGGER trg_gastos_igl_bu BEFORE UPDATE ON gastos FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- grupos
DROP TRIGGER IF EXISTS trg_grupos_igl_bi$$
CREATE TRIGGER trg_grupos_igl_bi BEFORE INSERT ON grupos FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_grupos_igl_bu$$
CREATE TRIGGER trg_grupos_igl_bu BEFORE UPDATE ON grupos FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- Lideres
DROP TRIGGER IF EXISTS trg_lideres_igl_bi$$
CREATE TRIGGER trg_lideres_igl_bi BEFORE INSERT ON Lideres FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_lideres_igl_bu$$
CREATE TRIGGER trg_lideres_igl_bu BEFORE UPDATE ON Lideres FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- matrimonios
DROP TRIGGER IF EXISTS trg_matrimonios_igl_bi$$
CREATE TRIGGER trg_matrimonios_igl_bi BEFORE INSERT ON matrimonios FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_matrimonios_igl_bu$$
CREATE TRIGGER trg_matrimonios_igl_bu BEFORE UPDATE ON matrimonios FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- miembros
DROP TRIGGER IF EXISTS trg_miembros_igl_bi$$
CREATE TRIGGER trg_miembros_igl_bi BEFORE INSERT ON miembros FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_miembros_igl_bu$$
CREATE TRIGGER trg_miembros_igl_bu BEFORE UPDATE ON miembros FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- miembro_zona_grupo_ministerio
DROP TRIGGER IF EXISTS trg_mzgm_igl_bi$$
CREATE TRIGGER trg_mzgm_igl_bi BEFORE INSERT ON miembro_zona_grupo_ministerio FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_mzgm_igl_bu$$
CREATE TRIGGER trg_mzgm_igl_bu BEFORE UPDATE ON miembro_zona_grupo_ministerio FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- ministerio
DROP TRIGGER IF EXISTS trg_ministerio_igl_bi$$
CREATE TRIGGER trg_ministerio_igl_bi BEFORE INSERT ON ministerio FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_ministerio_igl_bu$$
CREATE TRIGGER trg_ministerio_igl_bu BEFORE UPDATE ON ministerio FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- permisos (su ID_sede suele ser 1000: la iglesia la pone el código)
DROP TRIGGER IF EXISTS trg_permisos_igl_bi$$
CREATE TRIGGER trg_permisos_igl_bi BEFORE INSERT ON permisos FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_permisos_igl_bu$$
CREATE TRIGGER trg_permisos_igl_bu BEFORE UPDATE ON permisos FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- requerimiento_culto
DROP TRIGGER IF EXISTS trg_requerimiento_culto_igl_bi$$
CREATE TRIGGER trg_requerimiento_culto_igl_bi BEFORE INSERT ON requerimiento_culto FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_requerimiento_culto_igl_bu$$
CREATE TRIGGER trg_requerimiento_culto_igl_bu BEFORE UPDATE ON requerimiento_culto FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- salas
DROP TRIGGER IF EXISTS trg_salas_igl_bi$$
CREATE TRIGGER trg_salas_igl_bi BEFORE INSERT ON salas FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_salas_igl_bu$$
CREATE TRIGGER trg_salas_igl_bu BEFORE UPDATE ON salas FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- seguimiento
DROP TRIGGER IF EXISTS trg_seguimiento_igl_bi$$
CREATE TRIGGER trg_seguimiento_igl_bi BEFORE INSERT ON seguimiento FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_seguimiento_igl_bu$$
CREATE TRIGGER trg_seguimiento_igl_bu BEFORE UPDATE ON seguimiento FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

-- zonas
DROP TRIGGER IF EXISTS trg_zonas_igl_bi$$
CREATE TRIGGER trg_zonas_igl_bi BEFORE INSERT ON zonas FOR EACH ROW
BEGIN
  DECLARE v INT;
  IF NEW.ID_sede <> 1000 THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no existe'; END IF;
    IF NEW.ID_iglesia = 0 THEN SET NEW.ID_iglesia = v;
    ELSEIF NEW.ID_iglesia <> v THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede pertenece a otra iglesia'; END IF;
  END IF;
  IF NEW.ID_iglesia = 0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro sin iglesia'; END IF;
END$$
DROP TRIGGER IF EXISTS trg_zonas_igl_bu$$
CREATE TRIGGER trg_zonas_igl_bu BEFORE UPDATE ON zonas FOR EACH ROW
BEGIN
  DECLARE v INT;
  SET NEW.ID_iglesia = OLD.ID_iglesia;
  IF NEW.ID_sede <> 1000 AND NOT (NEW.ID_sede <=> OLD.ID_sede) THEN
    SET v = (SELECT ID_iglesia FROM sedes WHERE ID = NEW.ID_sede);
    IF v IS NULL OR v <> NEW.ID_iglesia THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del registro'; END IF;
  END IF;
END$$

DELIMITER ;

-- 4.7 Comprobación: deben salir 46 triggers "_igl_" (2 por tabla)
SELECT event_object_table, trigger_name
FROM information_schema.triggers
WHERE trigger_schema = DATABASE() AND trigger_name LIKE '%\_igl\_%'
ORDER BY event_object_table, trigger_name;
Notas:

Si el 4.1 falla en alguna tabla con «Duplicate column name», esa tabla ya tiene la columna: sáltate esa línea y sigue.
Si falla un trigger de gastos con un error del tipo «This version of MariaDB doesn't yet support multiple triggers», tu MariaDB es anterior a la 10.2.3. En ese caso pásame el resultado de SELECT VERSION(); y lo integro dentro del trigger trg_gastos_numero_pago que ya existe.
Recuerda que en los pasos 5, 6 y 7 hay que cambiar utf8mb4_0900_ai_ci por utf8mb4_unicode_ci.