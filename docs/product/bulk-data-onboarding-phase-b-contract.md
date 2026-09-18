# Bulk Data Onboarding — Phase B Contract

## Status

**REAL FACTORY VALIDATION: PENDING DATA**

Phase B starts only when a real customer factory database or export is available.

## Phase B Sequence

1. SOURCE INVENTORY
2. SOURCE ADAPTER SELECTION / IMPLEMENTATION
3. SOURCE-SPECIFIC MAPPING
4. REAL DRY RUN
5. DATA QUALITY REVIEW
6. QUARANTINE REVIEW
7. CONTROLLED REAL IMPORT
8. RECONCILIATION
9. REAL FACTORY SANITY CHECK
10. MP2 REAL DATA VALIDATION
11. CONTEXT REAL DATA VALIDATION
12. FACTORY INTELLIGENCE VALIDATION
13. DIGITAL WORKFORCE OBSERVE / DRAFT VALIDATION

## Foundation Delivered in Phase A

- `BulkDataImportJob` persisted lifecycle
- Synthetic structured source provider
- Source catalog + schema profiler
- `FactoryDataMappingProfile`
- Staging, quarantine, idempotency, dry-run, impact preview
- Controlled commit into canonical MP1 domains
- Lineage, reconciliation, rollback foundation
- Checkpoint / resume foundation
- Delta / watermark JSON foundation
- Veri Aktarım Merkezi UI shell
- Tenant / facility scoped permissions and RLS

## Explicit Non-Goals in Phase A

- No fake customer source adapter
- No fabricated customer mapping
- No claim of real factory validation PASS
