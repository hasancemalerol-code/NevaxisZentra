import type { AiTaskSourceReference } from "../domain/ai-task";

export type AiTaskSourceType =
  AiTaskSourceReference["sourceType"];

export const AI_TASK_SOURCE_TYPE_LABELS: Readonly<
  Record<AiTaskSourceType, string>
> = {
  erp: "ERP",
  machine: "Makine",
  iot: "IoT Sensörü",
  quality: "Kalite",
  maintenance: "Bakım",
  energy: "Enerji",
  manual: "Manuel Kayıt",
  calculated: "Hesaplanan Veri",
};

export const AI_TASK_SOURCE_TYPE_DESCRIPTIONS: Readonly<
  Record<AiTaskSourceType, string>
> = {
  erp: "ERP sisteminden alınan operasyonel veya ticari veri",
  machine: "Makine, PLC veya üretim ekipmanı tarafından üretilen veri",
  iot: "Bağlı sensör veya IoT cihazından alınan ölçüm",
  quality: "Kalite kontrol, ölçüm veya uygunsuzluk kaydı",
  maintenance: "Bakım geçmişi, arıza veya servis kaydı",
  energy: "Enerji tüketimi veya enerji izleme verisi",
  manual: "Kullanıcı tarafından girilen doğrulanmış kayıt",
  calculated: "CrevaFactOS tarafından diğer kaynaklardan hesaplanan sonuç",
};

export function getAiTaskSourceTypeLabel(
  sourceType: AiTaskSourceType,
): string {
  return AI_TASK_SOURCE_TYPE_LABELS[sourceType];
}

export function getAiTaskSourceTypeDescription(
  sourceType: AiTaskSourceType,
): string {
  return AI_TASK_SOURCE_TYPE_DESCRIPTIONS[sourceType];
}