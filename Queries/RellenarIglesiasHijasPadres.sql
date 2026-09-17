-- ---------------------------------------------------------------------
-- 8.2 Rellenar la iglesia de las hijas desde su tabla padre
--     (si están vacías no hace nada; si tienen datos de prueba, los deja coherentes)
-- ---------------------------------------------------------------------
UPDATE accounting_periods c          JOIN fiscal_years p           ON p.id = c.fiscal_year_id      SET c.organization_id = p.organization_id;
UPDATE approval_decisions c          JOIN approval_requests p      ON p.id = c.approval_request_id SET c.organization_id = p.organization_id;
UPDATE approval_rules c              JOIN approval_workflows p     ON p.id = c.workflow_id         SET c.organization_id = p.organization_id;
UPDATE bank_statement_lines c        JOIN bank_statements p        ON p.id = c.bank_statement_id   SET c.organization_id = p.organization_id;
UPDATE budget_lines c                JOIN budgets p                ON p.id = c.budget_id           SET c.organization_id = p.organization_id;
UPDATE cash_count_lines c            JOIN cash_sessions p          ON p.id = c.cash_session_id     SET c.organization_id = p.organization_id;
UPDATE document_links c              JOIN documents p              ON p.id = c.document_id         SET c.organization_id = p.organization_id;
UPDATE donor_profiles c              JOIN parties p                ON p.id = c.party_id            SET c.organization_id = p.organization_id;
UPDATE financial_transaction_lines c JOIN financial_transactions p ON p.id = c.transaction_id      SET c.organization_id = p.organization_id;
UPDATE journal_entry_lines c         JOIN journal_entries p        ON p.id = c.journal_entry_id    SET c.organization_id = p.organization_id;
UPDATE posting_rules c               JOIN posting_rule_sets p      ON p.id = c.rule_set_id         SET c.organization_id = p.organization_id;
UPDATE fund_site_links c             JOIN funds p                  ON p.id = c.fund_id             SET c.organization_id = p.organization_id;
UPDATE payment_allocations c         JOIN payments p               ON p.id = c.payment_id          SET c.organization_id = p.organization_id;