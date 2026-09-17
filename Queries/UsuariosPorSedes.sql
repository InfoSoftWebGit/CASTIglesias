-- =====================================================================
-- PASO 5 · USUARIO_SEDES (varias sedes concretas por usuario)
-- =====================================================================
-- El código todavía no la usa (siguiente paso). Se crea y se carga ya para no migrar dos veces.
CREATE TABLE usuario_sedes (
  ID          INT AUTO_INCREMENT PRIMARY KEY,
  ID_iglesia  INT NOT NULL,
  ID_usuario  INT NOT NULL,
  ID_sede     INT NULL COMMENT 'NULL = todas las sedes de la iglesia, también las futuras',
  -- El UNIQUE no compara NULL: esta columna lo convierte en 0 para evitar dos "todas"
  clave_sede  INT GENERATED ALWAYS AS (IFNULL(ID_sede, 0)) STORED,
  nivel       ENUM('consulta','operar','aprobar','administrar') NOT NULL,
  creado_en   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  creado_por  INT NULL,
  UNIQUE KEY uk_usuario_sedes (ID_usuario, clave_sede),
  CONSTRAINT fk_us_usuario FOREIGN KEY (ID_iglesia, ID_usuario) REFERENCES usuarios (ID_iglesia, ID_usuario),
  CONSTRAINT fk_us_sede    FOREIGN KEY (ID_iglesia, ID_sede)    REFERENCES sedes (ID_iglesia, ID),
  CONSTRAINT fk_us_creador FOREIGN KEY (creado_por)             REFERENCES usuarios (ID_usuario)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5.1 Comprobación previa: usuarios cuya sede no existe (harían fallar la carga).
--     Si sale alguno, corrige su ID_sede antes del 5.2.
SELECT ID_usuario, correo_electronico, ID_sede
FROM usuarios
WHERE ID_sede <> 1000 AND ID_sede NOT IN (SELECT ID FROM sedes);

-- 5.2 Carga inicial con lo que hoy hace el login
INSERT INTO usuario_sedes (ID_iglesia, ID_usuario, ID_sede, nivel)
SELECT u.ID_iglesia, u.ID_usuario,
       IF(u.ID_sede = 1000 OR u.Rol IN ('AdminGlobal','PastorGeneral'), NULL, u.ID_sede),
       CASE WHEN u.Rol IN ('AdminGlobal','PastorGeneral') THEN 'administrar'
            WHEN u.Rol = 'PastorSede' THEN 'aprobar'
            ELSE 'operar' END
FROM usuarios u
WHERE u.es_admin_plataforma = 0;

-- 5.3 Comprobación
SELECT us.*, u.correo_electronico, u.Rol
FROM usuario_sedes us JOIN usuarios u ON u.ID_usuario = us.ID_usuario;