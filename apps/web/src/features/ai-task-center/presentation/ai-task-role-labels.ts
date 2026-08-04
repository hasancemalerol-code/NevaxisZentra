import type { AiTaskActorRole } from "../domain/ai-task-policy";

export const AI_TASK_ACTOR_ROLE_LABELS: Readonly<
  Record<AiTaskActorRole, string>
> = {
  operator: "Operatör",
  supervisor: "Vardiya Sorumlusu",
  "department-manager": "Bölüm Yöneticisi",
  "plant-manager": "Fabrika Yöneticisi",
  "system-administrator": "Sistem Yöneticisi",
};

export const AI_TASK_ACTOR_ROLE_DESCRIPTIONS: Readonly<
  Record<AiTaskActorRole, string>
> = {
  operator:
    "Atanmış görevleri inceleyebilir ve yetkisi kapsamındaki operasyonel adımları uygulayabilir.",
  supervisor:
    "Vardiya ve operasyon görevlerini inceleyebilir, standart görevleri onaylayabilir.",
  "department-manager":
    "Bölüm kapsamındaki yüksek etkili ve kritik görevleri değerlendirebilir.",
  "plant-manager":
    "Fabrika genelindeki kritik kararları ve operasyonel etkileri onaylayabilir.",
  "system-administrator":
    "Sistem, entegrasyon ve teknik operasyon görevlerini yönetebilir.",
};

export function isAiTaskActorRole(
  value: string | undefined,
): value is AiTaskActorRole {
  return (
    value === "operator" ||
    value === "supervisor" ||
    value === "department-manager" ||
    value === "plant-manager" ||
    value === "system-administrator"
  );
}

export function getAiTaskActorRoleLabel(
  role: string | undefined,
): string {
  if (!role) {
    return "Rol tanımlanmadı";
  }

  if (!isAiTaskActorRole(role)) {
    return role;
  }

  return AI_TASK_ACTOR_ROLE_LABELS[role];
}

export function getAiTaskActorRoleDescription(
  role: string | undefined,
): string {
  if (!role) {
    return "Kullanıcı rolü henüz tanımlanmadı.";
  }

  if (!isAiTaskActorRole(role)) {
    return "Bu rol için açıklama bulunmuyor.";
  }

  return AI_TASK_ACTOR_ROLE_DESCRIPTIONS[role];
}