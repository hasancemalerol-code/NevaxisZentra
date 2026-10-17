import { formatDateTime } from "@/lib/presentation/format";
import type { ErpConnectionStatus, ErpSyncRunStatus } from "../domain/erp";

const LOGO_CAPABILITIES = ["customers.read", "products.read"] as const;
const MIKRO_CAPABILITIES = ["customers.read", "products.read"] as const;
const NETSIS_CAPABILITIES = ["customers.read", "products.read"] as const;

export function getErpProviderLabel(provider: string, displayName?: string): string {
  if (provider === "simulator") {
    return displayName ?? "Örnek ERP";
  }

  if (provider === "logo") {
    return displayName ?? "Logo";
  }

  if (provider === "mikro") {
    return displayName ?? "Mikro";
  }

  if (provider === "netsis") {
    return displayName ?? "Netsis";
  }

  return provider;
}

export function getErpLogoCapabilities(): readonly string[] {
  return LOGO_CAPABILITIES;
}

export function getErpMikroCapabilities(): readonly string[] {
  return MIKRO_CAPABILITIES;
}

export function getErpNetsisCapabilities(): readonly string[] {
  return NETSIS_CAPABILITIES;
}

export function getErpNetsisConnectionStateLabel(input: {
  status: ErpConnectionStatus | string;
  hasCredentialReference: boolean;
  connectionMode: string;
  lastErrorCode?: string | null;
}): string {
  if (input.lastErrorCode === "netsis.authentication.failed" || (!input.hasCredentialReference && input.connectionMode === "api")) {
    return "Kimlik Bilgisi Gerekli";
  }

  if (input.status === "healthy") {
    return "Sağlıklı";
  }

  if (input.status === "configured" || input.status === "draft") {
    return input.hasCredentialReference ? "Kurulum Bekliyor" : "Kimlik Bilgisi Gerekli";
  }

  if (input.status === "degraded") {
    return "Bağlantı Doğrulandı";
  }

  if (input.status === "unavailable") {
    return "Bağlantı Kurulamadı";
  }

  return getErpConnectionStatusLabel(input.status);
}

export function getErpNetsisAdapterReadyLabel(): string {
  return "Bağlantı hazır";
}

export function getErpNetsisLiveValidatedLabel(isLiveValidated: boolean): string | null {
  return isLiveValidated ? "Gerçek Netsis Bağlantısı Doğrulandı" : null;
}

export function getErpNetsisTransportLabel(connectionMode: string, hasTestFixtureCredential: boolean): string {
  if (hasTestFixtureCredential) {
    return "Test Transport";
  }

  if (connectionMode === "api") {
    return "Netsis Bridge (HTTP)";
  }

  return "Yapılandırılmadı";
}

export function getErpNetsisSetupGuidance(): string {
  return "Gerçek Netsis bağlantısı için Netsis erişimi bulunan entegrasyon bilgisayarında Zentra Netsis Köprüsü yapılandırılmalıdır.";
}

export function getErpMikroConnectionStateLabel(input: {
  status: ErpConnectionStatus | string;
  hasCredentialReference: boolean;
  connectionMode: string;
  lastErrorCode?: string | null;
}): string {
  if (input.lastErrorCode === "mikro.authentication.failed" || (!input.hasCredentialReference && input.connectionMode === "api")) {
    return "Kimlik Bilgisi Gerekli";
  }

  if (input.status === "healthy") {
    return "Sağlıklı";
  }

  if (input.status === "configured" || input.status === "draft") {
    return input.hasCredentialReference ? "Kurulum Bekliyor" : "Kimlik Bilgisi Gerekli";
  }

  if (input.status === "degraded") {
    return "Bağlantı Doğrulandı";
  }

  if (input.status === "unavailable") {
    return "Bağlantı Kurulamadı";
  }

  return getErpConnectionStatusLabel(input.status);
}

export function getErpMikroAdapterReadyLabel(): string {
  return "Bağlantı hazır";
}

export function getErpMikroLiveValidatedLabel(isLiveValidated: boolean): string | null {
  return isLiveValidated ? "Gerçek Mikro Bağlantısı Doğrulandı" : null;
}

export function getErpMikroTransportLabel(connectionMode: string, hasTestFixtureCredential: boolean): string {
  if (hasTestFixtureCredential) {
    return "Test Transport";
  }

  if (connectionMode === "api") {
    return "Mikro Bridge (HTTP)";
  }

  return "Yapılandırılmadı";
}

export function getErpMikroSetupGuidance(): string {
  return "Gerçek Mikro bağlantısı için Mikro erişimi bulunan entegrasyon bilgisayarında Zentra Mikro Köprüsü yapılandırılmalıdır.";
}

export function getErpLogoConnectionStateLabel(input: {
  status: ErpConnectionStatus | string;
  hasCredentialReference: boolean;
  connectionMode: string;
  lastErrorCode?: string | null;
}): string {
  if (input.lastErrorCode === "logo.authentication.failed" || (!input.hasCredentialReference && input.connectionMode === "api")) {
    return "Kimlik Bilgisi Gerekli";
  }

  if (input.status === "healthy") {
    return "Sağlıklı";
  }

  if (input.status === "configured" || input.status === "draft") {
    return input.hasCredentialReference ? "Kurulum Bekliyor" : "Kimlik Bilgisi Gerekli";
  }

  if (input.status === "degraded") {
    return "Bağlantı Doğrulandı";
  }

  if (input.status === "unavailable") {
    return "Bağlantı Kurulamadı";
  }

  return getErpConnectionStatusLabel(input.status);
}

export function getErpLogoAdapterReadyLabel(): string {
  return "Bağlantı hazır";
}

export function getErpLogoLiveValidatedLabel(isLiveValidated: boolean): string | null {
  return isLiveValidated ? "Gerçek Logo Bağlantısı Doğrulandı" : null;
}

export function getErpLogoTransportLabel(connectionMode: string, hasTestFixtureCredential: boolean): string {
  if (hasTestFixtureCredential) {
    return "Test Transport";
  }

  if (connectionMode === "api") {
    return "Logo Bridge (HTTP)";
  }

  return "Yapılandırılmadı";
}

export function getErpLogoSetupGuidance(): string {
  return "Gerçek Logo bağlantısı için Logo erişimi bulunan entegrasyon bilgisayarında Zentra Logo Köprüsü yapılandırılmalıdır.";
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

  if (entityType === "production") {
    return "Üretim";
  }

  if (entityType === "documents") {
    return "Belgeler";
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
      return "Güncel Değil";
    default:
      return "Bilinmiyor";
  }
}

export function getErpSourceFreshnessTone(
  freshness: string,
): "ready" | "paused" | "critical" | "in-progress" {
  switch (freshness) {
    case "fresh":
      return "ready";
    case "stale":
      return "paused";
    default:
      return "in-progress";
  }
}

export function formatErpDateTime(iso?: string | null): string {
  return formatDateTime(iso);
}

export function formatErpKpiTimestamp(iso?: string | null): { date: string; time: string } {
  if (!iso) {
    return { date: "—", time: "" };
  }

  const date = new Date(iso);
  return {
    date: date.toLocaleDateString("tr-TR", {
      day: "numeric",
      month: "short",
      year: "numeric",
    }),
    time: date.toLocaleTimeString("tr-TR", {
      hour: "2-digit",
      minute: "2-digit",
    }),
  };
}

export function formatErpDate(iso?: string | null): string {
  if (!iso) {
    return "—";
  }

  return new Date(iso).toLocaleDateString("tr-TR", {
    day: "numeric",
    month: "short",
    year: "numeric",
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

export function getErpProductionOrderStatusLabel(status: string): string {
  switch (status) {
    case "planned":
      return "Planlandı";
    case "released":
      return "Serbest Bırakıldı";
    case "in-progress":
      return "Devam Ediyor";
    case "completed":
      return "Tamamlandı";
    case "cancelled":
      return "İptal";
    case "closed":
      return "Kapalı";
    default:
      return status;
  }
}

export function getErpProductionOrderStatusTone(
  status: string,
): "ready" | "paused" | "critical" | "in-progress" {
  switch (status) {
    case "completed":
    case "closed":
      return "ready";
    case "in-progress":
    case "released":
      return "in-progress";
    case "cancelled":
      return "critical";
    default:
      return "paused";
  }
}

export function getErpDocumentTypeLabel(documentType: string): string {
  switch (documentType) {
    case "sales-order":
      return "Satış Siparişi";
    case "purchase-order":
      return "Satın Alma";
    case "delivery-note":
      return "İrsaliye";
    case "invoice":
      return "Fatura";
    case "production-order":
      return "Üretim Emri";
    case "quality-document":
      return "Kalite Belgesi";
    case "generic":
      return "Genel";
    default:
      return documentType;
  }
}

export function getErpWritebackStatusLabel(status: string): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "pending-approval":
      return "Onay Bekliyor";
    case "approved":
      return "Onaylandı";
    case "rejected":
      return "Reddedildi";
    case "executing":
      return "Uygulanıyor";
    case "succeeded":
      return "Başarılı";
    case "failed":
      return "Başarısız";
    case "blocked":
      return "Engellendi";
    case "superseded":
      return "Geçersiz Kılındı";
    default:
      return status;
  }
}

export function getErpWritebackStatusTone(
  status: string,
): "ready" | "paused" | "critical" | "in-progress" {
  switch (status) {
    case "succeeded":
    case "approved":
      return "ready";
    case "pending-approval":
    case "executing":
      return "in-progress";
    case "failed":
    case "rejected":
    case "blocked":
      return "critical";
    default:
      return "paused";
  }
}

export function getErpWritebackCommandTypeLabel(commandType: string): string {
  switch (commandType) {
    case "sales-order.create-draft":
      return "Satış Siparişi Taslağı";
    default:
      return commandType;
  }
}

export function getErpWritebackDecisionLabel(decision: string): string {
  switch (decision) {
    case "approved":
      return "Onaylandı";
    case "rejected":
      return "Reddedildi";
    default:
      return decision;
  }
}
