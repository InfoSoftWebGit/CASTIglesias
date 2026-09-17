-- ---------------------------------------------------------------------
-- 8.5 UNIQUE (iglesia, id) en las tablas a las que apuntan FK compuestas.
--     Es lo que permite exigir "misma iglesia" en las relaciones.
-- ---------------------------------------------------------------------
ALTER TABLE accounting_periods     ADD UNIQUE KEY uk_accounting_periods_org_id     (organization_id, id);
ALTER TABLE activities             ADD UNIQUE KEY uk_activities_org_id             (organization_id, id);
ALTER TABLE approval_requests      ADD UNIQUE KEY uk_approval_requests_org_id      (organization_id, id);
ALTER TABLE approval_workflows     ADD UNIQUE KEY uk_approval_workflows_org_id     (organization_id, id);
ALTER TABLE bank_statements        ADD UNIQUE KEY uk_bank_statements_org_id        (organization_id, id);
ALTER TABLE bank_statement_lines   ADD UNIQUE KEY uk_bank_statement_lines_org_id   (organization_id, id);
ALTER TABLE budgets                ADD UNIQUE KEY uk_budgets_org_id                (organization_id, id);
ALTER TABLE budget_lines           ADD UNIQUE KEY uk_budget_lines_org_id           (organization_id, id);
ALTER TABLE cash_sessions          ADD UNIQUE KEY uk_cash_sessions_org_id          (organization_id, id);
ALTER TABLE documents              ADD UNIQUE KEY uk_documents_org_id              (organization_id, id);
ALTER TABLE financial_concepts     ADD UNIQUE KEY uk_financial_concepts_org_id     (organization_id, id);
ALTER TABLE financial_transactions ADD UNIQUE KEY uk_financial_transactions_org_id (organization_id, id);
ALTER TABLE fiscal_years           ADD UNIQUE KEY uk_fiscal_years_org_id           (organization_id, id);
ALTER TABLE funds                  ADD UNIQUE KEY uk_funds_org_id                  (organization_id, id);
ALTER TABLE journal_entries        ADD UNIQUE KEY uk_journal_entries_org_id        (organization_id, id);
ALTER TABLE ledger_accounts        ADD UNIQUE KEY uk_ledger_accounts_org_id        (organization_id, id);
ALTER TABLE parties                ADD UNIQUE KEY uk_parties_org_id                (organization_id, id);
ALTER TABLE payables               ADD UNIQUE KEY uk_payables_org_id               (organization_id, id);
ALTER TABLE payments               ADD UNIQUE KEY uk_payments_org_id               (organization_id, id);
ALTER TABLE posting_rule_sets      ADD UNIQUE KEY uk_posting_rule_sets_org_id      (organization_id, id);
ALTER TABLE posting_rules          ADD UNIQUE KEY uk_posting_rules_org_id          (organization_id, id);
ALTER TABLE projects               ADD UNIQUE KEY uk_projects_org_id               (organization_id, id);
ALTER TABLE treasury_accounts      ADD UNIQUE KEY uk_treasury_accounts_org_id      (organization_id, id);
ALTER TABLE treasury_movements     ADD UNIQUE KEY uk_treasury_movements_org_id     (organization_id, id);
-- Tablas pastorales a las que apunta finanzas
ALTER TABLE miembros   ADD UNIQUE KEY uk_miembros_iglesia_id   (ID_iglesia, ID_miembro);
ALTER TABLE ministerio ADD UNIQUE KEY uk_ministerio_iglesia_id (ID_iglesia, ID);