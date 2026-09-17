-- Datos legales, de facturación y de contabilidad de cada iglesia (el inquilino).
-- Si alguna columna ya existe en tu BBDD nueva, quita su línea.
ALTER TABLE iglesias
  ADD COLUMN nombre_legal         VARCHAR(200) NULL AFTER nombre_iglesia,
  ADD COLUMN cif                  VARCHAR(20)  NULL,
  ADD COLUMN forma_juridica       ENUM('entidad_religiosa','asociacion','fundacion','otra') NULL,
  ADD COLUMN numero_rer           VARCHAR(30)  NULL COMMENT 'Registro de Entidades Religiosas',
  ADD COLUMN direccion            VARCHAR(200) NULL,
  ADD COLUMN cp                   VARCHAR(10)  NULL,
  ADD COLUMN idProvincia          SMALLINT UNSIGNED NULL,
  ADD COLUMN idMunicipio          SMALLINT UNSIGNED NULL,
  ADD COLUMN codigo_pais          CHAR(2)      NOT NULL DEFAULT 'ES',
  ADD COLUMN email_contacto       VARCHAR(150) NULL,
  ADD COLUMN email_facturacion    VARCHAR(150) NULL,
  ADD COLUMN telefono             VARCHAR(20)  NULL,
  ADD COLUMN moneda               CHAR(3)      NOT NULL DEFAULT 'EUR',
  ADD COLUMN zona_horaria         VARCHAR(40)  NOT NULL DEFAULT 'Europe/Madrid',
  ADD COLUMN mes_inicio_ejercicio TINYINT      NOT NULL DEFAULT 1,
  -- Por defecto 'pendiente_configuracion': es el estado de las altas que llegan por pago
  ADD COLUMN estado ENUM('pendiente_configuracion','prueba','activa','suspendida','cerrada')
             NOT NULL DEFAULT 'pendiente_configuracion',
  ADD COLUMN creado_en      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ADD COLUMN actualizado_en DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
  ADD UNIQUE KEY uk_iglesias_cif (cif),
  ADD CONSTRAINT ck_iglesias_mes CHECK (mes_inicio_ejercicio BETWEEN 1 AND 12);

-- Las iglesias actuales ya funcionan: se marcan activas.
-- Si alguna tenía activo = 0, pásala después a 'suspendida' a mano.
UPDATE iglesias SET estado = 'activa' WHERE ID > 0;
