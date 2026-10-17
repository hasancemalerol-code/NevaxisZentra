import type {
  MaintenanceRequestSummary,
  MaintenanceWorkOrderSummary,
  PreventiveMaintenanceDueSummary,
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

export function getMaintenanceWorkNoteTypeLabel(noteType: string): string {
  switch (noteType) {
    case "diagnosis":
      return "Teşhis";
    case "action":
      return "İşlem";
    case "observation":
      return "Gözlem";
    case "handover":
      return "Devir";
    case "general":
      return "Genel";
    default:
      return noteType;
  }
}

export function getPreventiveDueStatusLabel(status: string): string {
  switch (status) {
    case "due":
      return "Vadesi Geldi";
    case "work-order-created":
      return "Emir Oluşturuldu";
    case "completed":
      return "Tamamlandı";
    case "skipped":
      return "Atlandı";
    case "cancelled":
      return "İptal";
    default:
      return status;
  }
}

export function getPreventiveDueStatusTone(
  status: string,
): "neutral" | "information" | "danger" | "success" {
  switch (status) {
    case "due":
      return "information";
    case "work-order-created":
      return "information";
    case "completed":
      return "success";
    case "cancelled":
    case "skipped":
      return "neutral";
    default:
      return "neutral";
  }
}

export function isOpenPreventiveDue(status: string): boolean {
  return status === "due" || status === "work-order-created";
}

export function isOverduePreventiveDue(
  due: Pick<PreventiveMaintenanceDueSummary, "dueAtUtc" | "status">,
): boolean {
  if (due.status !== "due") {
    return false;
  }

  return Date.parse(due.dueAtUtc) < Date.now();
}

export function isUpcomingPreventiveDue(
  due: Pick<PreventiveMaintenanceDueSummary, "dueAtUtc" | "status">,
): boolean {
  if (due.status !== "due") {
    return false;
  }

  return Date.parse(due.dueAtUtc) >= Date.now();
}

export function formatMaintenanceCurrency(value?: number | null): string {
  if (value === undefined || value === null) {
    return "—";
  }

  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: "TRY",
    maximumFractionDigits: 2,
  }).format(value);
}

export function formatMaintenanceDate(iso?: string): string {
  if (!iso) {
    return "—";
  }

  return new Date(iso).toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

export function formatMaintenanceDateTime(iso?: string): string {
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
): "neutral" | "information" | "danger" | "success" | "warning" {
  switch (priority) {
    case "critical":
      return "danger";
    case "high":
      return "warning";
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

/**
 * Caption for an empty mean-repair-time (MTTR) value. MTTR only uses COMPLETED fault/corrective work orders that
 * have both start and completion times, so "no corrective maintenance" is only true when there is no completed one.
 */
export function describeMeanRepairUnavailable(
  workOrders: ReadonlyArray<Readonly<{ type: string; status: string }>>,
): string {
  const hasCompletedCorrective = workOrders.some(
    (item) => (item.type === "fault" || item.type === "corrective") && item.status === "completed",
  );

  return hasCompletedCorrective
    ? "Başlangıç ve bitiş zamanı kayıtlı onarım yok"
    : "Tamamlanmış düzeltici bakım yok";
}

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
        href: `/bakim/arizalar/${request.id}`,
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
        href: `/bakim/planlama#gecikenler`,
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

export function getMaintenanceConditionStatusLabel(status: string): string {
  switch (status) {
    case "normal":
      return "Normal";
    case "attention":
      return "Dikkat";
    case "warning":
      return "Uyarı";
    case "critical":
      return "Kritik";
    case "unknown":
      return "Veri Yetersiz";
    default:
      return status;
  }
}

export function getMaintenanceConditionStatusTone(
  status: string,
): "neutral" | "information" | "success" | "warning" | "danger" {
  switch (status) {
    case "normal":
      return "success";
    case "attention":
      return "information";
    case "warning":
      return "warning";
    case "critical":
      return "danger";
    default:
      return "neutral";
  }
}

export function getMaintenanceFailureCategoryLabel(category: string): string {
  switch (category) {
    case "mechanical":
      return "Mekanik";
    case "electrical":
      return "Elektrik";
    case "hydraulic":
      return "Hidrolik";
    case "pneumatic":
      return "Pnömatik";
    case "cooling":
      return "Soğutma";
    case "heating":
      return "Isıtma";
    case "control":
      return "Kontrol";
    case "mold":
      return "Kalıp";
    case "robot":
      return "Robot";
    case "auxiliary":
      return "Yardımcı";
    default:
      return "Diğer";
  }
}

const CONDITION_REASON_LABELS: Readonly<Record<string, string>> = {
  "critical open maintenance item": "Kritik açık bakım kaydı var",
  "repeat failure threshold reached": "Tekrarlayan arıza eşiğine ulaşıldı",
  "preventive maintenance heavily overdue": "Periyodik bakım ciddi şekilde gecikmiş",
  "recent fault recorded": "Yakın zamanda arıza kaydedildi",
  "overdue preventive maintenance": "Periyodik bakım vadesi geçmiş",
  "no active deterministic maintenance risk": "Etkin bakım riski saptanmadı",
  "insufficient maintenance history": "Bakım geçmişi yetersiz",
};

/** Turkish text for a backend-generated condition reason; unknown English-looking codes fall back to a neutral phrase. */
export function getMaintenanceConditionReasonLabel(reason: string): string {
  const known = CONDITION_REASON_LABELS[reason.trim().toLowerCase()];
  if (known) {
    return known;
  }

  return /^[\x20-\x7e]+$/.test(reason) && !/[ğüşıöçĞÜŞİÖÇ]/.test(reason) && /^[a-z0-9 ._-]+$/i.test(reason)
    ? "Durum değerlendirmesinde ek bir koşul saptandı"
    : reason;
}

const EVENT_ENTITY_LABELS: Readonly<Record<string, string>> = {
  request: "Arıza kaydı",
  workorder: "Bakım emri",
  plan: "Periyodik bakım planı",
  due: "Periyodik bakım vadesi",
  note: "Bakım notu",
  part: "Parça kullanımı",
};

const EVENT_VERB_LABELS: Readonly<Record<string, string>> = {
  created: "oluşturuldu",
  generated: "oluşturuldu",
  updated: "güncellendi",
  assigned: "atandı",
  started: "başlatıldı",
  completed: "tamamlandı",
  cancelled: "iptal edildi",
  canceled: "iptal edildi",
  closed: "kapatıldı",
  triaged: "sınıflandırıldı",
  converted: "bakım emrine dönüştürüldü",
  waiting: "beklemeye alındı",
  resumed: "yeniden başlatıldı",
  added: "eklendi",
  recorded: "kaydedildi",
  skipped: "atlandı",
};

function eventTokens(text: string): string[] {
  return text
    .toLowerCase()
    .replace(/work[\s._-]*order/g, "workorder")
    .split(/[^a-z0-9]+/)
    .filter((token) => token && token !== "maintenance");
}

/**
 * Turkish sentence for a maintenance activity / timeline event ("maintenance.workorder.created" or
 * "Maintenance workorder created" -> "Bakım emri oluşturuldu"). Returns null for a technical code that cannot be
 * translated, so callers hide it instead of printing it. Text that is already readable passes through unchanged.
 */
export function localizeMaintenanceEventText(text: string | null | undefined): string | null {
  const value = text?.trim();
  if (!value) {
    return null;
  }

  const tokens = eventTokens(value);
  const entity = tokens.find((token) => EVENT_ENTITY_LABELS[token]);
  const verb = [...tokens].reverse().find((token) => EVENT_VERB_LABELS[token]);
  if (entity && verb) {
    return `${EVENT_ENTITY_LABELS[entity]} ${EVENT_VERB_LABELS[verb]}`;
  }

  if (/^[a-z0-9]+(?:[._-][a-z0-9]+)+$/i.test(value)) {
    return null;
  }

  if (/^[A-Za-z0-9 ,.'-]+$/.test(value) && !/[ğüşıöçĞÜŞİÖÇ]/.test(value) && /\b(?:the|was|has|is|created|updated|maintenance|work|request|order)\b/i.test(value)) {
    return null;
  }

  return value;
}

/** Event label and optional human detail for one activity entry; the technical code is never returned. */
export function describeMaintenanceActivity(entry: Readonly<{ eventType?: string; summary?: string }>): Readonly<{
  label: string;
  detail: string | null;
}> {
  const label =
    localizeMaintenanceEventText(entry.eventType) ?? localizeMaintenanceEventText(entry.summary) ?? "Bakım olayı";
  const detail = localizeMaintenanceEventText(entry.summary);
  return { label, detail: detail && detail !== label ? detail : null };
}
