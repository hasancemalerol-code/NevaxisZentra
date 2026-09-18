# NevaxisZentra Dashboard Lock

- Status: Locked
- Date: 2026-08-04
- Screen: Primary Dashboard / AI Task Center
- Change level: No structural change without explicit product approval

## Purpose

This document locks the primary dashboard’s information architecture, visual language, content boundary, and relationship to AI Task Center. Implementation may improve accessibility, responsiveness, performance, and data correctness without redesigning the structure.

## Positioning

The dashboard is the daily factory-operations view for the AI Production Manager and AI Task Center. It is not an accounting, finance, payroll, official inventory, or general purchasing ERP screen.

AI Task Center data shown on the dashboard is authoritative for tasks and approvals. NevaxisZentra Teams and Web reference the same records.

## Locked Visual Language

- Light, corporate background
- Light blue-gray sidebar
- White content cards
- Dark navy primary text
- Blue active states and links
- Green normal and success states
- Orange attention and medium risk
- Red critical risk and failure
- Purple AI, confidence, and analysis emphasis
- Light borders and restrained shadows
- Desktop-first, readable ERP/MES-level information density
- Clear card, table, and status boundaries

The dashboard must not be converted to a dark, neon, chat-only, or heavily decorative AI interface.

## Locked Structure

The following order and major regions are preserved:

```text
Left Sidebar
  +
Top Bar
  +
Top KPI Cards
  +
AI Task Center
  +
Daily Operations Summary
  +
Risks and Alerts
  +
Plan Reliability
  +
Machine Status
  +
Active Production Orders
  +
Live Alarms
  +
Material Risks
  +
Mold Status
  +
Maintenance and Breakdowns
  +
Quick Actions
  +
Subsystem Health Bar
```

Card contents may evolve within their purpose, but a package must not reorder, remove, merge, or replace major regions without explicit approval.

## Sidebar Boundary

The primary sidebar and dashboard must not add native entries or modules for:

- Finance
- Accounting
- Receivables/payables
- Official or native stock ledger
- Payroll or personnel master records
- General purchasing ERP
- General CRM, HR, WMS, banking, or invoicing

Material risks, critical-stock alerts, supplier risks, order risks, and operational cost impact may remain because they are factory-operation analysis derived from authoritative external data. Labels must not imply ledger ownership.

## Quick Actions

Quick Actions may create a native plan, task, draft, review, or approval request. They must not silently finalize official ERP records, approve critical actions, or issue unrestricted machine commands.

## Required States

Every region supports loading, empty, stale, partial, permission-denied, and error states. Source freshness and degraded integration health are visible where decisions depend on them.

## Change Control

Any structural change requires a written product decision describing user evidence, affected locked regions, information-architecture impact, migration, and updated acceptance criteria. Ordinary feature work does not authorize dashboard redesign.
