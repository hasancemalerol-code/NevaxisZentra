import type { OrderDraftTransfer } from "../domain/order-draft";

export type OrderTargetSystem = NonNullable<
  OrderDraftTransfer["targetSystem"]
>;

export const ORDER_TARGET_SYSTEM_LABELS = {
  erp: "ERP",
  mes: "MES",
  crevafactos: "NEVAXIS FACTOS",
} satisfies Record<OrderTargetSystem, string>;

export const ORDER_TARGET_SYSTEM_DESCRIPTIONS = {
  erp:
    "Yetkili onayından geçen sipariş bağlı ERP sistemine aktarılır.",
  mes:
    "Siparişe bağlı üretim talebi veya üretim emri bağlı MES sistemine iletilir.",
  crevafactos:
    "Sipariş NEVAXIS FACTOS içinde takip edilir ve bağlı operasyon akışlarına yönlendirilir.",
} satisfies Record<OrderTargetSystem, string>;

export function getOrderTargetSystemLabel(
  targetSystem: OrderDraftTransfer["targetSystem"],
): string {
  if (!targetSystem) {
    return "Hedef sistem tanımlanmadı";
  }

  return ORDER_TARGET_SYSTEM_LABELS[targetSystem];
}

export function getOrderTargetSystemDescription(
  targetSystem: OrderDraftTransfer["targetSystem"],
): string {
  if (!targetSystem) {
    return "Sipariş aktarımı için henüz bir hedef sistem belirlenmedi.";
  }

  return ORDER_TARGET_SYSTEM_DESCRIPTIONS[targetSystem];
}