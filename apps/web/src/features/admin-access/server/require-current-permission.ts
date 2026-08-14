import { forbidden } from "next/navigation";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "./admin-access-effective-access";

export async function forbidUnlessGranted(
  permissionKey: string,
): Promise<void> {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, permissionKey)) {
    forbidden();
  }
}

export async function denyUnlessGranted(
  permissionKey: string,
): Promise<
  | Readonly<{ status: "granted" }>
  | Readonly<{ status: "failure"; message: string }>
> {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, permissionKey)) {
    return { status: "failure", message: "Authorization denied." };
  }

  return { status: "granted" };
}

export async function denyUnlessAnyGranted(
  permissionKeys: readonly string[],
): Promise<
  | Readonly<{ status: "granted" }>
  | Readonly<{ status: "failure"; message: string }>
> {
  const access = await fetchCurrentEffectiveAccess();
  if (!permissionKeys.some((key) => hasGrantedPermission(access, key))) {
    return { status: "failure", message: "Authorization denied." };
  }

  return { status: "granted" };
}
