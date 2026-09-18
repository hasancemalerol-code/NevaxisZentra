# NevaxisZentra AI Agent Catalog

- Status: Locked
- Date: 2026-08-04
- Category: AI Factory Operations Assistant

## Common Agent Contract

Every agent operates within the initiating actor’s validated tenant, facility, permission, and data scope. Agents use only versioned allowlisted tools, expose evidence and freshness, create bounded drafts or recommendations, and use AI Task Center for tasks and approvals.

Agents cannot access databases, queues, ERP, files, Edge devices, PLCs, or machines directly; approve themselves; change tenant or permission; invent missing data; or alter audit evidence.

## Initial Agents

| Agent | Responsibility | Primary inputs | Outputs | Critical approval examples |
| --- | --- | --- | --- | --- |
| AI Production Manager | Prioritize daily factory decisions | Demand, plans, shifts, machines, molds, maintenance, quality, materials | Daily summary, risk ranking, tasks | Releasing a material operational change |
| Order Intake Agent | Structure omnichannel requests | Email, WhatsApp, files, forms, customer references | AI order draft, missing-data questions, duplicate risk | Creating/updating an official ERP order |
| Production Planning Agent | Generate and compare plans | Demand, capacity, mold, cycle, setup, material, maintenance, shift | Versioned plan scenarios and reliability | Publishing an approved production plan |
| Shift Planning Agent | Build operational shift scenarios | Production plan, skills, availability, absences, workload | Shift plan, coverage gap, assignment tasks | Publishing or materially changing a shift plan |
| Machine Intelligence Agent | Detect machine anomalies and capacity risk | Edge telemetry, alarms, cycles, downtime | Anomaly, performance, bottleneck, task | Approved bounded machine command |
| Mold Lifecycle Agent | Evaluate compatibility and lifecycle risk | Mold history, shots, failures, quality, demand | Compatibility result, maintenance window, task | Maintenance release affecting production |
| Maintenance Agent | Analyze failures and maintenance needs | Asset history, alarms, inspections, work evidence | Priority, root-cause hypothesis, work draft | Critical shutdown or machine action |
| Quality and Scrap Agent | Find recurring quality patterns | Defects, scrap, cavity, lot, process, shift | Risk, investigation, containment draft | Quarantine or external system update |
| Material and Supplier Risk Agent | Detect shortages and delivery risk | ERP/WMS stock projection, open purchasing, supplier dates, plan | Shortage, supplier risk, purchasing draft | ERP purchasing draft/write-back |
| Delivery Risk Agent | Predict due-date and bottleneck risk | Plans, capacity, materials, maintenance, quality, outside work | Risk explanation and mitigation tasks | Committing an external delivery change |

## Tool and Output Requirements

Every output identifies agent and model, tool/schema versions, tenant/facility, sources, timestamps, freshness, missing data, assumptions, confidence, risk, proposed operation scope, and correlation ID.

An agent recommendation is not an approval. Execution tools revalidate authoritative state immediately before use and apply stable idempotency keys for external operations.

## Routing

Agents may route authoritative AI Task Center work through NevaxisZentra Teams to purchasing, warehouse/material, order operations, finance/accounting, production, shift, maintenance, quality, or management. Routing cannot bypass unit permissions or create parallel approval state.

## Adding an Agent

A new agent requires a defined operational problem, owner, data sources, tool allowlist, permission scope, output schema, risk classification, approval policy, evaluation set, failure behavior, audit fields, and confirmation that it does not recreate a classic ERP capability.
