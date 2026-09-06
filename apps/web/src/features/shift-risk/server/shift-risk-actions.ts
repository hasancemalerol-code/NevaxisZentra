"use server";

import "./shift-risk-server-boundary";

import type {
  ShiftRiskAssessmentListFilters,
  EvaluateShiftRiskCommand,
} from "../domain/shift-risk-types";
import {
  evaluateShiftRisk,
  getShiftRiskAssessment,
  getShiftRiskOverview,
  listShiftRiskAssessments,
} from "./shift-risk-api";
import { resolveShiftWorkforceRequestContext } from "../../shift-workforce/server/shift-workforce-request-context";

async function resolveContext() {
  const contextResult = await resolveShiftWorkforceRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure" as const,
      message: contextResult.message,
      code: contextResult.code,
    };
  }
  return { status: "success" as const, context: contextResult.context };
}

export async function loadShiftRiskOverviewAction() {
  const resolved = await resolveContext();
  if (resolved.status === "failure") return resolved;

  const result = await getShiftRiskOverview(resolved.context);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }
  return { status: "success", overview: result.value };
}

export async function loadShiftRiskAssessmentsAction(filters: ShiftRiskAssessmentListFilters = {}) {
  const resolved = await resolveContext();
  if (resolved.status === "failure") return resolved;

  const result = await listShiftRiskAssessments(resolved.context, filters);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }
  return { status: "success", assessments: result.value };
}

export async function loadShiftRiskAssessmentDetailAction(assessmentId: string) {
  const resolved = await resolveContext();
  if (resolved.status === "failure") return resolved;

  const result = await getShiftRiskAssessment(resolved.context, assessmentId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }
  return { status: "success", assessment: result.value };
}

export async function evaluateShiftRiskAction(command: EvaluateShiftRiskCommand) {
  const resolved = await resolveContext();
  if (resolved.status === "failure") return resolved;

  const result = await evaluateShiftRisk(resolved.context, command);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }
  return { status: "success", result: result.value };
}
