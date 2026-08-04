# CrevaFactOS User Roles

- Status: Locked
- Date: 2026-08-04
- Authorization model: Permission-based, scoped, deny by default

## Principle

Roles are maintained permission bundles, not the final authorization decision. Access evaluates:

```text
authenticated user
  + active membership
  + tenant/company/facility scope
  + permission
  + resource and operation scope
  + delegation
  + approval
  + operation limit
  + current state
```

CrevaFactOS does not create finance, accounting, receivables/payables, payroll, official inventory, or general purchasing ERP roles. Users from those units receive only the operational read, task, review, approval, or connector permissions needed in CrevaFactOS.

## Initial Operational Roles

| Role bundle | Primary responsibility | Typical boundaries |
| --- | --- | --- |
| Tenant Administrator | Membership, role bundles, configuration | Cannot approve its own elevation or bypass audit |
| Factory Manager | Cross-functional operational oversight | Facility-scoped; critical execution follows approval policy |
| Production Manager | Production priorities, plans, and release review | No unrestricted ERP or machine write |
| Production Planner | Plan scenarios and constraints | Publishing requires assigned permission and approval where configured |
| Shift Supervisor | Shift execution, assignments, communication | Facility/shift scoped; no payroll ownership |
| Operator | Assigned production and evidence capture | Own assignments and allowlisted equipment context only |
| Mold Manager | Mold lifecycle, availability, and maintenance coordination | Mold/facility scope |
| Maintenance Lead/Technician | Maintenance, breakdown, diagnosis, and completion evidence | Assigned assets; critical machine commands remain controlled |
| Quality Manager/Inspector | Inspections, nonconformance, scrap, and containment review | Quality scope; critical containment requires policy approval |
| Material/Warehouse Coordinator | Material readiness, lot verification, and shortage tasks | No official inventory ledger mutation |
| Purchasing Liaison | Supplier/material risk and approved purchasing-draft tasks | ERP remains purchasing system of record |
| Order Operations | Omnichannel intake, validation, and order-draft review | Cannot silently finalize an official ERP order |
| Finance/Accounting Reviewer | Operational financial-risk review | Read/review/approve only as granted; no native ledger |
| Integration Administrator | Connector mapping, health, and credentials | Cannot grant business approval through connector administration |
| Auditor | Read-only audit and AI Decision Ledger review | No mutation or operational execution |
| Executive/Management Viewer | Factory summary, risks, and assigned decisions | Tenant/facility and data-sensitivity scoped |

Organizations may compose narrower role bundles. Permission names and risk policies, not display titles, determine access.

## Unit Routing

AI Task Center and CrevaFactOS Teams can route work to:

- Purchasing
- Warehouse/material
- Order operations
- Finance/accounting
- Production
- Shift operations
- Maintenance
- Quality
- Management

Routing creates visibility or assignment; it does not grant permission, approval authority, or ownership of the underlying record.

## Separation of Duties

Risk policy can require different identities for requester, recommender, approver, and executor. At minimum:

- AI cannot approve its own proposal.
- A user cannot approve their own permission elevation.
- Connector administration does not imply business approval.
- Audit readers cannot change audited records.
- Critical write-back may prohibit requester self-approval.

## Delegation

Delegation must identify delegator, delegate, permission/approval scope, tenant/facility, valid-from and expiry times, reason, and revocation state. It cannot exceed the delegator’s authority and is fully audited.

## Approval Authority

Approval is an AI Task Center record, not a chat reaction or UI flag. It identifies the approver, exact operation scope, limits, evidence version, expiry, decision, and reason. Web and Teams act on the same record.

Approval becomes invalid when expired, revoked, already consumed where single-use, or when relevant permission, target state, input data, or requested scope materially changes.
