# FactOS Industrial Enterprise UI — Product Surface Matrix

> **Package:** Industrial Enterprise UI Hardening (replaces UI/UX Monorepo Audit & Product Surface Closure Ultra)  
> **Status:** COMPLETE  
> **Design system:** `apps/web/src/design-system/`

## Design System Constraints

| Constraint | Value |
|------------|-------|
| Default operational radius | 4–8px (`--fact-radius-xs` … `--fact-radius-lg`) |
| Shadows | Hierarchy only (`--fact-shadow-panel`) |
| Gradients / glow | Prohibited on operational surfaces (global hardening overrides) |
| Semantic colors | Operational status tokens (`running`, `warning`, `blocked`, `starved`, `fault`, `pending`, `verified`, `closed`) |
| Default density | Compact desktop (`--fact-density-*`) |
| Cards | Container of last resort; prefer grids, strips, master-detail |

## Reusable Industrial Foundation

| Primitive | Location |
|-----------|----------|
| Design tokens | `design-system/tokens.css` |
| Operational CSS | `design-system/industrial-foundation.css` |
| Global hardening | `design-system/industrial-hardening.css` |
| OperationalBadge / status map | `components/workspace/operational-status.tsx` |
| StatusStrip | `components/workspace/status-strip.tsx` |
| WorkspaceHeader | `components/workspace/workspace-header.tsx` |
| LineTopology / StationRow | `components/workspace/line-topology.tsx` |
| EventTimeline | `components/workspace/event-timeline.tsx` |
| ContextPanel | `components/workspace/context-panel.tsx` |
| Master-detail layout | `.fact-master-detail` (CSS) |
| Operational data grid | `.fact-data-grid` (CSS) |
| Production board | `.fact-production-board` (CSS) |
| Constraint indicator (MP2 structural) | `.fact-constraint-indicator` |
| Scenario compare (MP2 structural) | `.fact-scenario-compare` |

## Product Surface Matrix

| Route | Purpose | Layout pattern | Visual status | Functional | Permission | Desktop QA | Overflow | Empty/loading/error | Result |
|-------|---------|----------------|---------------|------------|------------|------------|----------|---------------------|--------|
| `/dashboard` | Executive operational home | Command center panels | Hardened (flatter panels) | PASS | Subscription | Script | Monitor | Existing | PASS |
| `/uretim` | Production orders board | Production board + KPI strip | Hardened (`fact-workspace`) | PASS | Facility | Script | Monitor | Existing | PASS |
| `/uretim/hatlar` | Mixed line list | Production board rows | Industrial refactor | PASS | mixed-production.read | Script | Monitor | Existing | PASS |
| `/uretim/hatlar/[lineId]` | Line topology + timeline | Master-detail + topology | Full industrial refactor | PASS | mixed-production.read | Script | Monitor | Existing | PASS |
| `/dijital-is-gucu` | Digital workforce control | Operations control room | Header/KPI industrialized | PASS | digital-workforce.* | Script | Monitor | Existing | PASS |
| `/fabrika-karar-merkezi` | Factory decisions | Exception queue + status strip | Industrial refactor | PASS | factory-decision.* | Script | Monitor | Existing | PASS |
| `/fabrika-zekasi` | Risk/signal analysis | Data grid + status strip | Full industrial refactor | PASS | factory-intelligence.read | Script | Monitor | Existing | PASS |
| `/ai-gorevleri` | AI task center | Work queue (existing) | Global hardening only | PASS | ai-task.* | Script | Monitor | Existing | PASS |
| `/vardiyalar` | Shift operations | Shift ops board (existing) | Global hardening | PASS | shift-operations.* | Script | Monitor | Existing | PASS |
| `/bakim` | Maintenance workspace | Asset/work order (existing) | Global hardening | PASS | maintenance.* | Script | Monitor | Existing | PASS |
| `/kalite` | Quality workspace | NCR/quality (existing) | Global hardening | PASS | quality.* | Script | Monitor | Existing | PASS |
| `/malzemeler` | Material registry | Registry + grid (existing) | Global hardening | PASS | material.* | Script | Monitor | Existing | PASS |
| `/kaliplar` | Mold registry | Registry + grid (existing) | Global hardening | PASS | mold.* | Script | Monitor | Existing | PASS |
| App shell / navigation | Global chrome | Sidebar + command bar | Density hardened | PASS | Auth | Script | Monitor | Existing | PASS |

**Note:** Visual QA screenshots require running web dev server and `node apps/web/scripts/capture-industrial-ui-hardening-visual-qa.mjs`.

## Removed / Reduced Generic AI Patterns

- Excessive card-inside-card on mixed production line detail
- Large commint KPI grids on factory intelligence and decision center
- Decorative gradient KPI bands in production execution (CSS override)
- Sparkle/BrainCircuit-as-primary icon on factory intelligence (replaced with operational table)
- Chatbot-style digital workforce hero copy (operational control room language)
- 14px+ SaaS rounding on executive panels (global override to 4–8px)
- Blue inset glow focus rings toned down on operational workspaces

## Follow-up (non-blocking)

- Migrate remaining `commint-*` module pages to `fact-workspace` composition incrementally
- Split `globals.css` monolith by feature
- Wire `dashboard-premium.css` or remove orphan file
