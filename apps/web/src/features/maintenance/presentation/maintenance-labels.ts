import type {
  MaintenanceRequestSummary,
  MaintenanceWorkOrderSummary,
  PreventiveMaintenancePlanSummary,
} from "../domain/maintenance";

export function getMaintenanceStatusLabel(status: string): string {
  switch (status) {
    case "open":
      return "Açık";
    case "triaged":
      return "Sınıflandırıldı";
    case "converted-to-work-order":
      return "Emre Dönüştürüldü";
    case "closed":
      return "Kapatıldı";
    case "cancelled":
      return "İptal";
    case "draft":
      return "Taslak";
    case "planned":
      return "Planlandı";
    case "assigned":
      return "Atandı";
    case "in-progress":
      return "Devam Ediyor";
    case "waiting":
      return "Bekliyor";
    case "completed":
      return "Tamamlandı";
    case "active":
      return "Aktif";
    case "inactive":
      return "Pasif";
    default:
      return status;
  }
}

export function getMaintenancePriorityLabel(priority: string): string {
  switch (priority) {
    case "low":
      return "Düşük";
    case "normal":
      return "Normal";
    case "high":
      return "Yüksek";
    case "critical":
      return "Kritik";
    default:
      return priority;
  }
}

export function getMaintenanceAssetTypeLabel(assetType: string): string {
  switch (assetType) {
    case "machine":
      return "Makine";
    case "mold":
      return "Kalıp";
    case "auxiliary":
    case "auxiliary-equipment":
      return "Yardımcı Ekipman";
    case "tooling":
      return "Takım";
    case "facility":
      return "Tesis";
    default:
      return assetType;
  }
}

export function getMaintenanceTypeLabel(type: string): string {
  switch (type) {
    case "fault":
      return "Arıza";
    case "corrective":
      return "Düzeltici Bakım";
    case "preventive":
      return "Periyodik Bakım";
    case "inspection":
      return "Kontrol";
    case "other":
      return "Diğer";
    default:
      return type;
  }
}

export function getMaintenanceResolutionCodeLabel(code: string): string {
  switch (code) {
    case "repaired":
      return "Onarıldı";
    case "adjusted":
      return "Ayarlandı";
    case "cleaned":
      return "Temizlendi";
    case "replaced-part":
      return "Parça Değiştirildi";
    case "temporary-fix":
      return "Geçici Çözüm";
    case "no-fault-found":
      return "Arıza Bulunamadı";
    case "other":
      return "Diğer";
    default:
      return code;
  }
}

export function getMaintenanceIntervalLabel(intervalType: string, intervalValue: number): string {
  switch (intervalType) {
    case "days":
      return `${intervalValue} gün`;
    case "weeks":
      return `${intervalValue} hafta`;
    case "months":
      return `${intervalValue} ay`;
    default:
      return `${intervalValue} ${intervalType}`;
  }
}

export function formatMaintenanceAge(isoTimestamp: string): string {
  const timestamp = Date.parse(isoTimestamp);
  if (Number.isNaN(timestamp)) {
    return "—";
  }

  const diffMs = Date.now() - timestamp;
  const hours = Math.floor(diffMs / (1000 * 60 * 60));
  if (hours < 1) {
    return "Az önce";
  }

  if (hours < 24) {
    return `${hours} sa`;
  }

  const days = Math.floor(hours / 24);
  return `${days} gün`;
}

export function getMaintenancePriorityTone(
  priority: string,
): "neutral" | "information" | "danger" | "success" {
  switch (priority) {
    case "critical":
      return "danger";
    case "high":
      return "information";
    default:
      return "neutral";
  }
}

export function getMaintenanceWorkOrderStatusTone(
  status: string,
): "neutral" | "information" | "danger" | "success" {
  switch (status) {
    case "in-progress":
      return "information";
    case "waiting":
      return "information";
    case "completed":
      return "success";
    case "cancelled":
      return "neutral";
    default:
      return "neutral";
  }
}

const OPEN_REQUEST_STATUSES = new Set(["open", "triaged"]);

export function isOpenMaintenanceRequest(status: string): boolean {
  return OPEN_REQUEST_STATUSES.has(status);
}

const ACTIVE_WORK_ORDER_STATUSES = new Set([
  "draft",
  "planned",
  "assigned",
  "in-progress",
  "waiting",
]);

export function isActiveMaintenanceWorkOrder(status: string): boolean {
  return ACTIVE_WORK_ORDER_STATUSES.has(status);
}

export function isOverduePreventivePlan(plan: Pick<PreventiveMaintenancePlanSummary, "nextDueAtUtc" | "status">): boolean {
  if (plan.status !== "active" || !plan.nextDueAtUtc) {
    return false;
  }

  return Date.parse(plan.nextDueAtUtc) < Date.now();
}

export type MaintenanceCriticalAlert = Readonly<{
  id: string;
  kind: "critical-work-order" | "critical-request" | "overdue-preventive" | "waiting-work-order";
  title: string;
  reference: string;
  reason: string;
  statusLabel: string;
  href: string;
  tone: "danger" | "information" | "neutral";
}>;

export function buildMaintenanceCriticalAlerts(input: Readonly<{
  requests: readonly MaintenanceRequestSummary[];
  workOrders: readonly MaintenanceWorkOrderSummary[];
  preventivePlans: readonly PreventiveMaintenancePlanSummary[];
}>): readonly MaintenanceCriticalAlert[] {
  const alerts: MaintenanceCriticalAlert[] = [];

  for (const request of input.requests) {
    if (request.priority === "critical" && isOpenMaintenanceRequest(request.status)) {
      alerts.push({
        id: `req-${request.id}`,
        kind: "critical-request",
        title: "Kritik arıza kaydı",
        reference: request.requestNumber,
        reason: request.title,
        statusLabel: getMaintenancePriorityLabel(request.priority),
        href: `/bakim`,
        tone: "danger",
      });
    }
  }

  for (const workOrder of input.workOrders) {
    if (workOrder.priority === "critical" && isActiveMaintenanceWorkOrder(workOrder.status)) {
      alerts.push({
        id: `wo-${workOrder.id}`,
        kind: "critical-work-order",
        title: "Kritik bakım emri",
        reference: workOrder.workOrderNumber,
        reason: workOrder.assetName ?? workOrder.assetReference,
        statusLabel: getMaintenancePriorityLabel(workOrder.priority),
        href: `/bakim/emirler/${workOrder.id}`,
        tone: "danger",
      });
    }

    if (workOrder.status === "waiting") {
      alerts.push({
        id: `wait-${workOrder.id}`,
        kind: "waiting-work-order",
        title: "Bekleyen bakım emri",
        reference: workOrder.workOrderNumber,
        reason: workOrder.assetReference,
        statusLabel: getMaintenanceStatusLabel(workOrder.status),
        href: `/bakim/emirler/${workOrder.id}`,
        tone: "information",
      });
    }
  }

  for (const plan of input.preventivePlans) {
    if (isOverduePreventivePlan(plan)) {
      alerts.push({
        id: `pm-${plan.id}`,
        kind: "overdue-preventive",
        title: "Geciken periyodik bakım",
        reference: plan.code,
        reason: plan.name,
        statusLabel: plan.nextDueAtUtc
          ? new Date(plan.nextDueAtUtc).toLocaleDateString("tr-TR")
          : "—",
        href: `/bakim`,
        tone: "information",
      });
    }
  }

  return alerts
    .sort((a, b) => {
      const priority = (kind: MaintenanceCriticalAlert["kind"]) => {
        switch (kind) {
          case "critical-work-order":
          case "critical-request":
            return 0;
          case "overdue-preventive":
            return 1;
          default:
            return 2;
        }
      };

      return priority(a.kind) - priority(b.kind);
    })
    .slice(0, 8);
}
