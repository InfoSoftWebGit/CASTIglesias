-- =====================================================================
-- PASO 8 · RELACIONAR EL MÓDULO FINANCIERO
-- =====================================================================
SET SQL_SAFE_UPDATES = 0;

-- ---------------------------------------------------------------------
-- 8.1 organization_id en las tablas hijas que no lo tienen.
--     Toda tabla con datos de una iglesia lleva su iglesia, también las líneas,
--     para que el filtro global de EF Core las proteja igual que a las cabeceras.
-- ---------------------------------------------------------------------
ALTER TABLE accounting_periods          ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE approval_decisions          ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE approval_rules              ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE bank_statement_lines        ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE budget_lines                ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE cash_count_lines            ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE document_links              ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE donor_profiles              ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE financial_transaction_lines ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE journal_entry_lines         ADD COLUMN organization_id INT NOT NULL AFTER id;
ALTER TABLE posting_rules               ADD COLUMN organization_id INT NOT NULL AFTER id;
-- Estas dos no tienen columna id (su clave es compuesta)
ALTER TABLE fund_site_links             ADD COLUMN organization_id INT NOT NULL FIRST;
ALTER TABLE payment_allocations         ADD COLUMN organization_id INT NOT NULL FIRST;
-- roles: NULL = rol del sistema (común a todas las iglesias); con valor = rol propio de una iglesia
ALTER TABLE roles ADD COLUMN organization_id INT NULL COMMENT 'NULL = rol del sistema' AFTER id;