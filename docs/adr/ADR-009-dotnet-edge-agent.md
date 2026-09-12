# ADR-009: .NET Edge Agent

- Status: Accepted
- Date: 2026-08-04
- Decision owners: NevaxisFactOS Product & Development
- Scope: Factory-local machine, PLC, device, and network integrations

## Context

Factory devices often use OPC UA, Modbus, MQTT, serial protocols, TCP sockets, vendor APIs, files, or local network resources that must not be exposed directly to the cloud. Connectivity can be intermittent, and machine safety must remain local.

## Decision

NevaxisFactOS uses a separate .NET Edge Agent in `apps/edge-agent` for factory-local connectivity.

Edge Agent responsibilities include:

- Device identity and allowlisted adapter configuration
- Read-first telemetry acquisition and normalization
- Local buffering during connectivity loss
- Secure outbound synchronization
- Time, sequence, quality, and source metadata
- Health, diagnostics, and controlled configuration updates
- Execution of explicitly approved command contracts when enabled

The cloud API never connects directly into the factory control network.

## Safety Boundary

Emergency stops, interlocks, safety relays, safety PLC logic, guards, pressure limits, and hard-real-time control remain local and authoritative. Edge Agent and cloud services cannot disable, bypass, or replace them.

Unrestricted Modbus writes, OPC UA writes, PLC program changes, scripts, generated control code, or arbitrary vendor commands are prohibited.

## Approved Commands

An approved command requires:

- A versioned allowlisted command type and bounded parameters
- Tenant, facility, device, and user permission validation
- An active AI Task Center approval when critical
- Approval-expiry and stale-state revalidation
- A stable command/idempotency identifier
- Local device and safety acceptance
- Timeout, result, and verification behavior
- Audit and AI Decision Ledger linkage

Local safety logic may reject an approved cloud request. Rejection is a valid outcome and must be reported rather than bypassed.

## Security and Operations

- Edge identity uses rotatable per-installation credentials.
- Configuration is signed or integrity-protected and auditable.
- Inbound network exposure is minimized.
- Buffered data is encrypted where sensitive and bounded by retention policy.
- Updates support controlled rollout and rollback.
- Telemetry includes source time and ingestion time to expose stale data.

## Consequences

- Device adapters are isolated behind stable contracts.
- Telemetry is implemented before machine commands.
- Edge health and synchronization state are visible without leaking factory-network secrets.
