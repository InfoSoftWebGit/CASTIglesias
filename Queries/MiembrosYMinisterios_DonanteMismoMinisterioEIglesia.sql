-- ---------------------------------------------------------------------
-- 8.8 Miembros y ministerios (compuestas: el donante y el ministerio son de la misma iglesia)
-- ---------------------------------------------------------------------
ALTER TABLE parties                     ADD CONSTRAINT fkm_parties_member        FOREIGN KEY (organization_id, member_id)   REFERENCES miembros (ID_iglesia, ID_miembro);
ALTER TABLE budget_lines                ADD CONSTRAINT fkm_budget_lines_ministry FOREIGN KEY (organization_id, ministry_id) REFERENCES ministerio (ID_iglesia, ID);
ALTER TABLE financial_transactions      ADD CONSTRAINT fkm_fin_tx_ministry       FOREIGN KEY (organization_id, ministry_id) REFERENCES ministerio (ID_iglesia, ID);
ALTER TABLE financial_transaction_lines ADD CONSTRAINT fkm_fin_txl_ministry      FOREIGN KEY (organization_id, ministry_id) REFERENCES ministerio (ID_iglesia, ID);
ALTER TABLE journal_entry_lines         ADD CONSTRAINT fkm_journal_lines_ministry FOREIGN KEY (organization_id, ministry_id) REFERENCES ministerio (ID_iglesia, ID);