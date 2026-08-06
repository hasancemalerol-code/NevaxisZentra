export const PRODUCTION_PLAN_FALLBACK_LABELS =
  Object.freeze({
    facility: "Tesis belirtilmedi",
    machine: "Makine atanmadı",
    mold: "Kalıp atanmadı",
    material: "Malzeme atanmadı",
    shift: "Vardiya belirtilmedi",
    startTime: "Başlangıç zamanı belirlenmedi",
    endTime: "Bitiş zamanı belirlenmedi",
    duration: "Süre hesaplanamadı",
    targetSystem: "Aktarım hedefi belirlenmedi",
    notes: "Plan açıklaması bulunmuyor.",
    sourceReference: "Kaynak referansı bulunmuyor",
    orderReference: "Sipariş belirtilmedi",
    productId: "Ürün kodu belirtilmedi",
    productName: "Ürün adı belirtilmedi",
    deliveryDate: "Termin belirtilmedi",
    auditDate: "Tarih bilgisi bulunmuyor",
  });

export type ProductionPlanFallbackLabelKey =
  keyof typeof PRODUCTION_PLAN_FALLBACK_LABELS;

export function getProductionPlanFallbackLabel(
  key: ProductionPlanFallbackLabelKey,
): string {
  return PRODUCTION_PLAN_FALLBACK_LABELS[key];
}

export function getProductionPlanDisplayValue(
  value: string | null | undefined,
  fallbackKey: ProductionPlanFallbackLabelKey,
): string {
  const normalizedValue = value?.trim();

  return (
    normalizedValue ||
    getProductionPlanFallbackLabel(
      fallbackKey,
    )
  );
}