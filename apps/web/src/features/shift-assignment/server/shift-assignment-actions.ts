"use server";

import { revalidatePath } from "next/cache";

import type {
  ApproveShiftAssignmentCommand,
  CancelShiftAssignmentCommand,
  CreateShiftAssignmentDraftCommand,
  ShiftAssignmentListFilters,
  ShiftAssignmentSelectionContext,
} from "../domain/shift-assignment-types";
import {
  approveShiftAssignment,
  cancelShiftAssignment,
  createShiftAssignmentDraft,
} from "./shift-assignment-api";
import {
  readShiftAssignmentCandidateDetail,
  readShiftAssignmentCandidates,
  readShiftAssignmentDetail,
  readShiftAssignmentRegistry,
  resolveContext,
} from "./shift-assignment-queries";

type ActionResult =
  | Readonly<{ status: "success"; id: string; version: number }>
  | Readonly<{ status: "failure"; message: string; code?: string }>;

export async function loadShiftAssignmentCandidatesAction(
  selection: ShiftAssignmentSelectionContext,
) {
  const result = await readShiftAssignmentCandidates(selection);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return {
    status: "success" as const,
    result: result.result,
  };
}

export async function loadShiftAssignmentCandidateDetailAction(
  personId: string,
  selection: ShiftAssignmentSelectionContext,
) {
  const result = await readShiftAssignmentCandidateDetail(personId, selection);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return {
    status: "success" as const,
    candidate: result.candidate,
  };
}

export async function loadShiftAssignmentRegistryAction(
  filters: ShiftAssignmentListFilters = {},
) {
  const result = await readShiftAssignmentRegistry(filters);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return {
    status: "success" as const,
    assignments: result.assignments,
  };
}

export async function loadShiftAssignmentDetailAction(assignmentId: string) {
  const result = await readShiftAssignmentDetail(assignmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return {
    status: "success" as const,
    assignment: result.assignment,
    auditEvents: result.auditEvents,
  };
}

export async function createShiftAssignmentDraftAction(
  command: CreateShiftAssignmentDraftCommand,
): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return { status: "failure", message: resolved.message, code: resolved.code };
  }

  const result = await createShiftAssignmentDraft(resolved.context, command);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  revalidatePath("/vardiyalar");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

export async function approveShiftAssignmentAction(
  assignmentId: string,
  command: ApproveShiftAssignmentCommand,
): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return { status: "failure", message: resolved.message, code: resolved.code };
  }

  const result = await approveShiftAssignment(resolved.context, assignmentId, command);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  revalidatePath("/vardiyalar");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

export async function cancelShiftAssignmentAction(
  assignmentId: string,
  command: CancelShiftAssignmentCommand,
): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return { status: "failure", message: resolved.message, code: resolved.code };
  }

  const result = await cancelShiftAssignment(resolved.context, assignmentId, command);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  revalidatePath("/vardiyalar");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}
