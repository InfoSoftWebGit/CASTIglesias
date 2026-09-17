-- =====================================================================
-- PASO 6 · REGISTRO DE ACCESOS DEL ADMINISTRADOR DE PLATAFORMA
-- =====================================================================
-- Cada entrada de un administrador en una iglesia cliente. Los datos de fe son
-- de categoría especial (art. 9 RGPD): cada acceso debe poder justificarse.
CREATE TABLE accesos_plataforma (
  ID          BIGINT AUTO_INCREMENT PRIMARY KEY,
  ID_usuario  INT NOT NULL,
  ID_iglesia  INT NOT NULL COMMENT 'Iglesia en la que entra',
  motivo      VARCHAR(300) NOT NULL,
  inicio      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  fin         DATETIME NULL COMMENT 'Al cambiar de iglesia o cerrar sesión',
  ip_hash     CHAR(64) NULL COMMENT 'SHA-256 de la IP',
  KEY ix_accesos_iglesia (ID_iglesia, inicio),
  KEY ix_accesos_abiertos (ID_usuario, fin),
  CONSTRAINT fk_acc_usuario FOREIGN KEY (ID_usuario) REFERENCES usuarios (ID_usuario),
  CONSTRAINT fk_acc_iglesia FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;