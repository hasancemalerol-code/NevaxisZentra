# NevaxisFactOS Core Product Capabilities

- Status: Locked
- Date: 2026-08-04
- Category: AI Factory Operations Assistant
- Sector: Plastic injection molding and mold manufacturing

## Product Boundary

NevaxisFactOS consumes permitted data from existing ERP, HR, communication, machine, and factory systems and turns it into operational plans, risks, tasks, approvals, and audited actions.

It does not own accounting, finance, receivables/payables, official stock ledger, invoicing, payroll, classic purchasing records, or official customer, supplier, and personnel master records where another system is authoritative.

## 1. Omnichannel Order Intake and AI Drafts

NevaxisFactOS can ingest requests from email, WhatsApp Business Platform, Web forms, customer portals, sales messages, Excel/CSV, PDF, images, scanned documents, and entered call notes.

AI extracts customer reference, product, quantity, due date, material, color, mold, packaging, technical requirements, attachments, and uncertainty. The result is an approval-ready draft, never an automatically final official order. Missing information is explicit and external records remain linked by source identity.

## 2. AI Production Manager

The AI Production Manager analyzes open demand, current production, due-date risk, machines, molds, shifts, materials, maintenance, quality, and downtime. It produces a prioritized decision list and creates authoritative AI Task Center tasks where action is needed.

It cannot approve or directly execute critical actions.

## 3. Daily Factory Summary

The summary combines planned versus actual production, delayed work, critical machines, downtime, upcoming maintenance, mold status, material risk, shift gaps, quality/scrap signals, pending approvals, and priority management decisions. It may create tasks and approval requests rather than only generating a report.

## 4. Production Planning

Production planning is a native NevaxisFactOS capability. It evaluates demand priority and due date, machine/mold compatibility, cavity and cycle data, setup and changeover, material availability, maintenance, shift capacity, operator skills, quality risk, and external operations.

Plans may present alternatives such as lowest delay, setup, energy, mold changes, overtime, or risk. Each version records assumptions, evidence, freshness, constraints, expected effect, confidence, and approval state.

## 5. Plan Reliability

Reliability reflects source freshness, machine and mold availability, material sufficiency, personnel availability, cycle-data quality, maintenance conflicts, breakdown probability, quality risk, and external-operation uncertainty. Low or unknown reliability is visible and cannot be presented as certainty.

## 6. Shift Planning

Shift planning is a native locked capability. It combines production plans with availability, skills, certificates, absence, facility scope, maintenance and quality duties, workload, and overtime risk.

NevaxisFactOS owns operational shift scenarios and approved shift plans. HR/ERP remains authoritative for official personnel, payroll, and leave-accounting records. Publishing a shift plan follows Human-in-the-Loop rules.

## 7. Machine Intelligence

Edge Agent and supported integrations provide operational status, counters, cycle times, alarms, downtime, temperature, pressure, energy, and process deviations. NevaxisFactOS analyzes performance, capacity, anomaly, and maintenance impact while local machine control and safety remain authoritative.

## 8. Machine–Mold Compatibility

Compatibility evaluates tonnage, mold dimensions and weight, tie-bar spacing, injection volume, screw characteristics, clamping, robotics, auxiliary equipment, process needs, and known restrictions. Risky matches show the violated constraints and supporting data.

## 9. Mold Lifecycle and Maintenance Optimization

NevaxisFactOS owns the operational mold card, location, cavity data, status, shot count, production history, maintenance, failures, quality relationships, trials, revisions, and technical documents.

Maintenance optimization combines usage, failure history, quality signals, planned demand, and previous work to recommend a low-impact maintenance window and create a maintenance-task draft.

## 10. Maintenance and Breakdown Analysis

Native scope covers machine, mold, and auxiliary-equipment maintenance; periodic and predictive work; breakdown reports; recurring failure analysis; MTBF/MTTR; task coordination; and root-cause evidence in the injection-molding context.

## 11. Quality and Scrap Analysis

Analysis correlates product, machine, mold, cavity, material lot, operator, shift, process parameter, defect, and time. AI may identify recurring patterns and propose inspection, containment, or investigation tasks. Critical containment and external write-back require approval.

## 12. Material and Supplier Risk

NevaxisFactOS reads stock, reservations, safety stock, open purchasing, lots, and expected delivery from authoritative ERP/WMS/purchasing sources. It calculates shortages, depletion, production-blocking material, supplier delay, and alternatives.

It does not maintain the official inventory ledger or classic purchasing records. Approved purchasing drafts are written to the authoritative system through controlled integration.

## 13. Delivery and Bottleneck Risk

Risk analysis combines capacity, mold availability, maintenance, materials, staffing, setup, quality, downtime, outside operations, and shipment readiness to explain likely delay and recommended mitigation.

## 14. AI Task Center and NevaxisFactOS Teams

AI Task Center is authoritative for task, assignment, approval, delegation, expiry, evidence, execution, and result state.

NevaxisFactOS Teams provides internal messaging, routing, task interaction, approval viewing, and shift communication. Teams and Web use the same task and approval records; neither creates an independent workflow truth.

Tasks can route to purchasing, warehouse/material, order operations, finance/accounting, production, shift, maintenance, quality, and management units. Routing does not grant permission or change data ownership.

## 15. Audit, AI Decision Ledger, and Controlled Execution

AI outputs show rationale, confidence, risk, expected operational and financial effect, sources, freshness, missing data, approval state, and result. Critical execution requires permission and approval, immediate stale-data revalidation, bounded tool access, idempotency, outcome verification, and immutable audit evidence.

ERP access is read-only-first. Machine commands remain allowlisted and local safety controls cannot be bypassed.

## Acceptance Boundary

A capability is in scope only when it supports factory operations without recreating a classic ERP system of record. Any exception requires an explicit product decision and ADR.
