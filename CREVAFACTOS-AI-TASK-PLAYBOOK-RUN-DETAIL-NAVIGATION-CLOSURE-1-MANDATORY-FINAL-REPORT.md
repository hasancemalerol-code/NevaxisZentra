# CREVAFACTOS-AI-TASK-PLAYBOOK-RUN-DETAIL-NAVIGATION-CLOSURE-1 — MANDATORY FINAL REPORT

## STATUS

Run-detail navigation from **Çalışan Akışlar** is fixed and verified. The canonical run-detail route loads the existing visual-review run with correct data; all regression verifiers pass.

## Root Cause

Two presentation-layer issues combined:

1. **Turkish view query not recognized** — URLs using `?gorunum=calisan` were not mapped to the active-runs view. The library client only accepted `gorunum=active`, so deep links/bookmarks with `calisan` rendered the wrong tab content and broke the expected navigation context.
2. **Weak run-card navigation affordance** — `Detayı gör` used a low-contrast `ai-task-playbook-inline-link` without a stable test id or premium button styling, making the action easy to miss and harder to verify. The underlying route `/ai-gorevleri/playbooklar/calisma/{runId}` was already correct.

No orchestration, API, or route-tree changes were required beyond a safety `notFound()` guard on `[playbookId]` when `playbookId === "calisma"`.

## Canonical Route

| Item | Value |
|---|---|
| Run detail route | `/ai-gorevleri/playbooklar/calisma/{runId}` |
| Active runs library view | `/ai-gorevleri/playbooklar?gorunum=calisan` |
| Data source | `GET /api/ai-tasks/playbook-runs/{runId}` via `readAiTaskPlaybookRunDetail` |

## Identifier

| Check | Result |
|---|---|
| `Detayı gör` uses `run.id` (not playbookId/sourceTaskId/key) | **PASS** |
| Link href matches clicked run card test id | **PASS** |
| Detail page loads same runId | **PASS** |

## Navigation

| Check | Result |
|---|---|
| Mouse click | **PASS** |
| Keyboard Enter | **PASS** |
| URL changes to `/calisma/{runId}` | **PASS** |
| Browser Back returns to `?gorunum=calisan` | **PASS** |
| No run mutation on navigation | **PASS** |

## Detail Data

| Check | Result |
|---|---|
| Correct run (Kritik Makine Olayı) | **PASS** |
| 6 steps | **PASS** |
| Progress 2/6 | **PASS** |
| Status Devam Ediyor | **PASS** |

## Security

| Check | Result |
|---|---|
| Authenticated browser session required | **PASS** |
| Canonical API fetch with facility context | **PASS** |
| Invalid run shows product not-found copy | **PASS** (`Playbook çalışması bulunamadı.`) |

## Regression

| Verifier | Result |
|---|---|
| `verify-ai-task-playbook-run-detail-navigation-1-live-flow.mjs` | **PASS** |
| `verify-ai-task-playbook-1-live-flow.mjs` | **PASS** |
| `verify-ai-task-decision-intelligence-1-live-flow.mjs` | **PASS** |
| `verify-ai-task-automation-1-live-flow.mjs` | **PASS** |
| `verify-ai-task-sla-1-dashboard-protected-paths.mjs` | **PASS** |

## Dashboard Freeze

| Check | Result |
|---|---|
| Protected dashboard source path diffs | **0** |

## Screenshots

| File | Purpose |
|---|---|
| `.cursor-screenshots/ai-task-playbook-run-detail-nav-1-running-list.png` | Çalışan Akışlar with Detayı gör |
| `.cursor-screenshots/ai-task-playbook-run-detail-nav-1-detail-open.png` | Run detail after navigation |

## Git

| Check | Result |
|---|---|
| staged | **0** |
| commit | **NO** |

## FINAL VERDICT

**READY_FOR_VISUAL_REVIEW**
