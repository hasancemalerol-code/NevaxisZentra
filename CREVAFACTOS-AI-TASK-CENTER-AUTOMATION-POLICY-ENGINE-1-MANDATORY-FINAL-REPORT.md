# CREVAFACTOS-AI-TASK-CENTER-AUTOMATION-POLICY-ENGINE-1 — Mandatory Final Report

## STATUS

**READY_FOR_VISUAL_REVIEW**

**VISUAL STATUS = PENDING USER APPROVAL**

---

## Starting HEAD

`51590d009565bc47a925ba7fc0e7e5ed4ab8cf21` (working tree base; current HEAD `5d3199ae5a7d5831288d37b526e404d7b28a50b6` includes prior closure commit)

---

## Scope Delivered

Production-grade operational automation engine for canonical AI Görev Merkezi:

- Rule model with versioning and audit revisions
- Deterministic evaluator with conflict resolution
- Safe simulation (no mutation)
- Automatic assignment, SLA, evidence, closure approval integration
- Manual override persistence
- Execution audit trail with idempotency
- Management UI at `/ai-gorevleri/otomasyon`
- Live flow verifier with screenshots

No second task engine created — extends existing AI task infrastructure.

---

## Rule Model

Tables:

| Table | Purpose |
|-------|---------|
| `ai_task_automation_rules` | Active rule definitions |
| `ai_task_automation_rule_revisions` | Version history on edit/enable/disable |
| `ai_task_automation_executions` | Immutable execution audit |

Task extensions on `ai_tasks`:

- `closure_policy_id`, `minimum_evidence_required`, `four_eyes_required`
- `automation_rule_id`, `automation_rule_version`
- `automation_override_json`, `automation_metadata_json`, `automation_tags_json`

---

## Triggers

`task-created`, `task-source-linked`, `task-priority-changed`, `task-escalated`, `task-reopened`, `task-closure-requested`

Integrated on create via `ApplyRoutingOnCreateAsync` → automation first, legacy routing fallback.

---

## Conditions

Structured AND conditions: `sourceModule`, `taskCategory`, `priority`, `severity`, `escalationLevel`, `currentAssignmentState`, etc.

Operators: `equals`, `not-equals`, `in`, `not-in`, `gte`, `lte`

---

## Actions

`assignToTeam`, `assignToMembership`, `applySlaPolicy`, `requireEvidence`, `requireClosureApproval`, `enableFourEyes`, `applyClosurePolicy`, `setPriority`, `notifyTeamsChannel`, `addTaskTag`, etc.

---

## Evaluation Engine

`AiTaskAutomationEngine` — stable ordering by `sortOrder` + `ruleKey`, tenant/facility scoped, explainable result with matched/skipped/conflicts/proposed changes.

Idempotency key: `automation:{taskId}:{trigger}:{ruleId}:{ruleVersion}:{taskVersion}`

---

## Conflict Resolution

Same-field conflicts resolved by lower `sortOrder` (higher priority). Conflicts exposed in evaluation result and execution audit.

---

## Manual Override

On manual/reassignment assign: `automation_override_json` persisted with scope `assignment`, actor, reason, `AllowAutomationResume: false`.

Automation skips overridden scopes on re-evaluation.

---

## Simulation

`POST /api/ai-tasks/automation/simulate` — read-only evaluation. Disabled rules simulatable when `ruleId` specified.

Live flow verified: version unchanged, no `automationRuleId` linked after simulate.

---

## Policy Integration

Reuses existing:

- `ai_task_sla_policies` via `applySlaPolicy` action
- `ai_task_closure_policies` via `applyClosurePolicy` action
- `AiTaskClosureEngine` respects task-level overrides (`MinimumEvidenceRequired`, `ClosureApprovalRequired`, pinned `ClosurePolicyId`)

---

## Assignment

`assignToTeam` / `assignToMembership` actions with tenant/facility validation. Assignment source `automation`. Legacy `AiTaskRoutingService` fallback when automation does not assign.

Live flow: shift-handover + critical → Bakım team channel `bb000001-03d3-4000-8000-0000000003d3`

---

## SLA

`applySlaPolicy` with policy key `critical-default` applied on matching tasks.

---

## Evidence / Closure Governance

Actions set `minimum_evidence_required`, `closure_approval_required`, `four_eyes_required`. Closure engine merges with DB policies.

Live flow: evidence ≥ 1, closure approval required = PASS

---

## Teams

`notifyTeamsChannel` action with dedupe via audit fingerprint (idempotent notification guard).

---

## Security / RLS

Permissions:

- `ai-task.automation.read`
- `ai-task.automation.manage`
- `ai-task.automation.simulate`

RLS + FORCE RLS on all automation tables. Tenant/facility isolation enforced. Cross-facility assignment validation.

---

## Database / Migration

Applied via `.cursor-screenshots/automation-migration.sql` to `crevafactos_local` (Docker `crevafactos-local-postgres-1`).

EF entity configurations with snake_case column mappings.

---

## UI

Route: `/ai-gorevleri/otomasyon`

Sections:

- **A. Otomasyon Kuralları** — list + actions
- **B. Politikalar** — SLA/closure policy library
- **C. Simülasyon** — task + rule preview
- **D. Çalışma Geçmişi** — execution history

Rule builder: WHEN / IF / THEN structured controls.

Nav link added in AI Görev Merkezi list header.

---

## Live Flow

Verifier: `apps/web/scripts/verify-ai-task-automation-1-live-flow.mjs` — **PASSED**

Flow A: create rule → simulate (no mutation) → enable → matching task automated → audit → idempotency → manual override → disable → post-disable no automation

---

## Verifiers

| Check | Result |
|-------|--------|
| Rule persistence | PASS |
| Rule versioning | PASS |
| Evaluator determinism | PASS |
| Conflict resolution | PASS |
| Simulation no-mutation | PASS |
| Automatic assignment | PASS |
| SLA integration | PASS |
| Evidence/closure integration | PASS |
| Manual override | PASS |
| Idempotency | PASS |
| Tenant/facility isolation | PASS (validation + RLS) |
| Permissions | PASS |
| Shift Handover integration | PASS (`sourceModule=shift-handover`) |
| Dashboard freeze | PASS (0 protected-path diffs) |

---

## Screenshots

- `.cursor-screenshots/ai-task-automation-1-rules.png`
- `.cursor-screenshots/ai-task-automation-1-builder.png`
- `.cursor-screenshots/ai-task-automation-1-simulation.png`
- `.cursor-screenshots/ai-task-automation-1-execution-history.png`

---

## Dashboard Absolute Freeze

Compare base: `51590d009565bc47a925ba7fc0e7e5ed4ab8cf21`

**protected dashboard paths changed = 0**

---

## Git

```
git diff --check   → CRLF warnings only
git status --short → modified + untracked automation package files
staged             = 0
commit             = NO
```

---

## FINAL VERDICT

**READY_FOR_VISUAL_REVIEW**

**VISUAL STATUS = PENDING USER APPROVAL**

Do not commit.
