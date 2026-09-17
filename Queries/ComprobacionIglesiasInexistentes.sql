-- ---------------------------------------------------------------------
-- 8.3 Comprobación antes de las FK: ninguna fila con una iglesia inexistente.
--     Debe salir VACÍA. Si sale algo, son datos de prueba: corrígelos o bórralos.
-- ---------------------------------------------------------------------
SELECT 'activities' t, COUNT(*) n FROM activities WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'approval_requests', COUNT(*) FROM approval_requests WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'approval_workflows', COUNT(*) FROM approval_workflows WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'audit_events', COUNT(*) FROM audit_events WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'bank_statements', COUNT(*) FROM bank_statements WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'budgets', COUNT(*) FROM budgets WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'cash_sessions', COUNT(*) FROM cash_sessions WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'documents', COUNT(*) FROM documents WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'financial_concepts', COUNT(*) FROM financial_concepts WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'financial_transactions', COUNT(*) FROM financial_transactions WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'fiscal_years', COUNT(*) FROM fiscal_years WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'funds', COUNT(*) FROM funds WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'journal_entries', COUNT(*) FROM journal_entries WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'ledger_accounts', COUNT(*) FROM ledger_accounts WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'parties', COUNT(*) FROM parties WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'payments', COUNT(*) FROM payments WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'transfers', COUNT(*) FROM transfers WHERE organization_id NOT IN (SELECT ID FROM iglesias) UNION ALL
SELECT 'treasury_accounts', COUNT(*) FROM treasury_accounts WHERE organization_id NOT IN (SELECT ID FROM iglesias);