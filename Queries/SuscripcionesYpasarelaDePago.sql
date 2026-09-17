-- =====================================================================
-- PASO 7 · SUSCRIPCIONES Y PASARELA DE PAGO
-- =====================================================================

-- 7.1 Planes que vende Congrega (global)
CREATE TABLE planes (
  ID              INT AUTO_INCREMENT PRIMARY KEY,
  codigo          VARCHAR(30)  NOT NULL UNIQUE,
  nombre          VARCHAR(100) NOT NULL,
  descripcion     VARCHAR(500) NULL,
  precio_mensual  DECIMAL(19,4) NOT NULL,
  precio_anual    DECIMAL(19,4) NULL COMMENT 'NULL = sin opción anual',
  moneda          CHAR(3) NOT NULL DEFAULT 'EUR',
  max_usuarios    INT NOT NULL,
  max_sedes       INT NOT NULL,
  modulos         JSON NULL COMMENT 'p. ej. ["finanzas","jovenes"]',
  id_precio_mensual_pasarela VARCHAR(255) NULL COMMENT 'price_... de Stripe',
  id_precio_anual_pasarela   VARCHAR(255) NULL,
  activo          TINYINT(1) NOT NULL DEFAULT 1,
  orden           INT NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7.2 Contrato de cada iglesia; los límites se COPIAN del plan al contratar
CREATE TABLE suscripciones (
  ID               INT AUTO_INCREMENT PRIMARY KEY,
  ID_iglesia       INT NOT NULL,
  ID_plan          INT NOT NULL,
  periodicidad     ENUM('mensual','anual') NOT NULL,
  estado           ENUM('prueba','activa','impagada','cancelada','expirada') NOT NULL,
  max_usuarios     INT NOT NULL,
  max_sedes        INT NOT NULL,
  precio           DECIMAL(19,4) NOT NULL,
  moneda           CHAR(3) NOT NULL DEFAULT 'EUR',
  fecha_inicio     DATETIME NOT NULL,
  fin_periodo      DATETIME NOT NULL COMMENT 'Acceso pagado hasta esta fecha',
  fin_prueba       DATETIME NULL,
  cancelar_al_final TINYINT(1) NOT NULL DEFAULT 0,
  cancelada_en     DATETIME NULL,
  pasarela         ENUM('stripe','redsys') NOT NULL,
  id_cliente_pasarela     VARCHAR(255) NULL COMMENT 'Un cliente de Stripe por iglesia',
  id_suscripcion_pasarela VARCHAR(255) NULL UNIQUE,
  -- Una sola suscripción no terminada por iglesia
  marca_vigente    TINYINT GENERATED ALWAYS AS (IF(estado IN ('prueba','activa','impagada'), 1, NULL)) STORED,
  creado_en        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  actualizado_en   DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uk_suscripciones_iglesia_id      (ID_iglesia, ID),
  UNIQUE KEY uk_suscripciones_iglesia_vigente (ID_iglesia, marca_vigente),
  CONSTRAINT fk_susc_iglesia FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID),
  CONSTRAINT fk_susc_plan    FOREIGN KEY (ID_plan)    REFERENCES planes (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7.3 Cada intento de pago: de qué iglesia viene, si tenía usuarios y si es su primer pago
CREATE TABLE solicitudes_pago (
  ID                  INT AUTO_INCREMENT PRIMARY KEY,
  referencia          CHAR(36) NOT NULL UNIQUE COMMENT 'UUID enviado a Stripe como client_reference_id',
  tipo                ENUM('alta','renovacion','cambio_plan','ampliacion') NOT NULL,
  ID_iglesia          INT NULL COMMENT 'NULL en un alta hasta que el webhook crea la iglesia',
  ID_usuario          INT NULL COMMENT 'Quién paga, si ya tenía sesión',
  es_primer_pago      TINYINT(1) NOT NULL,
  usuarios_existentes INT NOT NULL DEFAULT 0 COMMENT 'Foto en el momento del pago',
  nombre_iglesia      VARCHAR(150) NOT NULL COMMENT 'Nombre con el que se crea la iglesia',
  cif                 VARCHAR(20)  NULL,
  nombre_contacto     VARCHAR(100) NOT NULL,
  apellidos_contacto  VARCHAR(150) NULL,
  email_contacto      VARCHAR(150) NOT NULL COMMENT 'Aquí se envía la factura',
  telefono_contacto   VARCHAR(20)  NULL,
  codigo_pais         CHAR(2) NOT NULL DEFAULT 'ES',
  ciudad              VARCHAR(100) NULL,
  ID_plan             INT NOT NULL,
  periodicidad        ENUM('mensual','anual') NOT NULL,
  importe_base        DECIMAL(19,4) NOT NULL,
  importe_impuestos   DECIMAL(19,4) NOT NULL DEFAULT 0,
  importe_total       DECIMAL(19,4) NOT NULL,
  moneda              CHAR(3) NOT NULL DEFAULT 'EUR',
  pasarela            ENUM('stripe','redsys') NOT NULL,
  id_sesion_pasarela  VARCHAR(255) NULL UNIQUE,
  -- pagada: Stripe lo confirmó. iglesia_creada: iglesia + suscripción + aviso interno hechos.
  -- Separados para poder repetir la creación si falla, sin volver a cobrar.
  estado ENUM('pendiente','pagada','iglesia_creada','fallida','expirada','cancelada') NOT NULL DEFAULT 'pendiente',
  motivo_error          VARCHAR(500) NULL,
  aviso_interno_enviado TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Evita repetir el correo a soporte',
  contactado_en         DATETIME NULL COMMENT 'Cuándo se contactó y configuró la iglesia',
  contactado_por        INT NULL,
  creado_en             DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  expira_en             DATETIME NULL,
  pagado_en             DATETIME NULL,
  iglesia_creada_en     DATETIME NULL,
  KEY ix_solicitudes_email  (email_contacto),
  KEY ix_solicitudes_estado (estado, creado_en),
  CONSTRAINT fk_sol_iglesia    FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID),
  CONSTRAINT fk_sol_usuario    FOREIGN KEY (ID_iglesia, ID_usuario) REFERENCES usuarios (ID_iglesia, ID_usuario),
  CONSTRAINT fk_sol_plan       FOREIGN KEY (ID_plan) REFERENCES planes (ID),
  CONSTRAINT fk_sol_contactado FOREIGN KEY (contactado_por) REFERENCES usuarios (ID_usuario),
  CONSTRAINT ck_sol_alta CHECK (tipo <> 'alta' OR es_primer_pago = 1)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7.4 Facturas de Congrega a la iglesia; los datos fiscales se copian al emitir
CREATE TABLE facturas_suscripcion (
  ID               INT AUTO_INCREMENT PRIMARY KEY,
  ID_iglesia       INT NOT NULL,
  ID_suscripcion   INT NULL,
  serie            VARCHAR(10) NOT NULL,
  numero           INT NOT NULL,
  fecha_emision    DATE NOT NULL,
  razon_social     VARCHAR(200) NOT NULL,
  cif              VARCHAR(20)  NULL,
  direccion_fiscal VARCHAR(300) NULL,
  base_imponible   DECIMAL(19,4) NOT NULL,
  tipo_iva         DECIMAL(5,2)  NOT NULL,
  cuota_iva        DECIMAL(19,4) NOT NULL,
  total            DECIMAL(19,4) NOT NULL,
  moneda           CHAR(3) NOT NULL DEFAULT 'EUR',
  estado           ENUM('emitida','rectificada') NOT NULL DEFAULT 'emitida',
  ID_factura_rectificada INT NULL,
  id_factura_pasarela VARCHAR(255) NULL UNIQUE,
  url_pdf          VARCHAR(500) NULL,
  enviada_en       DATETIME NULL COMMENT 'Cuándo salió el correo con la factura',
  creado_en        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uk_facturas_numero     (serie, numero),
  UNIQUE KEY uk_facturas_iglesia_id (ID_iglesia, ID),
  CONSTRAINT fk_fac_iglesia FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID),
  CONSTRAINT fk_fac_susc    FOREIGN KEY (ID_iglesia, ID_suscripcion) REFERENCES suscripciones (ID_iglesia, ID),
  CONSTRAINT fk_fac_rect    FOREIGN KEY (ID_iglesia, ID_factura_rectificada) REFERENCES facturas_suscripcion (ID_iglesia, ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7.5 Cobros reales (alta y renovaciones). Sin datos de tarjeta.
CREATE TABLE pagos_suscripcion (
  ID               INT AUTO_INCREMENT PRIMARY KEY,
  ID_iglesia       INT NOT NULL,
  ID_suscripcion   INT NULL,
  ID_solicitud     INT NULL COMMENT 'Solo en el primer cobro',
  ID_factura       INT NULL,
  importe          DECIMAL(19,4) NOT NULL,
  importe_reembolsado DECIMAL(19,4) NOT NULL DEFAULT 0,
  moneda           CHAR(3) NOT NULL DEFAULT 'EUR',
  estado           ENUM('correcto','fallido','reembolsado','reembolso_parcial','disputado') NOT NULL,
  metodo           VARCHAR(30) NULL,
  motivo_fallo     VARCHAR(500) NULL,
  pasarela         ENUM('stripe','redsys') NOT NULL,
  id_pago_pasarela VARCHAR(255) NOT NULL UNIQUE COMMENT 'Evita duplicados si Stripe repite el aviso',
  fecha_pago       DATETIME NOT NULL,
  creado_en        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY ix_pagos_iglesia_fecha (ID_iglesia, fecha_pago),
  CONSTRAINT fk_pag_iglesia FOREIGN KEY (ID_iglesia) REFERENCES iglesias (ID),
  CONSTRAINT fk_pag_susc    FOREIGN KEY (ID_iglesia, ID_suscripcion) REFERENCES suscripciones (ID_iglesia, ID),
  CONSTRAINT fk_pag_fact    FOREIGN KEY (ID_iglesia, ID_factura)     REFERENCES facturas_suscripcion (ID_iglesia, ID),
  CONSTRAINT fk_pag_sol     FOREIGN KEY (ID_solicitud) REFERENCES solicitudes_pago (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7.6 Avisos de la pasarela tal cual llegan: idempotencia y reproceso
CREATE TABLE eventos_pasarela (
  ID           BIGINT AUTO_INCREMENT PRIMARY KEY,
  pasarela     ENUM('stripe','redsys') NOT NULL,
  id_evento    VARCHAR(255) NOT NULL,
  tipo         VARCHAR(100) NOT NULL,
  payload      JSON NOT NULL,
  estado       ENUM('pendiente','procesado','ignorado','error') NOT NULL DEFAULT 'pendiente',
  error        TEXT NULL,
  intentos     INT NOT NULL DEFAULT 0,
  recibido_en  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  procesado_en DATETIME NULL,
  UNIQUE KEY uk_eventos (pasarela, id_evento),
  KEY ix_eventos_estado (estado, recibido_en)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7.7 Comprobación: deben salir las 6 tablas
SELECT table_name, table_collation
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN ('planes','suscripciones','solicitudes_pago','facturas_suscripcion','pagos_suscripcion','eventos_pasarela');