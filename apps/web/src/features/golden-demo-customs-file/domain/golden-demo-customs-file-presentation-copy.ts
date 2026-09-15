import { formatCustomsNumber, getCustomsDocumentLabel } from "./golden-demo-customs-file-labels";

import type { GoldenDemoCustomsFileWorkspaceApi } from "../server/golden-demo-customs-file-api-client";

export type CustomsWorkflowPhase = "tespit" | "karar" | "aksiyon" | "dogrulama";

const STATUS_ORDER = [
  "outcome-assigned",
  "context-loaded",
  "documents-prepared",
  "blocker-detected",
  "corrective-work-created",
  "corrective-work-completed",
  "evidence-rechecked",
  "approval-pending",
  "approved",
  "package-assembled",
  "verified-ready",
] as const;

export function getCustomsDemoStep(status: string): Readonly<{ stepNumber: number; totalSteps: number }> {
  const index = STATUS_ORDER.indexOf(status as (typeof STATUS_ORDER)[number]);
  return {
    stepNumber: index >= 0 ? index + 1 : 1,
    totalSteps: STATUS_ORDER.length,
  };
}

export function getCustomsStatusChipLabel(workspace: GoldenDemoCustomsFileWorkspaceApi): string {
  if (workspace.hero.showSuccessHero || workspace.status === "verified-ready") {
    return "Doğrulandı";
  }
  if (workspace.package && workspace.approval.isApproved && workspace.hero.blockingIssueCount === 0) {
    return "Müşavire Hazır";
  }
  if (workspace.approval.isApprovalRequired && !workspace.approval.isApproved) {
    return "Onay Bekliyor";
  }
  if (workspace.hero.blockingIssueCount > 0) {
    return "Eksik Belge Var";
  }
  return translateCustomsApiText(workspace.statusLabel);
}

export function getCustomsPrimaryOutcomeLabel(workspace: GoldenDemoCustomsFileWorkspaceApi): string {
  if (workspace.hero.showSuccessHero || workspace.status === "verified-ready") {
    return "Doğrulandı";
  }
  if (workspace.package && workspace.approval.isApproved && workspace.hero.brokerHandoffStatus === "READY") {
    return "Müşavire Hazır";
  }
  if (workspace.approval.isApprovalRequired && !workspace.approval.isApproved) {
    return "Onay Bekliyor";
  }
  if (workspace.hero.blockingIssueCount > 0) {
    return "Eksik Belge Var";
  }
  return "Hazırlanıyor";
}

export function getCustomsSupportingCopy(workspace: GoldenDemoCustomsFileWorkspaceApi): string {
  if (workspace.hero.subheadline?.trim()) {
    return translateCustomsApiText(workspace.hero.subheadline);
  }
  return translateCustomsApiText(workspace.assignedOutcomeText);
}

export function getCustomsWorkflowPhase(status: string): CustomsWorkflowPhase {
  switch (status) {
    case "verified-ready":
      return "dogrulama";
    case "package-assembled":
    case "approved":
    case "approval-pending":
    case "corrective-work-completed":
    case "evidence-rechecked":
      return "aksiyon";
    case "blocker-detected":
    case "corrective-work-created":
      return "karar";
    default:
      return "tespit";
  }
}

export function workflowStepClass(
  activePhase: CustomsWorkflowPhase,
  step: CustomsWorkflowPhase,
  status: string,
): string {
  const order: CustomsWorkflowPhase[] = ["tespit", "karar", "aksiyon", "dogrulama"];
  const stepIndex = order.indexOf(step);
  const activeIndex = order.indexOf(activePhase);

  if (stepIndex < activeIndex || (step === "dogrulama" && status === "verified-ready")) {
    return "golden-demo-workflow-rail__step golden-demo-workflow-rail__step--complete";
  }

  if (step === activePhase) {
    return "golden-demo-workflow-rail__step golden-demo-workflow-rail__step--active";
  }

  if (step === "karar" && activePhase === "tespit") {
    return "golden-demo-workflow-rail__step golden-demo-workflow-rail__step--active";
  }

  return "golden-demo-workflow-rail__step";
}

export function getCustomsDocumentStatusPresentation(status: string): Readonly<{ icon: string; label: string }> {
  switch (status.toLowerCase()) {
    case "ready":
      return { icon: "✓", label: "Hazır" };
    case "invalid":
      return { icon: "!", label: "İnceleme Gerekli" };
    case "draft":
    case "superseded":
      return { icon: "⏳", label: "Bekliyor" };
    default:
      return { icon: "⏳", label: translateCustomsApiText(status) };
  }
}

export function getCustomsRequirementStatusPresentation(
  status: string,
  blocking: boolean,
): Readonly<{ icon: string; label: string }> {
  switch (status.toLowerCase()) {
    case "pass":
      return { icon: "✓", label: "Doğrulandı" };
    case "fail":
      return { icon: "!", label: blocking ? "İnceleme Gerekli" : "Eksik" };
    case "not-required":
      return { icon: "✓", label: "Gerekmez" };
    default:
      return { icon: "⏳", label: "Bekliyor" };
  }
}

export function getCustomsBlockerTitle(blockerCode: string): string {
  switch (blockerCode) {
    case "packing-list-net-weight-mismatch":
      return "Çeki listesi net ağırlık tutarsızlığı";
    default:
      return translateCustomsApiText(blockerCode.replace(/-/g, " "));
  }
}

export function buildCustomsEvidenceSources(workspace: GoldenDemoCustomsFileWorkspaceApi): readonly string[] {
  const sources = new Set<string>();

  if (workspace.orderNumber) {
    sources.add("Sipariş Verisi");
  }
  if (workspace.shipmentReference) {
    sources.add("Sevkiyat Verisi");
  }
  if (workspace.documents.some((doc) => doc.documentType === "commercial-invoice" && doc.isCurrent)) {
    sources.add("Ticari Fatura");
  }
  if (workspace.documents.some((doc) => doc.documentType === "packing-list" && doc.isCurrent)) {
    sources.add("Çeki Listesi");
  }
  if (workspace.productLines.some((line) => line.gtipCode)) {
    sources.add("GTİP Kontrolü");
  }
  if (workspace.consistencyChecks.some((check) => check.checkCode.includes("weight"))) {
    sources.add("Ağırlık Kontrolü");
  }
  if (workspace.customerName || workspace.destinationCountry) {
    sources.add("Müşteri / Ülke Bilgisi");
  }
  if (
    workspace.documents.some(
      (doc) =>
        doc.isCurrent &&
        (doc.documentType === "atr-status" ||
          doc.documentType === "eur1-status" ||
          doc.documentType === "certificate-of-origin-status"),
    )
  ) {
    sources.add("Belge Doğrulaması");
  }

  for (const requirement of workspace.requirements) {
    if (requirement.status === "pass") {
      sources.add(translateCustomsApiText(requirement.label));
    }
  }

  return Array.from(sources);
}

export function formatCustomsApprovalTimestamp(value?: string | null): string {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  const day = date.toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
  const time = date.toLocaleTimeString("tr-TR", {
    hour: "2-digit",
    minute: "2-digit",
  });

  return `${day} · ${time}`;
}

export function formatCustomsApprovalAuthority(role: string): string {
  return translateCustomsApiText(role)
    .split("/")
    .map((part) =>
      part
        .trim()
        .split(" ")
        .map((word) => word.charAt(0).toLocaleUpperCase("tr-TR") + word.slice(1))
        .join(" "),
    )
    .join(" / ");
}

export function translateCustomsApiText(text: string): string {
  if (!text?.trim()) {
    return text;
  }

  const normalized = text.trim();

  const exact: Record<string, string> = {
    "Outcome Assigned": "İş Atandı",
    "Context Loaded": "Bağlam Yüklendi",
    "Documents Prepared": "Belgeler Hazırlandı",
    "Blocker Detected": "Engel Tespit Edildi",
    "Corrective Work Created": "Düzeltme İşi Oluşturuldu",
    "Corrective Work Completed": "Düzeltme Tamamlandı",
    "Evidence Re-checked": "Kanıt Yeniden Kontrol Edildi",
    "Approval Pending": "Onay Bekliyor",
    Approved: "Onaylandı",
    "Package Assembled": "Paket Birleştirildi",
    "Verified Ready": "Doğrulandı — Hazır",
    "VERIFIED READY": "DOĞRULANDI — HAZIR",
    "PARTIALLY READY": "KISMEN HAZIR",
    "NOT READY": "HAZIR DEĞİL",
    "INSUFFICIENT DATA": "YETERSİZ VERİ",
    PENDING: "Bekliyor",
    PASS: "Geçti",
    FAIL: "Başarısız",
    COMPLETE: "Tamamlandı",
    READY: "Hazır",
    open: "Açık",
    resolved: "Çözüldü",
    created: "Oluşturuldu",
    "in-progress": "Devam Ediyor",
    completed: "Tamamlandı",
    draft: "Taslak",
    ready: "Hazır",
    invalid: "Geçersiz",
    superseded: "Güncellendi",
    pass: "Geçti",
    fail: "Başarısız",
    "not-required": "Gerekmez",
    "insufficient-data": "Yetersiz Veri",
    "export-operations-reviewer": "ihracat operasyon inceleyici",
    "export-documentation-operator": "ihracat dokümantasyon operatörü",
    "Customs File Digital Worker": "Gümrük Dosyası Dijital Çalışanı",
    "Commercial Invoice": "Ticari Fatura",
    "Packing List": "Çeki Listesi",
    "A.TR Status": "A.TR Durumu",
    "EUR.1 Status": "EUR.1 Durumu",
    "Certificate of Origin Status": "Menşe Belgesi Durumu",
    "Order data complete": "Sipariş verisi tam",
    "Customer data complete": "Müşteri verisi tam",
    "Destination complete": "Varış noktası tam",
    "Product data complete": "Ürün verisi tam",
    "GTIP present if required": "GTİP (gerekirse) mevcut",
    "Origin present if required": "Menşe (gerekirse) mevcut",
    "Commercial Invoice ready": "Ticari fatura hazır",
    "Packing List ready": "Çeki listesi hazır",
    "Packing data consistent": "Sevkiyat verisi tutarlı",
    "Configured trade documents addressed": "Ticari belgeler ele alındı",
    "No blocking issues": "Engelleyici sorun yok",
    "Internal approval complete": "İç onay tamamlandı",
    "Net weight consistency": "Net ağırlık tutarlılığı",
    "Gross weight consistency": "Brüt ağırlık tutarlılığı",
    "Commercial Invoice presence": "Ticari fatura varlığı",
    "Packing List presence": "Çeki listesi varlığı",
    "Order vs Commercial Invoice quantity": "Sipariş / ticari fatura miktar uyumu",
    "Order vs Packing List quantity": "Sipariş / çeki listesi miktar uyumu",
    "Customer consistency": "Müşteri tutarlılığı",
    "Destination consistency": "Varış tutarlılığı",
    "Currency consistency": "Para birimi tutarlılığı",
    "Package count completeness": "Paket adedi tamlığı",
    "Requirements Evaluated": "Gereksinimler Değerlendirildi",
    "Correction Completed": "Düzeltme Tamamlandı",
    "Packing List New Version": "Çeki Listesi Yeni Sürüm",
    "Approval Requested": "Onay Talep Edildi",
    "Approval Completed": "Onay Tamamlandı",
    "Customs Package Assembled": "Gümrük Paketi Birleştirildi",
    "Verification Executed": "Doğrulama Yürütüldü",
    "Correct Packing List Weight Data": "Çeki Listesi Ağırlık Verisini Düzelt",
    "Packing list net weight does not match shipment net weight.":
      "Çeki listesi net ağırlığı sevkiyat net ağırlığı ile eşleşmiyor.",
    "Customs clearance package verified ready for customs broker handoff.":
      "Gümrük dosyası paketi müşavir devrine hazır olarak doğrulandı.",
    "Human approval is required before broker handoff.":
      "Müşavir devri öncesinde insan onayı gereklidir.",
    "Customs clearance package has not been assembled.":
      "Gümrük dosyası paketi henüz birleştirilmedi.",
    "Blocking issues remain unresolved.": "Engelleyici sorunlar çözülmedi.",
    "Mandatory requirements are not satisfied.": "Zorunlu gereksinimler karşılanmadı.",
    "Controlled technical intervention with validation run.":
      "Doğrulama koşulu ile kontrollü teknik müdahale.",
    measured: "Ölçüldü",
    calculated: "Hesaplandı",
    estimated: "Tahmini",
    verified: "Doğrulandı",
  };

  if (exact[normalized]) {
    return exact[normalized];
  }

  let translated = normalized;

  translated = translated.replace(
    /Packing List net weight ([0-9.]+) kg differs from shipment net weight ([0-9.]+) kg\./i,
    (_, packing: string, shipment: string) =>
      `Çeki listesi net ağırlığı ${formatCustomsNumber(Number(packing), 1)} kg; sevkiyat net ağırlığı ${formatCustomsNumber(Number(shipment), 1)} kg.`,
  );
  translated = translated.replace(/Commercial Invoice/gi, "Ticari Fatura");
  translated = translated.replace(/Packing List/gi, "Çeki Listesi");
  translated = translated.replace(/Customs Broker Handoff/gi, "Müşavir Devri");
  translated = translated.replace(/Blocking Issues/gi, "Engelleyici Sorunlar");
  translated = translated.replace(/Documents/gi, "Belgeler");
  translated = translated.replace(/READY/gi, "HAZIR");
  translated = translated.replace(/Agent/gi, "Dijital Çalışan");
  translated = translated.replace(/Ajan/gi, "Dijital Çalışan");

  return translated;
}

export type CustomsChecklistItem = Readonly<{
  key: string;
  name: string;
  icon: string;
  statusLabel: string;
}>;

export function getCustomsDocumentChecklist(workspace: GoldenDemoCustomsFileWorkspaceApi): readonly CustomsChecklistItem[] {
  const currentDocuments = workspace.documents.filter((doc) => doc.isCurrent);

  const documentItems = currentDocuments.map((doc) => {
    const status = getCustomsDocumentStatusPresentation(doc.status);
    return {
      key: doc.id,
      name: getCustomsDocumentLabel(doc.documentType),
      icon: status.icon,
      statusLabel: status.label,
    };
  });

  const requirementItems = workspace.requirements
    .filter((req) => req.required)
    .map((req) => {
      const status = getCustomsRequirementStatusPresentation(req.status, req.blocking);
      return {
        key: req.requirementCode,
        name: translateCustomsApiText(req.label),
        icon: status.icon,
        statusLabel: status.label,
      };
    });

  const consistencyItems = workspace.consistencyChecks.map((check) => {
    const status = getCustomsRequirementStatusPresentation(check.status, check.status === "fail");
    return {
      key: check.checkCode,
      name: translateCustomsApiText(check.label),
      icon: status.icon,
      statusLabel: status.label,
    };
  });

  return [...documentItems, ...requirementItems, ...consistencyItems];
}
