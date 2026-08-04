# ADR-008: Flutter Mobile Platform

- Status: Accepted
- Date: 2026-08-04
- Decision owners: CrevaFactOS Product & Development
- Scope: Mobile field experience

## Context

Factory managers, supervisors, operators, maintenance teams, quality teams, and material handlers require task-focused mobile access. Maintaining separate Android and iOS codebases would increase delivery and maintenance cost.

## Decision

CrevaFactOS mobile applications use Flutter in `apps/mobile`.

Mobile scope may include:

- AI Task Center task interaction and approval viewing
- Shift communication through CrevaFactOS Teams
- Production-plan and work-context viewing
- Machine and mold operational status
- Barcode and QR verification
- Material preparation and lot verification
- Maintenance and breakdown tasks
- Quality checks, first-piece review, and nonconformance capture
- Photos, attachments, alarms, and notifications
- Explicitly designed offline field workflows

Mobile stock checks or movements are operational interactions with the authoritative ERP/WMS integration; they do not establish a native official stock ledger. Mobile purchasing interaction is limited to risk review, task handling, and approved external drafts.

## Task and Approval Authority

Mobile and Teams display the same authoritative AI Task Center task and approval records as Web. Local offline state cannot create a final approval. Approval submission requires server validation of identity, permission, delegation, expiry, scope, and current data.

## Offline Rules

- Offline-capable operations are explicitly allowlisted.
- Local data is encrypted and minimized.
- Sync uses stable client operation identifiers and idempotent server handling.
- Conflicts and stale approvals fail safely and require review.
- Critical ERP write-back and machine commands are not executed solely from unvalidated offline state.

## Security

The application uses secure token storage, certificate-valid HTTPS, permission-aware screens, remote session revocation, and auditable device/session context. Secrets and unrestricted connector credentials are never embedded in the client.

## Consequences

- Flutter is an additional toolchain that requires pinned versions and targeted CI.
- Mobile features follow product ownership boundaries in ADR-011.
- Mobile UI does not replace the locked desktop dashboard.
