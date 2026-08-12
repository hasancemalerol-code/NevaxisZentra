export const SHIFT_WORKFORCE_ACTIVE_FILTER_OPTIONS = [
  { value: "", label: "Tüm durumlar" },
  { value: "true", label: "Aktif" },
  { value: "false", label: "Pasif" },
] as const;

export const SHIFT_WORKFORCE_AVAILABILITY_FILTER_OPTIONS = [
  { value: "", label: "Tüm müsaitlik" },
  { value: "available", label: "Müsait" },
  { value: "leave", label: "İzinli" },
  { value: "absent", label: "Devamsız" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const SHIFT_WORKFORCE_FRESHNESS_FILTER_OPTIONS = [
  { value: "", label: "Tüm tazelik" },
  { value: "fresh", label: "Güncel" },
  { value: "stale", label: "Bayat" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const SHIFT_WORKFORCE_COMPETENCY_LEVEL_OPTIONS = [
  { value: "basic", label: "Temel" },
  { value: "competent", label: "Yetkin" },
  { value: "proficient", label: "İleri" },
  { value: "expert", label: "Uzman" },
] as const;

export const SHIFT_WORKFORCE_VERIFICATION_STATUS_OPTIONS = [
  { value: "verified", label: "Doğrulandı" },
  { value: "pending-verification", label: "Doğrulama bekliyor" },
  { value: "expired", label: "Süresi doldu" },
  { value: "rejected", label: "Reddedildi" },
] as const;

export const LOCAL_PILOT_SOURCE_SYSTEM = "local-development-pilot";
export const LOCAL_PILOT_DISCLAIMER = "Yerel pilot veri";

export function formatShiftWorkforceCount(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return value.toLocaleString("tr-TR");
}

export function formatShiftWorkforceDateTime(value?: string | null): string {
  if (!value) {
    return "—";
  }

  return new Date(value).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function getAvailabilityLabel(state: string, label?: string): string {
  if (label) {
    return label;
  }

  switch (state) {
    case "available":
      return "Müsait";
    case "leave":
      return "İzinli";
    case "absent":
      return "Devamsız";
    case "unknown":
      return "Bilinmiyor";
    default:
      return "Bilinmiyor";
  }
}

export function getFreshnessLabel(state: string, label?: string): string {
  if (label) {
    return label;
  }

  switch (state) {
    case "fresh":
      return "Güncel";
    case "stale":
      return "Bayat";
    case "unknown":
      return "Bilinmiyor";
    default:
      return "Bilinmiyor";
  }
}

export function getEmploymentStatusLabel(isActive: boolean): string {
  return isActive ? "Aktif" : "Pasif";
}

export function getSourceSystemLabel(sourceSystem: string, label?: string): string {
  if (label) {
    return label;
  }

  if (sourceSystem === LOCAL_PILOT_SOURCE_SYSTEM) {
    return LOCAL_PILOT_DISCLAIMER;
  }

  switch (sourceSystem) {
    case "logo":
      return "Logo ERP";
    case "mikro":
      return "Mikro ERP";
    case "netsis":
      return "Netsis ERP";
    default:
      return "Harici kaynak";
  }
}

export function getCompetencyLevelLabel(level: string, label?: string): string {
  if (label) {
    return label;
  }

  switch (level) {
    case "basic":
      return "Temel";
    case "competent":
      return "Yetkin";
    case "proficient":
      return "İleri";
    case "expert":
      return "Uzman";
    default:
      return "Bilinmiyor";
  }
}

export function getVerificationStatusLabel(status: string, label?: string): string {
  if (label) {
    return label;
  }

  switch (status) {
    case "verified":
      return "Doğrulandı";
    case "pending-verification":
      return "Doğrulama bekliyor";
    case "expired":
      return "Süresi doldu";
    case "rejected":
      return "Reddedildi";
    default:
      return "Bilinmiyor";
  }
}

export function getCompetencyCodeLabel(code: string, label?: string): string {
  if (label) {
    return label;
  }

  switch (code) {
    case "injection-machine-operation":
      return "Enjeksiyon makinesi operasyonu";
    case "mold-change-setup":
      return "Kalıp değişimi ve kurulum";
    case "process-parameter-review":
      return "Proses parametre inceleme";
    case "material-handling":
      return "Malzeme taşıma";
    case "robot-auxiliary-equipment":
      return "Robot ve yardımcı ekipman";
    case "quality-check":
      return "Kalite kontrol";
    case "maintenance-support":
      return "Bakım desteği";
    default:
      return "Yetkinlik";
  }
}

export function availabilityBadgeClass(state: string): string {
  switch (state) {
    case "available":
      return "shift-workforce-badge shift-workforce-badge--available";
    case "leave":
      return "shift-workforce-badge shift-workforce-badge--leave";
    case "absent":
      return "shift-workforce-badge shift-workforce-badge--absent";
    default:
      return "shift-workforce-badge shift-workforce-badge--unknown";
  }
}

export function freshnessBadgeClass(state: string): string {
  switch (state) {
    case "fresh":
      return "shift-workforce-badge shift-workforce-badge--fresh";
    case "stale":
      return "shift-workforce-badge shift-workforce-badge--stale";
    default:
      return "shift-workforce-badge shift-workforce-badge--unknown";
  }
}

export function employmentBadgeClass(isActive: boolean): string {
  return isActive
    ? "shift-workforce-badge shift-workforce-badge--active"
    : "shift-workforce-badge shift-workforce-badge--inactive";
}

export function verificationBadgeClass(status: string): string {
  switch (status) {
    case "verified":
      return "shift-workforce-badge shift-workforce-badge--verified";
    case "pending-verification":
      return "shift-workforce-badge shift-workforce-badge--pending";
    case "expired":
      return "shift-workforce-badge shift-workforce-badge--expired";
    default:
      return "shift-workforce-badge shift-workforce-badge--unknown";
  }
}
