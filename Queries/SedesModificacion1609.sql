-- =====================================================================
-- PASO 3 · SEDES
-- =====================================================================

-- 3.1 Quitar temporalmente la FK a iglesias: MariaDB no permite pasar a NOT NULL
--     una columna que tiene una FK
ALTER TABLE sedes DROP FOREIGN KEY sedes_ibfk_1;

-- 3.2 "O tiene iglesia o nada": toda sede pertenece a una iglesia
ALTER TABLE sedes MODIFY COLUMN ID_iglesia INT NOT NULL;

-- 3.3 Volver a crear la FK a iglesias, con nombre propio
ALTER TABLE sedes
  ADD CONSTRAINT fk_sedes_iglesia FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID);

-- 3.4 Datos nuevos de la sede y relaciones con usuarios
ALTER TABLE sedes
  ADD COLUMN codigo       VARCHAR(10) NULL COMMENT 'Prefijo de numeraciones: MAD, TOL' AFTER ID_iglesia,
  ADD COLUMN es_principal TINYINT(1)  NOT NULL DEFAULT 0,
  -- 1 en la principal y NULL en el resto: con el UNIQUE de abajo, una sola principal por iglesia
  ADD COLUMN marca_principal TINYINT GENERATED ALWAYS AS (IF(es_principal = 1, 1, NULL)) STORED,
  ADD COLUMN direccion    VARCHAR(200) NULL,
  ADD COLUMN cp           VARCHAR(10)  NULL,
  ADD COLUMN idProvincia  SMALLINT UNSIGNED NULL,
  ADD COLUMN idMunicipio  SMALLINT UNSIGNED NULL,
  ADD COLUMN email        VARCHAR(150) NULL,
  ADD COLUMN telefono     VARCHAR(20)  NULL,
  ADD COLUMN ID_responsable_pastoral   INT NULL,
  ADD COLUMN ID_responsable_financiero INT NULL,
  -- Inactiva = no admite operaciones nuevas, pero conserva su histórico
  ADD COLUMN estado ENUM('activa','inactiva') NOT NULL DEFAULT 'activa',
  ADD COLUMN fecha_apertura DATE NULL,
  ADD COLUMN creado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ADD UNIQUE KEY uk_sedes_iglesia_id        (ID_iglesia, ID),
  ADD UNIQUE KEY uk_sedes_iglesia_codigo    (ID_iglesia, codigo),
  ADD UNIQUE KEY uk_sedes_iglesia_principal (ID_iglesia, marca_principal),
  -- Los responsables son usuarios de la MISMA iglesia
  ADD CONSTRAINT fk_sedes_resp_pastoral   FOREIGN KEY (ID_iglesia, ID_responsable_pastoral)   REFERENCES usuarios (ID_iglesia, ID_usuario),
  ADD CONSTRAINT fk_sedes_resp_financiero FOREIGN KEY (ID_iglesia, ID_responsable_financiero) REFERENCES usuarios (ID_iglesia, ID_usuario);

-- 3.5 Los límites pasan a la suscripción (paso 7): la columna sobra
ALTER TABLE sedes DROP COLUMN ID_tipolicenica;

-- 3.6 Marcar como principal la sede real más antigua de cada iglesia
--     (en tu caso, Fuenlabrada; la 1000 es el marcador y se excluye)
UPDATE sedes s
JOIN (SELECT ID_iglesia, MIN(ID) AS id_min FROM sedes WHERE ID <> 1000 GROUP BY ID_iglesia) p
  ON p.id_min = s.ID
SET s.es_principal = 1;

-- 3.7 Comprobación
SELECT ID, ID_iglesia, codigo, nombre_sede, es_principal, estado FROM sedes;
