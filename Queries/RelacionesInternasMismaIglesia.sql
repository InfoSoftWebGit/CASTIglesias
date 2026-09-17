-- ---------------------------------------------------------------------
-- 8.6 Relaciones internas del módulo: compuestas (misma iglesia).
--     Las FK simples que ya tenías se mantienen; estas añaden la condición de iglesia.
-- ---------------------------------------------------------------------
ALTER TABLE accounting_periods
  ADD CONSTRAINT fkc_accounting_periods_fiscal_year FOREIGN KEY (organization_id, fiscal_year_id) REFERENCES fiscal_years (organization_id, id);

ALTER TABLE approval_decisions
  ADD CONSTRAINT fkc_approval_decisions_request FOREIGN KEY (organization_id, approval_request_id) REFERENCES approval_requests (organization_id, id);

ALTER TABLE approval_requests
  ADD CONSTRAINT fkc_approval_requests_workflow FOREIGN KEY (organization_id, workflow_id) REFERENCES approval_workflows (organization_id, id);

ALTER TABLE approval_rules
  ADD CONSTRAINT fkc_approval_rules_workflow FOREIGN KEY (organization_id, workflow_id) REFERENCES approval_workflows (organization_id, id);

ALTER TABLE bank_statements
  ADD CONSTRAINT fkc_bank_statements_treasury FOREIGN KEY (organization_id, treasury_account_id) REFERENCES treasury_accounts (organization_id, id);

ALTER TABLE bank_statement_lines
  ADD CONSTRAINT fkc_bank_statement_lines_statement FOREIGN KEY (organization_id, bank_statement_id) REFERENCES bank_statements (organization_id, id);

ALTER TABLE budgets
  ADD CONSTRAINT fkc_budgets_fiscal_year FOREIGN KEY (organization_id, fiscal_year_id) REFERENCES fiscal_years (organization_id, id);

ALTER TABLE budget_commitments
  ADD CONSTRAINT fkc_budget_commitments_budget FOREIGN KEY (organization_id, budget_id)      REFERENCES budgets (organization_id, id),
  ADD CONSTRAINT fkc_budget_commitments_line   FOREIGN KEY (organization_id, budget_line_id) REFERENCES budget_lines (organization_id, id);

ALTER TABLE budget_lines
  ADD CONSTRAINT fkc_budget_lines_budget  FOREIGN KEY (organization_id, budget_id)         REFERENCES budgets (organization_id, id),
  ADD CONSTRAINT fkc_budget_lines_fund    FOREIGN KEY (organization_id, fund_id)           REFERENCES funds (organization_id, id),
  ADD CONSTRAINT fkc_budget_lines_project FOREIGN KEY (organization_id, project_id)        REFERENCES projects (organization_id, id),
  ADD CONSTRAINT fkc_budget_lines_account FOREIGN KEY (organization_id, ledger_account_id) REFERENCES ledger_accounts (organization_id, id),
  ADD CONSTRAINT fkc_budget_lines_concept FOREIGN KEY (organization_id, concept_id)        REFERENCES financial_concepts (organization_id, id),
  ADD CONSTRAINT fkc_budget_lines_period  FOREIGN KEY (organization_id, period_id)         REFERENCES accounting_periods (organization_id, id);

ALTER TABLE cash_count_lines
  ADD CONSTRAINT fkc_cash_count_lines_session FOREIGN KEY (organization_id, cash_session_id) REFERENCES cash_sessions (organization_id, id);

ALTER TABLE cash_sessions
  ADD CONSTRAINT fkc_cash_sessions_treasury FOREIGN KEY (organization_id, treasury_account_id) REFERENCES treasury_accounts (organization_id, id);

ALTER TABLE document_links
  ADD CONSTRAINT fkc_document_links_document FOREIGN KEY (organization_id, document_id) REFERENCES documents (organization_id, id);

ALTER TABLE document_sequences
  ADD CONSTRAINT fkc_document_sequences_fiscal_year FOREIGN KEY (organization_id, fiscal_year_id) REFERENCES fiscal_years (organization_id, id);

ALTER TABLE donor_profiles
  ADD CONSTRAINT fkc_donor_profiles_party FOREIGN KEY (organization_id, party_id) REFERENCES parties (organization_id, id);

ALTER TABLE financial_concepts
  ADD CONSTRAINT fkc_financial_concepts_fund            FOREIGN KEY (organization_id, default_fund_id)            REFERENCES funds (organization_id, id),
  ADD CONSTRAINT fkc_financial_concepts_income_account  FOREIGN KEY (organization_id, default_income_account_id)  REFERENCES ledger_accounts (organization_id, id),
  ADD CONSTRAINT fkc_financial_concepts_expense_account FOREIGN KEY (organization_id, default_expense_account_id) REFERENCES ledger_accounts (organization_id, id);

ALTER TABLE financial_transactions
  ADD CONSTRAINT fkc_fin_tx_fiscal_year FOREIGN KEY (organization_id, fiscal_year_id)          REFERENCES fiscal_years (organization_id, id),
  ADD CONSTRAINT fkc_fin_tx_period      FOREIGN KEY (organization_id, accounting_period_id)    REFERENCES accounting_periods (organization_id, id),
  ADD CONSTRAINT fkc_fin_tx_concept     FOREIGN KEY (organization_id, concept_id)              REFERENCES financial_concepts (organization_id, id),
  ADD CONSTRAINT fkc_fin_tx_party       FOREIGN KEY (organization_id, party_id)                REFERENCES parties (organization_id, id),
  ADD CONSTRAINT fkc_fin_tx_fund        FOREIGN KEY (organization_id, fund_id)                 REFERENCES funds (organization_id, id),
  ADD CONSTRAINT fkc_fin_tx_project     FOREIGN KEY (organization_id, project_id)              REFERENCES projects (organization_id, id),
  ADD CONSTRAINT fkc_fin_tx_activity    FOREIGN KEY (organization_id, activity_id)             REFERENCES activities (organization_id, id),
  ADD CONSTRAINT fkc_fin_tx_treasury    FOREIGN KEY (organization_id, treasury_account_id)     REFERENCES treasury_accounts (organization_id, id),
  ADD CONSTRAINT fkc_fin_tx_journal     FOREIGN KEY (organization_id, journal_entry_id)        REFERENCES journal_entries (organization_id, id),
  ADD CONSTRAINT fkc_fin_tx_reversed    FOREIGN KEY (organization_id, reversed_transaction_id) REFERENCES financial_transactions (organization_id, id);

ALTER TABLE financial_transaction_lines
  ADD CONSTRAINT fkc_fin_txl_transaction FOREIGN KEY (organization_id, transaction_id)    REFERENCES financial_transactions (organization_id, id),
  ADD CONSTRAINT fkc_fin_txl_concept     FOREIGN KEY (organization_id, concept_id)        REFERENCES financial_concepts (organization_id, id),
  ADD CONSTRAINT fkc_fin_txl_account     FOREIGN KEY (organization_id, ledger_account_id) REFERENCES ledger_accounts (organization_id, id),
  ADD CONSTRAINT fkc_fin_txl_fund        FOREIGN KEY (organization_id, fund_id)           REFERENCES funds (organization_id, id),
  ADD CONSTRAINT fkc_fin_txl_project     FOREIGN KEY (organization_id, project_id)        REFERENCES projects (organization_id, id),
  ADD CONSTRAINT fkc_fin_txl_activity    FOREIGN KEY (organization_id, activity_id)       REFERENCES activities (organization_id, id);

ALTER TABLE fund_movements
  ADD CONSTRAINT fkc_fund_movements_fund FOREIGN KEY (organization_id, fund_id) REFERENCES funds (organization_id, id);

ALTER TABLE fund_site_links
  ADD CONSTRAINT fkc_fund_site_links_fund FOREIGN KEY (organization_id, fund_id) REFERENCES funds (organization_id, id);

ALTER TABLE journal_entries
  ADD CONSTRAINT fkc_journal_entries_fiscal_year FOREIGN KEY (organization_id, fiscal_year_id)       REFERENCES fiscal_years (organization_id, id),
  ADD CONSTRAINT fkc_journal_entries_period      FOREIGN KEY (organization_id, accounting_period_id) REFERENCES accounting_periods (organization_id, id),
  ADD CONSTRAINT fkc_journal_entries_reversal    FOREIGN KEY (organization_id, reversal_of_entry_id) REFERENCES journal_entries (organization_id, id),
  ADD CONSTRAINT fkc_journal_entries_rule_set    FOREIGN KEY (organization_id, posting_rule_set_id)  REFERENCES posting_rule_sets (organization_id, id),
  ADD CONSTRAINT fkc_journal_entries_rule        FOREIGN KEY (organization_id, posting_rule_id)      REFERENCES posting_rules (organization_id, id);

ALTER TABLE journal_entry_lines
  ADD CONSTRAINT fkc_journal_lines_entry    FOREIGN KEY (organization_id, journal_entry_id)  REFERENCES journal_entries (organization_id, id),
  ADD CONSTRAINT fkc_journal_lines_account  FOREIGN KEY (organization_id, ledger_account_id) REFERENCES ledger_accounts (organization_id, id),
  ADD CONSTRAINT fkc_journal_lines_fund     FOREIGN KEY (organization_id, fund_id)           REFERENCES funds (organization_id, id),
  ADD CONSTRAINT fkc_journal_lines_project  FOREIGN KEY (organization_id, project_id)        REFERENCES projects (organization_id, id),
  ADD CONSTRAINT fkc_journal_lines_activity FOREIGN KEY (organization_id, activity_id)       REFERENCES activities (organization_id, id),
  ADD CONSTRAINT fkc_journal_lines_party    FOREIGN KEY (organization_id, party_id)          REFERENCES parties (organization_id, id);

ALTER TABLE ledger_accounts
  ADD CONSTRAINT fkc_ledger_accounts_parent FOREIGN KEY (organization_id, parent_account_id) REFERENCES ledger_accounts (organization_id, id);

ALTER TABLE payables
  ADD CONSTRAINT fkc_payables_party       FOREIGN KEY (organization_id, party_id)              REFERENCES parties (organization_id, id),
  ADD CONSTRAINT fkc_payables_transaction FOREIGN KEY (organization_id, source_transaction_id) REFERENCES financial_transactions (organization_id, id);

ALTER TABLE payments
  ADD CONSTRAINT fkc_payments_party    FOREIGN KEY (organization_id, party_id)            REFERENCES parties (organization_id, id),
  ADD CONSTRAINT fkc_payments_treasury FOREIGN KEY (organization_id, treasury_account_id) REFERENCES treasury_accounts (organization_id, id),
  ADD CONSTRAINT fkc_payments_journal  FOREIGN KEY (organization_id, journal_entry_id)    REFERENCES journal_entries (organization_id, id);

ALTER TABLE payment_allocations
  ADD CONSTRAINT fkc_payment_alloc_payment FOREIGN KEY (organization_id, payment_id) REFERENCES payments (organization_id, id),
  ADD CONSTRAINT fkc_payment_alloc_payable FOREIGN KEY (organization_id, payable_id) REFERENCES payables (organization_id, id);

ALTER TABLE posting_rules
  ADD CONSTRAINT fkc_posting_rules_rule_set FOREIGN KEY (organization_id, rule_set_id)             REFERENCES posting_rule_sets (organization_id, id),
  ADD CONSTRAINT fkc_posting_rules_concept  FOREIGN KEY (organization_id, concept_id)              REFERENCES financial_concepts (organization_id, id),
  ADD CONSTRAINT fkc_posting_rules_debit    FOREIGN KEY (organization_id, fixed_debit_account_id)  REFERENCES ledger_accounts (organization_id, id),
  ADD CONSTRAINT fkc_posting_rules_credit   FOREIGN KEY (organization_id, fixed_credit_account_id) REFERENCES ledger_accounts (organization_id, id);

ALTER TABLE reconciliation_matches
  ADD CONSTRAINT fkc_reconciliation_line     FOREIGN KEY (organization_id, bank_statement_line_id) REFERENCES bank_statement_lines (organization_id, id),
  ADD CONSTRAINT fkc_reconciliation_movement FOREIGN KEY (organization_id, treasury_movement_id)   REFERENCES treasury_movements (organization_id, id);

ALTER TABLE reversal_requests
  ADD CONSTRAINT fkc_reversal_transaction    FOREIGN KEY (organization_id, source_transaction_id)     REFERENCES financial_transactions (organization_id, id),
  ADD CONSTRAINT fkc_reversal_source_entry   FOREIGN KEY (organization_id, source_journal_entry_id)   REFERENCES journal_entries (organization_id, id),
  ADD CONSTRAINT fkc_reversal_reversal_entry FOREIGN KEY (organization_id, reversal_journal_entry_id) REFERENCES journal_entries (organization_id, id);

ALTER TABLE transfers
  ADD CONSTRAINT fkc_transfers_origin_treasury FOREIGN KEY (organization_id, origin_treasury_account_id)      REFERENCES treasury_accounts (organization_id, id),
  ADD CONSTRAINT fkc_transfers_dest_treasury   FOREIGN KEY (organization_id, destination_treasury_account_id) REFERENCES treasury_accounts (organization_id, id),
  ADD CONSTRAINT fkc_transfers_origin_fund     FOREIGN KEY (organization_id, origin_fund_id)                  REFERENCES funds (organization_id, id),
  ADD CONSTRAINT fkc_transfers_dest_fund       FOREIGN KEY (organization_id, destination_fund_id)             REFERENCES funds (organization_id, id),
  ADD CONSTRAINT fkc_transfers_origin_journal  FOREIGN KEY (organization_id, origin_journal_entry_id)         REFERENCES journal_entries (organization_id, id),
  ADD CONSTRAINT fkc_transfers_dest_journal    FOREIGN KEY (organization_id, destination_journal_entry_id)    REFERENCES journal_entries (organization_id, id);

ALTER TABLE treasury_accounts
  ADD CONSTRAINT fkc_treasury_accounts_ledger FOREIGN KEY (organization_id, ledger_account_id) REFERENCES ledger_accounts (organization_id, id);

ALTER TABLE treasury_movements
  ADD CONSTRAINT fkc_treasury_movements_account FOREIGN KEY (organization_id, treasury_account_id) REFERENCES treasury_accounts (organization_id, id);