-- ---------------------------------------------------------------------
-- 8.7 Sedes (compuestas) + prohibido usar la 1000 en finanzas
--     (en finanzas, "toda la iglesia" es NULL)
-- ---------------------------------------------------------------------
ALTER TABLE activities                  ADD CONSTRAINT fks_activities_site         FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_activities_site         CHECK (site_id IS NULL OR site_id <> 1000);
ALTER TABLE audit_events                ADD CONSTRAINT fks_audit_events_site       FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_audit_events_site       CHECK (site_id IS NULL OR site_id <> 1000);
ALTER TABLE budget_lines                ADD CONSTRAINT fks_budget_lines_site       FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_budget_lines_site       CHECK (site_id IS NULL OR site_id <> 1000);
ALTER TABLE cash_sessions               ADD CONSTRAINT fks_cash_sessions_site      FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_cash_sessions_site      CHECK (site_id <> 1000);
ALTER TABLE documents                   ADD CONSTRAINT fks_documents_site          FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_documents_site          CHECK (site_id IS NULL OR site_id <> 1000);
ALTER TABLE document_sequences          ADD CONSTRAINT fks_document_sequences_site FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_document_sequences_site CHECK (site_id IS NULL OR site_id <> 1000);
ALTER TABLE financial_transactions      ADD CONSTRAINT fks_fin_tx_site             FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_fin_tx_site             CHECK (site_id <> 1000);
ALTER TABLE financial_transaction_lines ADD CONSTRAINT fks_fin_txl_site            FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_fin_txl_site            CHECK (site_id <> 1000);
ALTER TABLE fund_movements              ADD CONSTRAINT fks_fund_movements_site     FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_fund_movements_site     CHECK (site_id <> 1000);
ALTER TABLE fund_site_links             ADD CONSTRAINT fks_fund_site_links_site    FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_fund_site_links_site    CHECK (site_id <> 1000);
ALTER TABLE funds                       ADD CONSTRAINT fks_funds_owner_site        FOREIGN KEY (organization_id, owner_site_id) REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_funds_owner_site        CHECK (owner_site_id IS NULL OR owner_site_id <> 1000);
ALTER TABLE journal_entry_lines         ADD CONSTRAINT fks_journal_lines_site      FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_journal_lines_site      CHECK (site_id IS NULL OR site_id <> 1000);
ALTER TABLE payables                    ADD CONSTRAINT fks_payables_site           FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_payables_site           CHECK (site_id <> 1000);
ALTER TABLE payments                    ADD CONSTRAINT fks_payments_site           FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_payments_site           CHECK (site_id <> 1000);
ALTER TABLE posting_rules               ADD CONSTRAINT fks_posting_rules_site      FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_posting_rules_site      CHECK (site_id IS NULL OR site_id <> 1000);
ALTER TABLE projects                    ADD CONSTRAINT fks_projects_site           FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_projects_site           CHECK (site_id IS NULL OR site_id <> 1000);
ALTER TABLE treasury_accounts           ADD CONSTRAINT fks_treasury_accounts_site  FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_treasury_accounts_site  CHECK (site_id IS NULL OR site_id <> 1000);
ALTER TABLE treasury_movements          ADD CONSTRAINT fks_treasury_movements_site FOREIGN KEY (organization_id, site_id)       REFERENCES sedes (ID_iglesia, ID), ADD CONSTRAINT ck_treasury_movements_site CHECK (site_id <> 1000);
ALTER TABLE transfers
  ADD CONSTRAINT fks_transfers_origin_site FOREIGN KEY (organization_id, origin_site_id)      REFERENCES sedes (ID_iglesia, ID),
  ADD CONSTRAINT fks_transfers_dest_site   FOREIGN KEY (organization_id, destination_site_id) REFERENCES sedes (ID_iglesia, ID),
  ADD CONSTRAINT ck_transfers_sites        CHECK (origin_site_id <> 1000 AND destination_site_id <> 1000);