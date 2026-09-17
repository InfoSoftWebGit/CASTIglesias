-- ---------------------------------------------------------------------
-- 8.9 Usuarios
--   Compuestas (usuario de la misma iglesia): responsables y asignaciones de rol.
--   Simples (cualquier usuario): quién creó, aprobó, cerró... porque el
--   administrador de plataforma pertenece a otra iglesia y también actúa.
-- ---------------------------------------------------------------------
ALTER TABLE accounting_periods  ADD CONSTRAINT fku_accounting_periods_closed_by FOREIGN KEY (closed_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE activities
  ADD CONSTRAINT fku_activities_responsible FOREIGN KEY (organization_id, responsible_user_id) REFERENCES usuarios (ID_iglesia, ID_usuario),
  ADD CONSTRAINT fku_activities_created_by  FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_activities_updated_by  FOREIGN KEY (updated_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE approval_decisions  ADD CONSTRAINT fku_approval_decisions_approver FOREIGN KEY (approver_user_id) REFERENCES usuarios (ID_usuario);
ALTER TABLE approval_requests   ADD CONSTRAINT fku_approval_requests_requested FOREIGN KEY (requested_by)     REFERENCES usuarios (ID_usuario);
ALTER TABLE audit_events        ADD CONSTRAINT fku_audit_events_actor          FOREIGN KEY (actor_user_id)    REFERENCES usuarios (ID_usuario);
ALTER TABLE bank_statements     ADD CONSTRAINT fku_bank_statements_created_by  FOREIGN KEY (created_by)       REFERENCES usuarios (ID_usuario);

ALTER TABLE budgets
  ADD CONSTRAINT fku_budgets_created_by  FOREIGN KEY (created_by)  REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_budgets_approved_by FOREIGN KEY (approved_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE cash_sessions
  ADD CONSTRAINT fku_cash_sessions_opened_by FOREIGN KEY (opened_by)           REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_cash_sessions_closed_by FOREIGN KEY (closed_by)           REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_cash_sessions_validator FOREIGN KEY (second_validator_id) REFERENCES usuarios (ID_usuario);

ALTER TABLE documents      ADD CONSTRAINT fku_documents_uploaded_by    FOREIGN KEY (uploaded_by)             REFERENCES usuarios (ID_usuario);
ALTER TABLE donor_profiles ADD CONSTRAINT fku_donor_profiles_verified  FOREIGN KEY (fiscal_data_verified_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE financial_concepts
  ADD CONSTRAINT fku_financial_concepts_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_financial_concepts_updated_by FOREIGN KEY (updated_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE financial_transactions
  ADD CONSTRAINT fku_fin_tx_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_fin_tx_updated_by FOREIGN KEY (updated_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE fiscal_years ADD CONSTRAINT fku_fiscal_years_closed_by FOREIGN KEY (closed_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE funds
  ADD CONSTRAINT fku_funds_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_funds_updated_by FOREIGN KEY (updated_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE fund_movements  ADD CONSTRAINT fku_fund_movements_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario);
ALTER TABLE journal_entries ADD CONSTRAINT fku_journal_entries_posted_by FOREIGN KEY (posted_by)  REFERENCES usuarios (ID_usuario);

ALTER TABLE ledger_accounts
  ADD CONSTRAINT fku_ledger_accounts_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_ledger_accounts_updated_by FOREIGN KEY (updated_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE parties
  ADD CONSTRAINT fku_parties_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_parties_updated_by FOREIGN KEY (updated_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE payables ADD CONSTRAINT fku_payables_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario);
ALTER TABLE payments ADD CONSTRAINT fku_payments_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE posting_rule_sets
  ADD CONSTRAINT fku_posting_rule_sets_created_by  FOREIGN KEY (created_by)  REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_posting_rule_sets_approved_by FOREIGN KEY (approved_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE projects
  ADD CONSTRAINT fku_projects_responsible FOREIGN KEY (organization_id, responsible_user_id) REFERENCES usuarios (ID_iglesia, ID_usuario),
  ADD CONSTRAINT fku_projects_created_by  FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_projects_updated_by  FOREIGN KEY (updated_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE reconciliation_matches
  ADD CONSTRAINT fku_reconciliation_created_by  FOREIGN KEY (created_by)  REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_reconciliation_approved_by FOREIGN KEY (approved_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE reversal_requests
  ADD CONSTRAINT fku_reversal_requested_by FOREIGN KEY (requested_by) REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_reversal_approved_by  FOREIGN KEY (approved_by)  REFERENCES usuarios (ID_usuario);

ALTER TABLE transfers ADD CONSTRAINT fku_transfers_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE treasury_accounts
  ADD CONSTRAINT fku_treasury_accounts_responsible FOREIGN KEY (organization_id, responsible_user_id) REFERENCES usuarios (ID_iglesia, ID_usuario),
  ADD CONSTRAINT fku_treasury_accounts_created_by  FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario),
  ADD CONSTRAINT fku_treasury_accounts_updated_by  FOREIGN KEY (updated_by) REFERENCES usuarios (ID_usuario);

ALTER TABLE treasury_movements ADD CONSTRAINT fku_treasury_movements_created_by FOREIGN KEY (created_by) REFERENCES usuarios (ID_usuario);

-- Un rol financiero se asigna a un usuario de la misma iglesia
ALTER TABLE user_roles
  ADD CONSTRAINT fku_user_roles_user FOREIGN KEY (organization_id, user_id) REFERENCES usuarios (ID_iglesia, ID_usuario);