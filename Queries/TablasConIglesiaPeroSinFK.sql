-- 9.2 Tablas CON iglesia pero SIN FK a iglesias (debe salir VACÍA)
SELECT c.table_name, c.column_name
FROM information_schema.columns c
JOIN information_schema.tables t
  ON t.table_schema = c.table_schema AND t.table_name = c.table_name AND t.table_type = 'BASE TABLE'
WHERE c.table_schema = DATABASE()
  AND LOWER(c.column_name) IN ('id_iglesia','organization_id')
  AND NOT EXISTS (SELECT 1 FROM information_schema.key_column_usage k
                  WHERE k.table_schema = c.table_schema AND k.table_name = c.table_name
                    AND k.column_name = c.column_name AND k.referenced_table_name = 'iglesias')
ORDER BY c.table_name;