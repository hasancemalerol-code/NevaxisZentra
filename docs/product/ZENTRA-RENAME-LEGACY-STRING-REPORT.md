# Zentra Rename — Legacy String Report (Pre-Commit)

## ACTIVE USER-VISIBLE FACTOS: 0

Web/desktop/login/shell branding verified → **Zentra** / **Nevaxis Zentra**.

## COMPATIBILITY (allowed)

| Pattern | Location | Reason |
|---------|----------|--------|
| `NEVAXIS_FACTOS_*` | `LegacyEnvironmentVariables.cs`, compose fallbacks | Legacy env alias |
| `NevaxisFactOS.session` | `saas-identity-constants.ts` | Cookie upgrade read path |
| `crevafactos` enum keys | Target-system labels | Customer data contract |
| `crevafactos_local` | Existing connection strings / docker fallback | Existing DB compatibility |
| `Software\NevaxisFactOS` | WiX registry | MSI upgrade detection |
| `NevaxisFactOS-local-postgres-data` | `compose.yaml` volume names | Preserve local Docker data |

## HISTORICAL MIGRATION (allowed)

All `202608*`–`202609*` migration IDs and filenames unchanged. Namespaces updated to `NevaxisZentra.Persistence.Migrations`.

## IMMUTABLE EVIDENCE (allowed)

Root `CREVAFACTOS-*` / `NEVAXISFACTOS-*` closure reports unchanged.

## GOLDEN DEMO FIXTURE KEYS (allowed)

English dictionary keys in `golden-demo-presentation-copy.ts` retain `FactOS` prefix for frozen API lookup; Turkish display values show **Zentra**.
