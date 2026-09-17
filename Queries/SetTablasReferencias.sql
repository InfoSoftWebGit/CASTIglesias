-- 0.1 Tipos de las columnas que se van a relacionar. Todas deben ser del MISMO tipo
--     (p. ej. int). Si alguna es "int unsigned" o "bigint", las FK fallarán.
SELECT table_name, column_name, column_type, is_nullable
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND (LOWER(column_name) IN ('id_iglesia','id_sede','id_usuario','id_miembro','correo_electronico')
       OR (table_name IN ('iglesias','sedes') AND column_name = 'ID'))
ORDER BY column_name, table_name;

-- 0.2 La fila marcador 1000 y las sedes reales
SELECT * FROM sedes ORDER BY ID;
SELECT * FROM iglesias;

-- 0.3 Correos repetidos (impiden el UNIQUE del paso 2)
SELECT correo_electronico, COUNT(*) FROM usuarios GROUP BY correo_electronico HAVING COUNT(*) > 1;

-- 0.4 Usuarios cuya iglesia no coincide con la de su sede (el fallo del DEFAULT 1)
SELECT u.ID_usuario, u.correo_electronico, u.ID_iglesia AS iglesia_usuario, s.ID AS sede, s.ID_iglesia AS iglesia_sede
FROM usuarios u JOIN sedes s ON s.ID = u.ID_sede
WHERE u.ID_sede <> 1000 AND u.ID_iglesia <> s.ID_iglesia;
-- Corrígelos antes de seguir; normalmente la buena es la de la sede:
-- UPDATE usuarios u JOIN sedes s ON s.ID = u.ID_sede SET u.ID_iglesia = s.ID_iglesia
-- WHERE u.ID_sede <> 1000 AND u.ID_iglesia <> s.ID_iglesia;

-- 0.5 Tablas financieras que todavía no tienen ID_iglesia
SELECT t.table_name
FROM information_schema.tables t
WHERE t.table_schema = DATABASE() AND t.table_type = 'BASE TABLE'
  AND NOT FIND_IN_SET(LOWER(t.table_name), @no_financieras)
  AND NOT EXISTS (SELECT 1 FROM information_schema.columns c
                  WHERE c.table_schema = t.table_schema AND c.table_name = t.table_name
                    AND LOWER(c.column_name) = 'id_iglesia')
ORDER BY t.table_name;
