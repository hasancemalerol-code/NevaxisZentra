# CREVAFACTOS-AI-TASK-DECISION-INTELLIGENCE-REVIEW-ACTION-CLOSURE-1 — Mandatory Final Report

## STATUS

**READY_FOR_VISUAL_REVIEW**

## Root Cause

The **AI değerlendirmesi** card in **Görev genel bakış** rendered **"İncele"** as a static `Badge` via `getAiTaskActionTypeLabel(task.recommendation.actionType)` when `actionType === "review"`. It was not an interactive control and had no click handler, `aria-expanded`, or linked inspection surface. Facts (Tespit, Gerekçe, etc.) were always visible inline with no toggle, so users perceived **"İncele"** as a broken action.

A separate **"İncele"** label in the Karar desteği closure card called `runClosureReview()` (API mutation), which was semantically different and added confusion.

## Intended Behavior

**"İncele"** is now a keyboard-accessible secondary `Button` in the overview AI card. Clicking it toggles a deterministic expandable inspection panel (`data-testid="ai-task-decision-inspection"`) for the current task's AI assessment without page reload, state mutation, re-analysis, or duplicate audit entries.

When open, the panel shows structured read-only details:

| Alan | Kaynak |
|------|--------|
| Tespit | `task.detectedIssue` |
| Gerekçe | `task.reasoning` |
| Onay durumu | `task.approval.state` |
| Hedef sistem | `task.recommendation.targetSystem` |
| Güven | latest analysis confidence band or task confidence score |
| Analiz zamanı | `latestAnalysis.createdAtUtc` (when analysis exists) |
| İlgili öneri | pending recommendation title (when present) |
| Sağlayıcı / Model | secondary technical metadata from `latestAnalysis` |

Second click closes the panel. No raw JSON is shown.

**Implementation files:**

- `apps/web/src/features/ai-task-center/components/ai-task-detail-overview-ai-card-client.tsx` — new client component with inspect toggle
- `apps/web/src/app/(app)/ai-gorevleri/[taskId]/page.tsx` — wires overview card with `latestAnalysis` from decision workspace
- `apps/web/src/app/globals.css` — `.ai-task-detail-overview__*` inspection panel styles
- `apps/web/src/features/ai-task-center/presentation/ai-task-decision-intelligence-presentation.ts` — `localizeAiTaskFixtureText()` mapping
- Karar desteği closure action renamed to **"Kapanış analizi çalıştır"** to avoid collision with inspect

## Localization

**remaining user-facing English = 0**

Deterministic fixture/category label mapping via `localizeAiTaskFixtureText()`:

| English (fixture) | Turkish (UI) |
|-------------------|--------------|
| Review profitability comparison | Kârlılık karşılaştırmasını incele |
| Human review before operational action | Operasyonel aksiyon öncesi insan incelemesi |
| Live flow detected issue | Canlı akışta sorun tespit edildi |
| Live flow reasoning | Canlı akış değerlendirmesi |

Applied in overview AI card and Karar desteği recommendation titles/rationales.

## Functional Verification

| Check | Result |
|-------|--------|
| İncele click | **PASS** |
| detail open | **PASS** |
| detail close | **PASS** |
| no mutation | **PASS** (task version unchanged after toggle) |
| no duplicate audit | **PASS** (audit count unchanged after toggle) |

Verifier: `apps/web/scripts/verify-ai-task-decision-review-action-1-live-flow.mjs`

## Regression

| Suite | Result |
|-------|--------|
| decision intelligence (`verify-ai-task-decision-intelligence-1-live-flow.mjs`) | **PASS** |
| automation (`verify-ai-task-automation-1-live-flow.mjs`) | **PASS** |
| dashboard freeze (`verify-ai-task-sla-1-dashboard-protected-paths.mjs`) | **PASS** |

Protected dashboard paths changed = **0**. No dashboard source edits.

## Screenshots

| File | State |
|------|-------|
| `.cursor-screenshots/ai-task-decision-review-action-1-before-open.png` | Captured — inspection closed |
| `.cursor-screenshots/ai-task-decision-review-action-1-open.png` | Captured — inspection open after İncele click |

## Git

| Item | Value |
|------|-------|
| staged | **0** |
| commit | **NO** |

## FINAL VERDICT

**READY_FOR_VISUAL_REVIEW**
