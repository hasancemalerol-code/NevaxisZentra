# AI Decision Ledger Foundation

The AI Decision Ledger is an immutable, tenant-scoped record of bounded recommendation evidence. It is separate from the audit trail: the ledger explains what recommendation was recorded and which references supported it, while audit records prove that the append action occurred and its outcome. A successful ledger append and its single `ai.decision.recorded` audit record commit in one PostgreSQL transaction and share a correlation ID.

## Allowed data

Records contain stable decision, capability, recommendation, model-provenance and failure codes; a bounded recommendation summary; optional descriptive confidence from 0 through 1; exact risk and status values; immutable evidence references; bounded structured alternatives and policy facts; correlation, causation, expiry, audit and superseding identifiers; and UTC timestamps. Evidence is a reference contract, not a raw-content container.

Prompts, completions, chain-of-thought, hidden reasoning, unrestricted raw input, model internals, credentials, tokens, full documents, conversations, telemetry dumps, embeddings, binary attachments and arbitrary nested JSON are prohibited. Typed request contracts reject unknown fields and the database bounds all JSON values.

## Risk, review and status

`Low`, `Medium`, `High` and `Critical` are descriptive risk classifications and never authorize execution. `High` and `Critical` always require human review. Confidence is optional descriptive evidence and is never an execution authority. The infrastructure statuses are:

- `Proposed`: a recommendation is available for consideration.
- `ReviewRequired`: a human must review the recommendation.
- `Accepted`: diagnostic-only in this foundation and must be human-attributed.
- `Rejected`: a recommendation was not accepted.
- `Expired`: its evidence window elapsed.
- `Superseded`: a newer immutable record replaces it.
- `Failed`: recommendation recording failed with a safe code.

Rows never transition in place. A changed status or recommendation is a new record whose optional `supersedesDecisionId` points to an earlier record in the same tenant.

## Access boundary

The runtime role can select through forced tenant RLS and append only through the typed `append_ai_decision_record` function. It cannot directly insert, update, delete or truncate ledger rows. Development exposes only permission-protected `POST /internal/ai-decisions` and `GET /internal/ai-decisions/recent`; reads return at most 25 safe summaries. The exact permissions are `system.ai-decision.write` and `system.ai-decision.read`, with no default role assignment.

This foundation includes no LLM client, prompt builder, AI agent runtime, autonomous execution, Human-in-the-Loop approval workflow, product workflow, outbox, messaging, connector, ERP write-back, Teams behavior or dashboard implementation.
