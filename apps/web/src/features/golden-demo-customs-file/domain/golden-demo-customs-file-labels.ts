export const GOLDEN_DEMO_CUSTOMS_FILE_ROUTE = "/golden-demo/gumruk-dosyasi";
export const GOLDEN_DEMO_CUSTOMS_FILE_TITLE = "Gümrük Dosyası — Altın Demo";
export const GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE =
  "Dijital çalışan gümrük dosyasını hazırlar, tutarsızlığı bulur, düzeltir ve müşavir devir teslimine hazırlar";

export const GOLDEN_DEMO_CUSTOMS_FILE_OUTCOME_ID = "cc000001-0010-4000-8000-000000000001";

export function getCustomsDocumentLabel(documentType: string): string {
  switch (documentType) {
    case "commercial-invoice":
      return "Commercial Invoice";
    case "packing-list":
      return "Packing List";
    case "atr-status":
      return "A.TR Status";
    case "eur1-status":
      return "EUR.1 Status";
    case "certificate-of-origin-status":
      return "Certificate of Origin Status";
    default:
      return documentType;
  }
}

export function formatCustomsNumber(value: number, fractionDigits = 0): string {
  return value.toLocaleString("tr-TR", {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  });
}
