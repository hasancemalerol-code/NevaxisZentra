import "./shift-workforce-server-boundary";

import type {
  ShiftWorkforceCompetencyDefinition,
  ShiftWorkforceListFilters,
  ShiftWorkforceOverview,
  ShiftWorkforcePersonDetail,
  ShiftWorkforcePersonListResult,
} from "../domain/shift-workforce-types";
import {
  getShiftWorkforceOverview,
  getShiftWorkforcePerson,
  listShiftWorkforceCompetencyDefinitions,
  listShiftWorkforcePersons,
  probeShiftWorkforceReadPermission,
} from "./shift-workforce-api";
import type { ShiftWorkforceApiFailure } from "./shift-workforce-api";
import { resolveShiftWorkforceRequestContext } from "./shift-workforce-request-context";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
  code?: ShiftWorkforceApiFailure["code"] | "invalid-scope";
}>;

export type ShiftWorkforceWorkspaceResult =
  | Readonly<{
      status: "success";
      overview: ShiftWorkforceOverview;
      persons: ShiftWorkforcePersonListResult;
      competencyDefinitions: readonly ShiftWorkforceCompetencyDefinition[];
      canManage: boolean;
    }>
  | ReadFailure;

export type ShiftWorkforcePersonDetailResult =
  | Readonly<{
      status: "success";
      person: ShiftWorkforcePersonDetail;
      canManage: boolean;
    }>
  | ReadFailure;


export async function resolveContext() {
  const contextResult = await resolveShiftWorkforceRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure" as const,
      message: contextResult.message,
      code: contextResult.code as "configuration-error" | "unauthenticated" | "invalid-scope",
    };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readShiftWorkforceWorkspace(
  filters: ShiftWorkforceListFilters = {},
): Promise<ShiftWorkforceWorkspaceResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [overviewResult, personsResult, definitionsResult] = await Promise.all([
    getShiftWorkforceOverview(resolved.context),
    listShiftWorkforcePersons(resolved.context, filters),
    listShiftWorkforceCompetencyDefinitions(resolved.context),
  ]);

  if (overviewResult.status === "failure") {
    return {
      status: "failure",
      message: overviewResult.message,
      code: overviewResult.code,
    };
  }

  if (personsResult.status === "failure") {
    return {
      status: "failure",
      message: personsResult.message,
      code: personsResult.code,
    };
  }

  if (definitionsResult.status === "failure") {
    return {
      status: "failure",
      message: definitionsResult.message,
      code: definitionsResult.code,
    };
  }

  return {
    status: "success",
    overview: overviewResult.value,
    persons: personsResult.value,
    competencyDefinitions: definitionsResult.value,
    canManage: overviewResult.value.capabilities.canManage,
  };
}

export async function readShiftWorkforcePersons(
  filters: ShiftWorkforceListFilters = {},
): Promise<
  | Readonly<{ status: "success"; persons: ShiftWorkforcePersonListResult }>
  | ReadFailure
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const personsResult = await listShiftWorkforcePersons(resolved.context, filters);
  if (personsResult.status === "failure") {
    return {
      status: "failure",
      message: personsResult.message,
      code: personsResult.code,
    };
  }

  return {
    status: "success",
    persons: personsResult.value,
  };
}

export async function readShiftWorkforcePersonDetail(
  personId: string,
): Promise<ShiftWorkforcePersonDetailResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [overviewResult, personResult] = await Promise.all([
    getShiftWorkforceOverview(resolved.context),
    getShiftWorkforcePerson(resolved.context, personId),
  ]);

  if (personResult.status === "failure") {
    return {
      status: "failure",
      message: personResult.message,
      code: personResult.code,
    };
  }

  if (overviewResult.status === "failure") {
    return {
      status: "failure",
      message: overviewResult.message,
      code: overviewResult.code,
    };
  }

  return {
    status: "success",
    person: personResult.value,
    canManage: overviewResult.value.capabilities.canManage,
  };
}

export async function resolveShiftWorkforceNavigationVisibility(): Promise<boolean> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return false;
  }

  return probeShiftWorkforceReadPermission(resolved.context);
}
