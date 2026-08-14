"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import type {
  CreateShiftWorkforceCompetencyCommand,
  ShiftWorkforceListFilters,
  UpdateShiftWorkforceCompetencyCommand,
} from "../domain/shift-workforce-types";
import {
  createShiftWorkforceCompetency,
  synchronizeShiftWorkforce,
  updateShiftWorkforceCompetency,
} from "./shift-workforce-api";
import {
  readShiftWorkforcePersonDetail,
  readShiftWorkforcePersons,
  resolveContext,
} from "./shift-workforce-queries";

type ActionResult =
  | Readonly<{ status: "success"; id: string; version: number }>
  | Readonly<{ status: "failure"; message: string }>;

export async function loadShiftWorkforcePersonsAction(
  filters: ShiftWorkforceListFilters = {},
) {
  const authorization = await denyUnlessGranted("shift-workforce.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await readShiftWorkforcePersons(filters);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    persons: result.persons,
  };
}

export async function loadShiftWorkforcePersonDetailAction(personId: string) {
  const authorization = await denyUnlessGranted("shift-workforce.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await readShiftWorkforcePersonDetail(personId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    person: result.person,
    canManage: result.canManage,
  };
}

export async function createShiftWorkforceCompetencyAction(
  personId: string,
  command: CreateShiftWorkforceCompetencyCommand,
): Promise<ActionResult> {
  const authorization = await denyUnlessGranted("shift-workforce.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return { status: "failure", message: resolved.message };
  }

  const result = await createShiftWorkforceCompetency(
    resolved.context,
    personId,
    command,
  );

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/vardiyalar");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

export async function updateShiftWorkforceCompetencyAction(
  personId: string,
  assignmentId: string,
  command: UpdateShiftWorkforceCompetencyCommand,
): Promise<ActionResult> {
  const authorization = await denyUnlessGranted("shift-workforce.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return { status: "failure", message: resolved.message };
  }

  const result = await updateShiftWorkforceCompetency(
    resolved.context,
    personId,
    assignmentId,
    command,
  );

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/vardiyalar");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

export async function synchronizeShiftWorkforceAction(): Promise<
  | Readonly<{ status: "success" }>
  | Readonly<{ status: "failure"; message: string }>
> {
  const authorization = await denyUnlessGranted("shift-workforce.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return { status: "failure", message: resolved.message };
  }

  const result = await synchronizeShiftWorkforce(resolved.context);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/vardiyalar");
  return { status: "success" };
}
