-- 2.1 "O tiene iglesia o nada": NOT NULL y SIN valor por defecto (adiós al DEFAULT 1)
ALTER TABLE usuarios
  MODIFY COLUMN ID_iglesia INT NOT NULL,
  -- Solo se activa por SQL; ninguna pantalla lo escribe
  ADD COLUMN es_admin_plataforma TINYINT(1) NOT NULL DEFAULT 0,
  -- El correo identifica a la persona en el login: único en toda la plataforma
  ADD UNIQUE KEY uk_usuarios_correo (correo_electronico),
  -- Destino de las FK compuestas (usuario de la MISMA iglesia)
  ADD UNIQUE KEY uk_usuarios_iglesia_id (ID_iglesia, ID_usuario);

-- 2.2 Coherencia usuario-sede, comprobada por la BBDD aunque el código falle.
--     1000 = todas las sedes de SU iglesia; cualquier otra sede debe ser de su iglesia.
DELIMITER $$
DROP TRIGGER IF EXISTS trg_usuarios_igl_bi$$
CREATE TRIGGER trg_usuarios_igl_bi BEFORE INSERT ON usuarios FOR EACH ROW
BEGIN
  IF NEW.ID_iglesia IS NULL OR NEW.ID_iglesia = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Usuario sin iglesia';
  END IF;
  IF NEW.ID_sede <> 1000 AND NOT EXISTS (SELECT 1 FROM sedes s WHERE s.ID = NEW.ID_sede AND s.ID_iglesia = NEW.ID_iglesia) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del usuario';
  END IF;
END$$
DROP TRIGGER IF EXISTS trg_usuarios_igl_bu$$
CREATE TRIGGER trg_usuarios_igl_bu BEFORE UPDATE ON usuarios FOR EACH ROW
BEGIN
  IF NEW.ID_sede <> 1000 AND NOT EXISTS (SELECT 1 FROM sedes s WHERE s.ID = NEW.ID_sede AND s.ID_iglesia = NEW.ID_iglesia) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sede no pertenece a la iglesia del usuario';
  END IF;
END$$
DELIMITER ;

-- 2.3 Iglesia interna de Congrega: tu usuario pertenece a ella, nunca a Vida y Familia.
--     Si claveLicencia o maxUsuarios no existen en tu BBDD nueva, quítalos.
INSERT INTO iglesias (nombre_iglesia, nombre_legal, estado, activo, claveLicencia, maxUsuarios)
VALUES ('Congrega (soporte)', 'Congrega CRM', 'activa', 1, 'CONGREGA-INTERNA', 0);
SET @id_congrega = LAST_INSERT_ID();

-- 2.4 Los dos administradores pasan a la iglesia interna como administradores de plataforma
UPDATE usuarios
SET ID_iglesia = @id_congrega, ID_sede = 1000, Rol = 'AdminGlobal', es_admin_plataforma = 1
WHERE correo_electronico IN ('tiegomanuel@gmail.com', 'lezcano.ruben@gmail.com');

-- Comprobación: deben salir exactamente 2 filas
SELECT ID_usuario, correo_electronico, ID_iglesia, Rol, es_admin_plataforma
FROM usuarios WHERE es_admin_plataforma = 1;

