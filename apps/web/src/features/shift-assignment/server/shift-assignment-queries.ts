import "./shift-assignment-server-boundary";

import {
  SHIFT_ASSIGNMENT_CONTEXT_OPTIONS,
  PILOT_SHIFT_ASSIGNMENT_CONTEXT,
} from "../domain/shift-assignment-labels";
import type {
  ShiftAssignmentCapabilities,
  ShiftAssignmentCandidateDetail,
  ShiftAssignmentCandidatesResult,
  ShiftAssignmentContextOptions,
  ShiftAssignmentDetail,
  ShiftAssignmentListFilters,
  ShiftAssignmentListResult,
  ShiftAssignmentSelectionContext,
} from "../domain/shift-assignment-types";
import type { ShiftAssignmentApiFailure } from "./shift-assignment-api";
import {
  getShiftAssignment,
  getShiftAssignmentCandidate,
  listShiftAssignmentAuditEvents,
  listShiftAssignmentCandidates,
  listShiftAssignments,
  probeShiftAssignmentPermissions,
} from "./shift-assignment-api";
import { resolveShiftWorkforceRequestContext } from "../../shift-workforce/server/shift-workforce-request-context";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
  code?: ShiftAssignmentApiFailure["code"] | "invalid-scope";
}>;

export type ShiftAssignmentPermissionsResult =
  | Readonly<{ status: "success"; capabilities: ShiftAssignmentCapabilities }>
  | ReadFailure;

export type ShiftAssignmentContextOptionsResult =
  | Readonly<{ status: "success"; options: ShiftAssignmentContextOptions }>
  | ReadFailure;

export async function resolveContext() {
  const contextResult = resolveShiftWorkforceRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure" as const,
      message: contextResult.message,
      code: contextResult.code as "configuration-error" | "unauthenticated" | "invalid-scope",
    };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readShiftAssignmentPermissions(): Promise<ShiftAssignmentPermissionsResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const capabilities = await probeShiftAssignmentPermissions(resolved.context);
  return { status: "success", capabilities };
}

export async function readShiftAssignmentContextOptions(): Promise<ShiftAssignmentContextOptionsResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  return {
    status: "success",
    options: SHIFT_ASSIGNMENT_CONTEXT_OPTIONS[0],
  };
}

export function getDefaultShiftAssignmentSelection(): ShiftAssignmentSelectionContext {
  const today = new Date().toISOString().slice(0, 10);
  return {
    productionOrderReference: PILOT_SHIFT_ASSIGNMENT_CONTEXT.productionOrderReference,
    machineAssetId: PILOT_SHIFT_ASSIGNMENT_CONTEXT.machineAssetId,
    productionShiftId: PILOT_SHIFT_ASSIGNMENT_CONTEXT.productionShiftId,
    shiftDate: today,
    requiredCompetencyCode: PILOT_SHIFT_ASSIGNMENT_CONTEXT.requiredCompetencyCode,
  };
}

export async function readShiftAssignmentCandidates(
  selection: ShiftAssignmentSelectionContext,
): Promise<
  | Readonly<{ status: "success"; result: ShiftAssignmentCandidatesResult }>
  | ReadFailure
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const candidatesResult = await listShiftAssignmentCandidates(resolved.context, selection);
  if (candidatesResult.status === "failure") {
    return {
      status: "failure",
      message: candidatesResult.message,
      code: candidatesResult.code,
    };
  }

  return {
    status: "success",
    result: candidatesResult.value,
  };
}

export async function readShiftAssignmentCandidateDetail(
  personId: string,
  selection: ShiftAssignmentSelectionContext,
): Promise<
  | Readonly<{ status: "success"; candidate: ShiftAssignmentCandidateDetail }>
  | ReadFailure
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const candidateResult = await getShiftAssignmentCandidate(
    resolved.context,
    personId,
    selection,
  );
  if (candidateResult.status === "failure") {
    return {
      status: "failure",
      message: candidateResult.message,
      code: candidateResult.code,
    };
  }

  return {
    status: "success",
    candidate: candidateResult.value,
  };
}

export async function readShiftAssignmentRegistry(
  filters: ShiftAssignmentListFilters = {},
): Promise<
  | Readonly<{ status: "success"; assignments: ShiftAssignmentListResult }>
  | ReadFailure
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const assignmentsResult = await listShiftAssignments(resolved.context, filters);
  if (assignmentsResult.status === "failure") {
    return {
      status: "failure",
      message: assignmentsResult.message,
      code: assignmentsResult.code,
    };
  }

  return {
    status: "success",
    assignments: assignmentsResult.value,
  };
}

export async function readShiftAssignmentDetail(
  assignmentId: string,
): Promise<
  | Readonly<{
      status: "success";
      assignment: ShiftAssignmentDetail;
      auditEvents: readonly import("../domain/shift-assignment-types").ShiftAssignmentAuditEvent[];
    }>
  | ReadFailure
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [assignmentResult, auditResult] = await Promise.all([
    getShiftAssignment(resolved.context, assignmentId),
    listShiftAssignmentAuditEvents(resolved.context, assignmentId),
  ]);

  if (assignmentResult.status === "failure") {
    return {
      status: "failure",
      message: assignmentResult.message,
      code: assignmentResult.code,
    };
  }

  if (auditResult.status === "failure") {
    return {
      status: "failure",
      message: auditResult.message,
      code: auditResult.code,
    };
  }

  return {
    status: "success",
    assignment: assignmentResult.value,
    auditEvents: auditResult.value,
  };
}
