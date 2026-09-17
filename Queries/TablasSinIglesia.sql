-- 9.1 Tablas SIN iglesia (ni ID_iglesia ni organization_id).
--     Solo deben salir: catálogos (ccaa, provincias, municipios, paises), iglesias,
--     licencias, planes, eventos_pasarela, accounting_templates,
--     accounting_template_accounts, permissions y role_permissions.
SELECT t.table_name
FROM information_schema.tables t
WHERE t.table_schema = DATABASE() AND t.table_type = 'BASE TABLE'
  AND NOT EXISTS (SELECT 1 FROM information_schema.columns c
                  WHERE c.table_schema = t.table_schema AND c.table_name = t.table_name
                    AND LOWER(c.column_name) IN ('id_iglesia','organization_id'))
ORDER BY t.table_name;