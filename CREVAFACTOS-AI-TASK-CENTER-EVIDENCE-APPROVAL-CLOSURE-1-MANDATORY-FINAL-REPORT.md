## STATUS

Enterprise closure governance delivered on the canonical AI Görev Merkezi infrastructure: evidence metadata, resolution, closure validation, four-eyes approval, rejection/reopen, policy engine, RLS-backed persistence, API endpoints, Teams notifications, premium detail/card UI, authoritative closure queue filters, and authenticated live flow A/B/C.

## Starting HEAD
038f8ee

## Scope Delivered

- **Closure governance model** — separate `closure_state` metadata on `ai_tasks` (does not break existing lifecycle `status` values): `none`, `awaiting-closure-approval`, `closure-rejected`, `closed`, `reopened`
- **Evidence** — `ai_task_evidence` table + immutable audit events; metadata/reference foundation (no fake blob uploads)
- **Policies** — `ai_task_closure_policies` with seeded dev policies (critical maintenance, quality NCR, high priority)
- **Engine** — `AiTaskClosureEngine` centralizes policy resolution + closure validation
- **Repository** — `AiTaskClosureRepository` for evidence, resolution, closure-request, approve/reject, reopen
- **API** — `/api/ai-tasks/{taskId}/evidence`, `/resolution`, `/closure-request`, `/approve-closure`, `/reject-closure`, `/reopen`, `/closure-validation`
- **Permissions** — `ai-task.evidence.manage`, `ai-task.closure.request`, `ai-task.closure.approve`, `ai-task.reopen`
- **Teams** — closure approval requested / approved / rejected / reopened notification types
- **UI** — premium `AiTaskClosurePanelClient` (Çözüm, Kanıtlar, Kapanış, Onay, Reopen); card badges; closure queue views with API-backed counts
- **Migration** — `20260903133347_AddAiTaskClosureEvidenceFoundation`

## Evidence

- Entity: `AiTaskEvidence` with type, title, description, reference metadata, source module/entity, submitter audit fields
- Types: photo, document, measurement, machine-reading, quality-record, maintenance-record, production-record, external-reference, note
- API: `GET/POST /api/ai-tasks/{taskId}/evidence`
- Policy-driven minimum count and required types (configurable per category/priority/source module/facility)
- Audit event: `evidence-submitted`

## Resolution

- Fields: `resolutionSummary`, `resolutionCode`, `closureReason` on task
- API: `POST /api/ai-tasks/{taskId}/resolution`
- Resolution codes: resolved, mitigated, accepted-risk, false-positive, deferred-follow-up
- Audit event: `resolution-updated`

## Closure Validation

- `AiTaskClosureEngine.ValidateClosureRequest` checks status, closure state, resolution summary/code, evidence count/types
- Returns structured issues (`code`, `message`); API maps `closure-validation-failed` → 409
- `GET /api/ai-tasks/{taskId}/closure-validation` + embedded `closureValidation` on detail read model
- UI shows missing requirements before "Tamamlanmak üzere gönder"

## Approval / Four-Eyes

- Policy flag `fourEyesRequired`; requester cannot approve own closure when enabled
- `POST /api/ai-tasks/{taskId}/closure-request` → `awaiting-closure-approval` when policy requires approval
- `POST /api/ai-tasks/{taskId}/approve-closure` completes task (`closed`, `finalClosedAtUtc`)
- Failure code `ai-task.four-eyes-violation` → 409
- Live flow: self-approval blocked; manager approval succeeds

## Rejection

- `POST /api/ai-tasks/{taskId}/reject-closure` requires reason
- Returns task to `in-progress`, sets `closure-rejected`, preserves prior resolution/evidence
- Audit event: `closure-rejected`; Teams notification to requester

## Reopen

- `POST /api/ai-tasks/{taskId}/reopen` on completed tasks with reason + permission
- Clears active closure request fields but preserves audit trail (prior `closure-approved` events remain)
- Sets `reopened` closure state, status → `in-progress`
- Audit event: `task-reopened` with `priorFinalClosedAtUtc` metadata

## SLA Interaction

- SLA derivation unchanged (`AiTaskSlaEngine`); approval wait does **not** pause SLA clocks (explicit, no ambiguous pause logic)
- `closureRequestedAtUtc`, `finalClosedAtUtc` recorded for operational reporting
- `resolvedAtUtc` set on final closure completion

## Escalation Interaction

- Existing escalation idempotency and SLA live flows unaffected
- Rejected/reopened tasks return to actionable `in-progress` and remain eligible for escalation per existing rules

## Security / RLS

- `ai_task_evidence` and `ai_task_closure_policies`: ENABLE + FORCE RLS, tenant-scoped policies, runtime grants
- All operations scoped by tenant/facility via existing `AiTaskOperationContext`
- Four-eyes + permission checks on approve/reject/reopen/evidence

## Teams

- New types: `task-closure-approval-requested`, `task-closure-approved`, `task-closure-rejected`, `task-reopened`
- Deep-link via existing task route builder
- Idempotent fingerprints per task/recipient

## Shift Handover Compatibility

- No Shift Handover code changes; source-linked tasks use same `ai_tasks` record and closure endpoints
- `sourceModule: shift-handover` tasks inherit policies via `sourceModule` policy scoring

## UI

- Detail: `AiTaskClosurePanelClient` integrated in `/ai-gorevleri/[taskId]` main column
- Card badges: Kanıt gerekli, Onay bekliyor, Revizyon istendi, Kapanış hazır
- Queue views: Onay Bekleyenler, Kanıt Eksik, Revizyon Bekleyen, Yeniden Açılanlar (`closureQueue` API filter)
- Audit labels for all new event types

## Live Flow

Script: `apps/web/scripts/verify-ai-task-closure-1-live-flow.mjs`

Flow A (approval + four-eyes): assign → lifecycle to in-progress → closure blocked without evidence → evidence + resolution → closure request → self-approval blocked → manager approval → completed + timeline ✓

Flow B (rejection): second task → closure request → reject with reason → in-progress + closure-rejected ✓

Flow C (reopen): reopen completed task → in-progress + prior history preserved ✓

API `/health/ready` = 200 before flow.

## Verifiers

| Verifier | Result |
|----------|--------|
| A. evidence policy | Engine + seeded policies |
| B. evidence persistence/RLS | Migration + RLS SQL |
| C. closure validation | Live flow blocked without evidence |
| D. four-eyes approval | Live flow self-approval 409 |
| E. rejection | Live flow Flow B |
| F. reopen | Live flow Flow C |
| G. audit/timeline | Live flow timeline assertions |
| H. permissions | Migration seed + API permission gates |
| I. tenant/facility isolation | Existing context + RLS |
| J. SLA regression | No SLA engine changes |
| K. escalation regression | No escalation engine changes |
| L. Teams notifications | Types + repository methods wired |
| M. Shift Handover compatibility | No duplicate task engine |
| N. dashboard freeze | 0 protected-path diffs vs 038f8ee |
| O. authenticated live flow | Script passed |

## Screenshots

- `.cursor-screenshots/ai-task-closure-1-resolution-evidence.png`
- `.cursor-screenshots/ai-task-closure-1-approval-request.png`
- `.cursor-screenshots/ai-task-closure-1-approved.png`
- `.cursor-screenshots/ai-task-closure-1-rejected.png`
- `.cursor-screenshots/ai-task-closure-1-reopened.png`

## Dashboard Absolute Freeze

protected dashboard paths changed = 0

## Git

```
git diff --check
```
CRLF warnings only (no conflict markers)

```
git status --short
```
Modified: 28 tracked files; new: closure backend, migration, API routes, UI panel, verifier, screenshots

```
git diff --stat
```
28 files changed, 1654 insertions(+), 14 deletions(-) (tracked); additional untracked new files listed above

staged = 0  
commit = NO

## FINAL VERDICT

READY_FOR_VISUAL_REVIEW

VISUAL STATUS = PENDING USER APPROVAL
