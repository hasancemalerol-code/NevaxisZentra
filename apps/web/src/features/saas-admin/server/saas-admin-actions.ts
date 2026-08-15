"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";

import { fetchMemberEffectiveAccess, mutateAdminAccess } from "./saas-admin-api-client";

function failureFromApi(
  result: Readonly<{ status: "failure"; message: string; code?: string; httpStatus?: number }>,
) {
  return {
    status: "failure" as const,
    message: result.message,
    code: result.code,
    httpStatus: result.httpStatus,
  };
}

export async function inviteSaasAdminUserAction(input: {
  email: string;
  intendedRoleReference?: string | null;
  idempotencyKey: string;
}) {
  const authorization = await denyUnlessGranted("admin-access.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await mutateAdminAccess("/api/admin-access/invitations", {
    email: input.email.trim(),
    intendedRoleReference: input.intendedRoleReference ?? null,
    idempotencyKey: input.idempotencyKey,
  });
  if (result.status === "failure") {
    return failureFromApi(result);
  }

  revalidatePath("/ayarlar");
  return { status: "success" as const, value: result.value };
}

export async function suspendSaasAdminMemberAction(input: {
  memberReference: string;
  expectedAuthorizationVersion: number;
  idempotencyKey: string;
}) {
  const authorization = await denyUnlessGranted("admin-access.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await mutateAdminAccess(
    `/api/admin-access/members/${encodeURIComponent(input.memberReference)}/suspend`,
    {
      expectedAuthorizationVersion: input.expectedAuthorizationVersion,
      idempotencyKey: input.idempotencyKey,
    },
  );
  if (result.status === "failure") {
    return failureFromApi(result);
  }

  revalidatePath("/ayarlar");
  return { status: "success" as const, value: result.value };
}

export async function reactivateSaasAdminMemberAction(input: {
  memberReference: string;
  expectedAuthorizationVersion: number;
  idempotencyKey: string;
}) {
  const authorization = await denyUnlessGranted("admin-access.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await mutateAdminAccess(
    `/api/admin-access/members/${encodeURIComponent(input.memberReference)}/reactivate`,
    {
      expectedAuthorizationVersion: input.expectedAuthorizationVersion,
      idempotencyKey: input.idempotencyKey,
    },
  );
  if (result.status === "failure") {
    return failureFromApi(result);
  }

  revalidatePath("/ayarlar");
  return { status: "success" as const, value: result.value };
}

export async function updateSaasAdminMemberRolesAction(input: {
  memberReference: string;
  roleReferences: readonly string[];
  expectedAuthorizationVersion: number;
  idempotencyKey: string;
}) {
  const authorization = await denyUnlessGranted("admin-access.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await mutateAdminAccess(
    `/api/admin-access/members/${encodeURIComponent(input.memberReference)}/roles`,
    {
      roleReferences: input.roleReferences,
      expectedAuthorizationVersion: input.expectedAuthorizationVersion,
      idempotencyKey: input.idempotencyKey,
    },
    "PUT",
  );
  if (result.status === "failure") {
    return failureFromApi(result);
  }

  revalidatePath("/ayarlar");
  return { status: "success" as const, value: result.value };
}

export async function updateSaasAdminMemberFacilitiesAction(input: {
  memberReference: string;
  facilityCodes: readonly string[];
  expectedAuthorizationVersion: number;
  idempotencyKey: string;
}) {
  const authorization = await denyUnlessGranted("admin-access.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await mutateAdminAccess(
    `/api/admin-access/members/${encodeURIComponent(input.memberReference)}/facilities`,
    {
      facilityCodes: input.facilityCodes,
      expectedAuthorizationVersion: input.expectedAuthorizationVersion,
      idempotencyKey: input.idempotencyKey,
    },
    "PUT",
  );
  if (result.status === "failure") {
    return failureFromApi(result);
  }

  revalidatePath("/ayarlar");
  return { status: "success" as const, value: result.value };
}

export async function updateSaasAdminRolePermissionsAction(input: {
  roleReference: string;
  grants: readonly Readonly<{ permissionKey: string; effect: string; recordScope?: string | null }>[];
  expectedVersion: number;
  idempotencyKey: string;
}) {
  const authorization = await denyUnlessGranted("admin-access.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await mutateAdminAccess(
    `/api/admin-access/roles/${encodeURIComponent(input.roleReference)}/permissions`,
    {
      grants: input.grants,
      expectedVersion: input.expectedVersion,
      idempotencyKey: input.idempotencyKey,
    },
  );
  if (result.status === "failure") {
    return failureFromApi(result);
  }

  revalidatePath("/ayarlar");
  return { status: "success" as const, value: result.value };
}

export async function revokeSaasAdminSessionAction(input: {
  sessionReference: string;
  idempotencyKey: string;
}) {
  const authorization = await denyUnlessGranted("admin-access.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await mutateAdminAccess(
    `/api/admin-access/sessions/${encodeURIComponent(input.sessionReference)}/revoke`,
    { idempotencyKey: input.idempotencyKey },
  );
  if (result.status === "failure") {
    return failureFromApi(result);
  }

  revalidatePath("/ayarlar");
  return { status: "success" as const, value: result.value };
}

export async function revokeAllSaasAdminMemberSessionsAction(input: {
  memberReference: string;
  idempotencyKey: string;
}) {
  const authorization = await denyUnlessGranted("admin-access.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await mutateAdminAccess(
    `/api/admin-access/members/${encodeURIComponent(input.memberReference)}/sessions/revoke-all`,
    { idempotencyKey: input.idempotencyKey },
  );
  if (result.status === "failure") {
    return failureFromApi(result);
  }

  revalidatePath("/ayarlar");
  return { status: "success" as const, value: result.value };
}

export async function loadSaasAdminEffectiveAccessAction(memberReference: string) {
  const authorization = await denyUnlessGranted("admin-access.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await fetchMemberEffectiveAccess(memberReference);
  if (result.status === "failure") {
    return failureFromApi(result);
  }

  return { status: "success" as const, value: result.value };
}
