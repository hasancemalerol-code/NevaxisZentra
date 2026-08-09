import type { ErpConnectionStatus, ErpSyncRunStatus } from "../domain/erp";

export function getErpProviderLabel(provider: string, displayName?: string): string {
  if (provider === "simulator") {
    return displayName ?? "ERP Simulator";
  }

  if (provider === "logo") {
    return "Logo (hazır değil)";
  }

  if (provider === "mikro") {
    return "Mikro (hazır değil)";
  }

  if (provider === "netsis") {
    return "Netsis (hazır değil)";
  }

  return provider;
}

export function getErpConnectionStatusLabel(status: ErpConnectionStatus | string): string {
  switch (status) {
    case "healthy":
      return "Sağlıklı";
    case "degraded":
      return "Sorunlu";
    case "unavailable":
      return "Kullanılamıyor";
    case "draft":
    case "configured":
      return "Yapılandırılmadı";
    case "disabled":
      return "Devre Dışı";
    default:
      return status;
  }
}

export function getErpConnectionStatusTone(
  status: ErpConnectionStatus | string,
): "ready" | "paused" | "critical" | "in-progress" {
  switch (status) {
    case "healthy":
      return "ready";
    case "degraded":
      return "paused";
    case "unavailable":
    case "disabled":
      return "critical";
    default:
      return "in-progress";
  }
}

export function getErpSyncStatusLabel(status: ErpSyncRunStatus | string): string {
  switch (status) {
    case "running":
      return "Çalışıyor";
    case "completed":
      return "Tamamlandı";
    case "completed-with-errors":
      return "Hatalarla tamamlandı";
    case "failed":
      return "Başarısız";
    default:
      return status;
  }
}

export function getErpEntityTypeLabel(entityType: string): string {
  if (entityType === "customers") {
    return "Müşteriler";
  }

  if (entityType === "products") {
    return "Ürünler";
  }

  if (entityType === "orders") {
    return "Siparişler";
  }

  if (entityType === "warehouses") {
    return "Depolar";
  }

  if (entityType === "inventory") {
    return "Stok";
  }

  if (entityType === "purchasing") {
    return "Satın Alma";
  }

  return entityType;
}

export function getErpMappingStatusLabel(status: string): string {
  switch (status) {
    case "unmapped":
      return "Eşleşmedi";
    case "suggested":
      return "Öneri Var";
    case "mapped":
      return "Eşleştirildi";
    case "conflict":
      return "Çakışma";
    case "ignored":
      return "Yoksayıldı";
    default:
      return status;
  }
}

export function getErpMappingStatusTone(
  status: string,
): "ready" | "paused" | "critical" | "in-progress" {
  switch (status) {
    case "mapped":
      return "ready";
    case "suggested":
      return "in-progress";
    case "conflict":
      return "critical";
    case "ignored":
      return "paused";
    default:
      return "paused";
  }
}

export function getErpOrderStatusLabel(status: string): string {
  switch (status) {
    case "open":
      return "Açık";
    case "confirmed":
      return "Onaylı";
    case "partially-fulfilled":
      return "Kısmi Karşılandı";
    case "fulfilled":
      return "Karşılandı";
    case "cancelled":
      return "İptal";
    default:
      return status;
  }
}

export function getErpReadinessBadgeLabel(ready: boolean, blockingCodes: readonly string[]): string {
  if (ready) {
    return "Operasyona Hazır";
  }

  if (blockingCodes.includes("mapping-conflict")) {
    return "Çakışma";
  }

  if (blockingCodes.includes("source-stale")) {
    return "Kaynak Güncel Değil";
  }

  return "Eksik Eşleştirme";
}

export function getErpSourceFreshnessLabel(freshness: string): string {
  switch (freshness) {
    case "fresh":
      return "Güncel";
    case "stale":
      return "Eski";
    default:
      return "Bilinmiyor";
  }
}

export function formatErpDateTime(iso?: string | null): string {
  if (!iso) {
    return "—";
  }

  return new Date(iso).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function getErpPurchaseOrderStatusLabel(status: string): string {
  switch (status) {
    case "open":
      return "Açık";
    case "partially-received":
      return "Kısmi Teslim";
    case "received":
      return "Teslim Alındı";
    case "cancelled":
      return "İptal";
    case "closed":
      return "Kapalı";
    default:
      return status;
  }
}

export function getErpShortageStatusLabel(status: string): string {
  switch (status) {
    case "sufficient":
      return "Yeterli";
    case "insufficient":
      return "Yetersiz";
    case "stale-source":
      return "Kaynak Eski";
    case "unknown":
      return "Bilinmiyor";
    default:
      return status;
  }
}

export function getErpShortageStatusTone(
  status: string,
): "ready" | "paused" | "critical" | "in-progress" {
  switch (status) {
    case "sufficient":
      return "ready";
    case "insufficient":
      return "critical";
    case "stale-source":
      return "paused";
    default:
      return "in-progress";
  }
}

export function getErpCoverageStatusLabel(status: string): string {
  switch (status) {
    case "covered":
      return "Karşılanıyor";
    case "partially-covered":
      return "Kısmi Karşılama";
    case "not-covered":
      return "Karşılanmıyor";
    case "unknown":
      return "Bilinmiyor";
    default:
      return status;
  }
}

export function formatErpQuantity(value: number | null | undefined, unit?: string | null): string {
  if (value == null) {
    return "—";
  }

  const formatted = value.toLocaleString("tr-TR", {
    maximumFractionDigits: 2,
  });

  return unit ? `${formatted} ${unit}` : formatted;
}
