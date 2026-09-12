import type { AiTaskRecommendation } from "../domain/ai-task";

export type AiTaskTargetSystem =
  NonNullable<AiTaskRecommendation["targetSystem"]>;

export const AI_TASK_TARGET_SYSTEM_LABELS: Readonly<
  Record<AiTaskTargetSystem, string>
> = {
  crevafactos: "FactOS",
  erp: "ERP",
  mes: "MES",
  maintenance: "Bakım Yönetimi",
  quality: "Kalite Yönetimi",
  notification: "Bildirim Servisi",
};

export const AI_TASK_TARGET_SYSTEM_DESCRIPTIONS: Readonly<
  Record<AiTaskTargetSystem, string>
> = {
  crevafactos:
    "İşlem FactOS içinde uygulanır veya takip edilir.",
  erp:
    "Onaylanan işlem bağlı ERP sistemine aktarılır.",
  mes:
    "İşlem üretim yürütme sistemine gönderilir.",
  maintenance:
    "Bakım talebi veya bakım kaydı oluşturulur.",
  quality:
    "Kalite kontrol veya uygunsuzluk kaydı oluşturulur.",
  notification:
    "İlgili kullanıcılara veya ekiplere bildirim gönderilir.",
};

export function getAiTaskTargetSystemLabel(
  targetSystem: AiTaskRecommendation["targetSystem"],
): string {
  if (!targetSystem) {
    return "FactOS";
  }

  return AI_TASK_TARGET_SYSTEM_LABELS[targetSystem];
}

export function getAiTaskTargetSystemDescription(
  targetSystem: AiTaskRecommendation["targetSystem"],
): string {
  if (!targetSystem) {
    return AI_TASK_TARGET_SYSTEM_DESCRIPTIONS.crevafactos;
  }

  return AI_TASK_TARGET_SYSTEM_DESCRIPTIONS[targetSystem];
}