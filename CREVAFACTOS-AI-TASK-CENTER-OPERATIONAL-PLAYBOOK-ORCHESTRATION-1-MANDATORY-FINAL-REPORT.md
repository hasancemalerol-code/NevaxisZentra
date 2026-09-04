# CREVAFACTOS-AI-TASK-CENTER-OPERATIONAL-PLAYBOOK-ORCHESTRATION-1 — Mandatory Final Report

## STATUS

**READY_FOR_VISUAL_REVIEW**

**VISUAL STATUS = PENDING USER APPROVAL**

## Starting HEAD

`1d255acce72373897c346769473b5fe42a9641bb`

## Architecture

Operational Playbooks extend the canonical AI Task Center without a second task engine.

| Layer | Responsibility |
|-------|----------------|
| Playbook template | Reusable procedure definition with versioned steps/dependencies |
| Playbook run | One live execution bound to source context and idempotency key |
| Playbook step | Unit of work mapped to canonical `ai_tasks` child items |
| Automation engine | May trigger `startPlaybook` deterministically |
| Decision Intelligence | May recommend `start-playbook` (human acceptance required) |
| Human | Launch, block/unblock, skip optional steps, cancel runs |

Precedence unchanged: mandatory automation policy → protected human override → AI advisory.

## Playbook Template

Tables: `ai_task_playbooks`, `ai_task_playbook_revisions`, `ai_task_playbook_steps`, `ai_task_playbook_step_dependencies`.

API: `GET/POST /api/ai-tasks/playbooks`, `GET/PUT /api/ai-tasks/playbooks/{id}`, enable/disable, `POST /api/ai-tasks/playbooks/validate-graph`.

Permissions: `ai-task.playbook.read`, `.manage`, `.run`, `.override`.

## Versioning

Updates create a new revision; existing runs retain their original `playbook_revision_id`. Historical run meaning is immutable.

## Steps

Step types: `task`, `approval`, `verification`, `decision-gate`. Policies per step: assignment, SLA, evidence, closure, optional condition, target duration.

## Dependency Graph

Explicit dependencies with `required-predecessor` / `optional-predecessor`. `AiTaskPlaybookGraphValidator` rejects self-deps, unknown steps, duplicates, and cycles.

## Sequential Execution

Live flow verified: A → B with B waiting until A completes.

## Parallel Execution

Live flow verified: C and D both become actionable after B completes; E remains blocked until both C and D complete.

## Conditional Steps

Bounded condition evaluation via `AiTaskPlaybookEngine.EvaluateCondition` (`sparePartRequired`, `qualityImpact`, `downtimeMinutes`). No arbitrary scripting.

## Canonical Task Integration

Each actionable step materializes a canonical `ai_tasks` row with `parent_task_id`, `playbook_run_id`, `playbook_run_step_id`, `dependency_state`. Existing assignment/SLA/escalation/evidence/closure/reopen infrastructure reused.

## Parent / Child Tasks

`ParentTaskId` on child tasks links to source/parent incident. Hierarchy loop prevention via single parent assignment at run start.

## Assignment / SLA

`ApplyRoutingOnCreateAsync` invoked for materialized child tasks. Playbook stores policy JSON; engines remain authoritative.

## Evidence / Approval

Approval steps create tasks in `awaiting-approval` with closure governance flags. Playbook orchestration cannot force-complete governed child tasks.

## Progress

Authoritative counters on run: required/optional completed totals, blocked count, approvals pending. Overall percent derived only from required steps.

## Critical Path

`AiTaskPlaybookEngine.CalculateCriticalPath` uses configured target durations when present; limited mode when duration data insufficient.

## Block / Unblock

`POST /api/ai-tasks/playbook-runs/{runId}/steps/{stepId}/block|unblock` with reason/actor. Downstream steps reevaluated. Live flow PASS.

## Completion

Run completes when all required steps complete and gates satisfied. Verified end-to-end in live flow.

## Cancellation

`POST /api/ai-tasks/playbook-runs/{runId}/cancel` requires reason; history preserved.

## Reopen

`NotifyTaskReopenedAsync` reactivates run step when linked child task reopened.

## Automation Integration

`AiTaskAutomationActionTypes.StartPlaybook` added. Automation repository starts run with idempotency key derived from execution context.

## Decision Intelligence Integration

`start-playbook` recommendation for critical maintenance/shift-handover tasks. Acceptance via recommendation flow starts canonical run; AI does not auto-launch.

## Shift Handover

Source-linked tasks with `sourceModule = shift-handover` used as playbook run context; child tasks retain source linkage.

## Teams

Playbook start notification via existing Teams task-link channel with idempotent audit fingerprint.

## Security / RLS

All new tables: RLS + FORCE RLS. Tenant/facility scoped. Permission-gated template/run/override actions.

## Database / Migration

Artifact: `.cursor-screenshots/playbook-migration.sql` (`20260904120000_AddAiTaskPlaybookOrchestrationFoundation`).

`ai_tasks` extended: `parent_task_id`, `playbook_run_id`, `playbook_run_step_id`, `dependency_state`, `dependency_block_reason`.

## API

`AiTaskPlaybookEndpointRouteBuilderExtensions.cs` registered in `Program.cs`.

## UI

Routes under `/ai-gorevleri/playbooklar` (library, builder, run detail). Turkish labels. Premium `.ai-task-playbook-*` styles in `globals.css`. Nav link from AI görevleri list.

## Live Flows

| Flow | Result |
|------|--------|
| Core playbook (A–F sequential/parallel) | **PASS** |
| Block/unblock | **PASS** |
| Cycle validation | **PASS** |
| Idempotency | **PASS** |
| Progress/audit | **PASS** |
| AI recommendation surface | **PASS** (screenshot) |

Verifier: `apps/web/scripts/verify-ai-task-playbook-1-live-flow.mjs`

## Verifiers

| Suite | Result |
|-------|--------|
| playbook live flow | **PASS** |
| decision intelligence regression | **PASS** |
| automation regression | **PASS** |
| dashboard protected paths | **PASS** |

## Screenshots

| File | State |
|------|-------|
| `.cursor-screenshots/ai-task-playbook-1-library.png` | Captured |
| `.cursor-screenshots/ai-task-playbook-1-builder.png` | Captured |
| `.cursor-screenshots/ai-task-playbook-1-run-overview.png` | Captured |
| `.cursor-screenshots/ai-task-playbook-1-parallel-steps.png` | Captured |
| `.cursor-screenshots/ai-task-playbook-1-blocked.png` | Captured |
| `.cursor-screenshots/ai-task-playbook-1-completed.png` | Captured |
| `.cursor-screenshots/ai-task-playbook-1-ai-recommendation.png` | Captured |

## Dashboard Absolute Freeze

protected dashboard paths changed = **0**

protected dashboard hashes changed = **0**

## Git

```
git diff --check  → CRLF warnings only (no conflict markers)
staged = 0
commit = NO
```

19 files changed, 842 insertions(+), 12 deletions(-) in tracked diff; additional new playbook files untracked.

## FINAL VERDICT

**READY_FOR_VISUAL_REVIEW**

**VISUAL STATUS = PENDING USER APPROVAL**
