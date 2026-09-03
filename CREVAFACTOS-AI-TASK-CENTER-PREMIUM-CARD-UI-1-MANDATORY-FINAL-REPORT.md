## STATUS

Premium operational task-card presentation is implemented for AI Görev Merkezi. Card view is the default desktop experience; compact list view remains available via explicit **Liste görünümü** toggle. All queue, SLA, assignment, escalation, routing, and detail functionality is preserved. Dashboard protected paths remain unchanged.

## Scope

**In scope**
- `apps/web/src/features/ai-task-center/**` presentation layer
- `apps/web/src/app/globals.css` AI Task Center unified/card styles
- KPI summary cards (removed demo preview copy)
- Live-flow verifier script for premium card UI

**Out of scope (untouched)**
- `apps/web/src/app/(app)/dashboard/**`
- `apps/web/src/components/dashboard/**`
- `apps/web/src/features/dashboard/**`
- `apps/web/src/app/command-center.css`
- `apps/web/src/app/dashboard*.css`
- AI Task Center backend / API / persistence

## Card UI

### desktop grid
- Default presentation: 2-column responsive card grid (`.ai-task-center-unified__card-grid`)
- Collapses to 1 column below 1024px
- Secondary **Liste görünümü** toggle via `?gosterim=liste` URL param
- Compact table retained only in list mode

### metadata hierarchy
Each card (`AiTaskCenterItemCard`) renders:
- **Top row:** source badge, priority badge, SLA badge, active escalation badge
- **Main:** title, summary, facility, assignee/team with initials avatar
- **Status row:** lifecycle status, due date, acknowledgment state
- **Footer:** related context / source origin, **Görevi Aç** action

### SLA treatment
- Uses authoritative `slaState`, `slaDueAt`, and `formatAiTaskSlaCountdown()`
- Tone mapping: on-track (success), due-soon (warning), overdue/breached (danger)
- Display text examples: `SLA içinde`, countdown (`2 sa 14 dk kaldı`), `SLA riski`, `SLA ihlali`
- Left accent bar color reflects SLA state

### escalation treatment
- Compact badges via `getAiTaskEscalationBadgeLabel()`: Eskalasyon 1/2, Yönetim Eskalasyonu, Kritik Eskalasyon
- Shown only when escalation is active; critical levels get stronger accent

### assignment treatment
- Assignee display name or **Atanmamış**
- Team channel label when applicable
- Initials avatar (no fake photos)
- Acknowledgment: **Teslim alındı** / **Teslim bekleniyor**

### source treatment
- Source/module badges preserved (AI Analizi, Manuel Görev, Teams Mesajı, Vardiya Teslimi, Onay Talebi, etc.)
- Origin filters remain secondary compact chips below queue rail

## Demo Cleanup

UI önizleme occurrences in AI Task Center feature code = **0**

KPI cards no longer pass `preview` prop; helper text uses authoritative contextual copy:
- Açık görev → `{total} toplam görev`
- Onay bekleyen → `İnsan kararı gereken görev`
- Kritik risk → `Öncelikli müdahale`
- Geciken görev → `SLA / son tarih aşımı`

## Functional Regression

| Check | Result |
|---|---|
| assignment | PASS |
| SLA | PASS |
| escalation | PASS |
| queues | PASS |
| task detail | PASS |
| Shift Handover task linkage | PASS |

**Verifier evidence**
- `verify-ai-task-center-unification-1-contract.mjs` — PASS
- `verify-ai-task-center-unification-1-live-flow.mjs` — PASS
- `verify-ai-task-sla-1-sla-derivation.mjs` — PASS
- `verify-ai-task-sla-1-escalation-idempotency.mjs` — PASS
- `verify-ai-task-sla-1-live-flow.mjs` — PASS
- `verify-ai-task-sla-1-no-mock-data.mjs` — PASS
- `verify-ai-task-center-premium-card-ui-1-live-flow.mjs` — PASS

## Dashboard Freeze

protected path diffs = **0**

Baseline: `0ca1bf061ba0d2e385cc711d2804ed68f2a65c1a`

## Screenshots

| File | Description |
|---|---|
| `.cursor-screenshots/ai-task-center-premium-card-ui-1-all-tasks.png` | All tasks card grid (default view) |
| `.cursor-screenshots/ai-task-center-premium-card-ui-1-sla-risk.png` | SLA Riski queue card view |
| `.cursor-screenshots/ai-task-center-premium-card-ui-1-escalated.png` | Eskale Edilenler queue card view |
| `.cursor-screenshots/ai-task-center-premium-card-ui-1-detail.png` | Task detail with operations panel |
| `apps/web/.cursor-screenshots/ai-task-sla-1-dashboard-regression-authenticated.png` | Dashboard regression (unchanged) |

## Visual Status

**PENDING USER APPROVAL**

## Git

```
git diff --check
→ whitespace warning in unrelated CREVAFACTOS-SHIFT-HANDOVER-SAFE-REINTRODUCTION-2-MANDATORY-FINAL-REPORT.md (pre-existing)

git status --short (task-relevant)
 M apps/web/src/app/globals.css
 M apps/web/src/features/ai-task-center/application/ai-task-center-composition.ts
 M apps/web/src/features/ai-task-center/components/ai-task-center-unified-workspace.tsx
 M apps/web/src/features/ai-task-center/components/ai-task-kpi-links.tsx
 M apps/web/src/features/ai-task-center/components/ai-task-list-page-content.tsx
 M apps/web/src/features/ai-task-center/domain/ai-task-center-item.ts
 M apps/web/src/features/ai-task-center/presentation/ai-task-sla-labels.ts
?? apps/web/src/features/ai-task-center/components/ai-task-center-item-card.tsx
?? apps/web/scripts/verify-ai-task-center-premium-card-ui-1-live-flow.mjs
?? .cursor-screenshots/ai-task-center-premium-card-ui-1-*.png

staged = 0
commit = NO
```

**Build note:** `next build` compiles successfully; full TypeScript gate fails on pre-existing errors outside this task scope (`vardiya-devir` form actions, `shift-handover-request-context.ts`). No new TypeScript errors in `ai-task-center` files.

## FINAL VERDICT

**READY_FOR_VISUAL_REVIEW**
