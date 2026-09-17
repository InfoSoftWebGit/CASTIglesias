-- 9.3 Resumen de relaciones por tabla
SELECT table_name, COUNT(*) AS num_fk
FROM information_schema.referential_constraints
WHERE constraint_schema = DATABASE()
GROUP BY table_name
ORDER BY table_name;