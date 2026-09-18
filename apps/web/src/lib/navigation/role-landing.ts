import type { AdminAccessEffectiveAccess } from "@/features/admin-access/server/admin-access-effective-access";
import { hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";

export type RoleLandingProfile =
  | "plant-manager"
  | "production-manager"
  | "planner"
  | "maintenance"
  | "quality"
  | "shift-supervisor"
  | "operator"
  | "default";

export function resolveRoleLandingProfile(
  access: AdminAccessEffectiveAccess | null,
): RoleLandingProfile {
  if (!access) {
    return "default";
  }

  if (
    hasGrantedPermission(access, "factory-case.read")
    && hasGrantedPermission(access, "factory-intelligence.read")
  ) {
    return "plant-manager";
  }

  if (hasGrantedPermission(access, "production.planning.read")) {
    return "planner";
  }

  if (hasGrantedPermission(access, "maintenance.read")) {
    return "maintenance";
  }

  if (hasGrantedPermission(access, "quality.read")) {
    return "quality";
  }

  if (hasGrantedPermission(access, "shift-workforce.read")) {
    return "shift-supervisor";
  }

  if (
    hasGrantedPermission(access, "production.execution.read")
    || hasGrantedPermission(access, "mixed-production.read")
  ) {
    return "production-manager";
  }

  if (hasGrantedPermission(access, "ai-task.read") && !hasGrantedPermission(access, "factory-case.read")) {
    return "operator";
  }

  return "default";
}

export function resolveRoleLandingPath(access: AdminAccessEffectiveAccess | null): string {
  const profile = resolveRoleLandingProfile(access);

  switch (profile) {
    case "planner":
      return "/planlama";
    case "maintenance":
    case "production-manager":
      return "/fabrika";
    case "quality":
      return "/isler";
    default:
      return "/";
  }
}

export function resolveRoleLandingLabel(profile: RoleLandingProfile): string {
  switch (profile) {
    case "plant-manager":
      return "Yönetici odaklı bugün görünümü";
    case "production-manager":
      return "Üretim ve fabrika operasyonları";
    case "planner":
      return "Planlama çalışma alanı";
    case "maintenance":
      return "Varlık ve bakım bağlamı";
    case "quality":
      return "Kalite ve iş akışı";
    case "shift-supervisor":
      return "Vardiya operasyonları";
    case "operator":
      return "Atanan operasyonel işler";
    default:
      return "Operasyonel öncelikler";
  }
}
