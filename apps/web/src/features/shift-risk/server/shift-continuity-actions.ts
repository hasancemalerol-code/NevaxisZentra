"use server";

import "./shift-risk-server-boundary";

import type {
  EvaluateShiftContinuityCommand,
  ShiftContinuityAssessmentListFilters,
} from "../domain/shift-continuity-types";
import {
  evaluateShiftContinuity,
  getShiftContinuityAssessment,
  getShiftContinuityOverview,
  listShiftContinuityAssessments,
} from "./shift-continuity-api";
import { resolveShiftWorkforceRequestContext } from "../../shift-workforce/server/shift-workforce-request-context";

async function resolveContext() {
  const contextResult = resolveShiftWorkforceRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure" as const,
      message: contextResult.message,
      code: contextResult.code,
    };
  }
  return { status: "success" as const, context: contextResult.context };
}

export async function loadShiftContinuityOverviewAction() {
  const resolved = await resolveContext();
  if (resolved.status === "failure") return resolved;

  const result = await getShiftContinuityOverview(resolved.context);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }
  return { status: "success", overview: result.value };
}

export async function loadShiftContinuityAssessmentsAction(
  filters: ShiftContinuityAssessmentListFilters = {},
) {
  const resolved = await resolveContext();
  if (resolved.status === "failure") return resolved;

  const result = await listShiftContinuityAssessments(resolved.context, filters);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }
  return { status: "success", assessments: result.value };
}

export async function loadShiftContinuityAssessmentDetailAction(assessmentId: string) {
  const resolved = await resolveContext();
  if (resolved.status === "failure") return resolved;

  const result = await getShiftContinuityAssessment(resolved.context, assessmentId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }
  return { status: "success", assessment: result.value };
}

export async function evaluateShiftContinuityAction(command: EvaluateShiftContinuityCommand) {
  const resolved = await resolveContext();
  if (resolved.status === "failure") return resolved;

  const result = await evaluateShiftContinuity(resolved.context, command);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }
  return { status: "success", result: result.value };
}
