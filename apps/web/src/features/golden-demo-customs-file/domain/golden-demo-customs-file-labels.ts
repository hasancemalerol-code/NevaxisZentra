export const GOLDEN_DEMO_CUSTOMS_FILE_ROUTE = "/golden-demo/gumruk-dosyasi";
export const GOLDEN_DEMO_CUSTOMS_FILE_TITLE = "Gümrük Dosyası Hazırlama";
export const GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE =
  "Gümrük Dosyası Dijital Çalışanı belgeleri toplar, tutarsızlıkları kontrol eder ve müşavir devrine hazırlar";

export const GOLDEN_DEMO_CUSTOMS_FILE_OUTCOME_ID = "cc000001-0010-4000-8000-000000000001";

export function getCustomsDocumentLabel(documentType: string): string {
  switch (documentType) {
    case "commercial-invoice":
      return "Ticari Fatura";
    case "packing-list":
      return "Çeki Listesi";
    case "atr-status":
      return "A.TR Durumu";
    case "eur1-status":
      return "EUR.1 Durumu";
    case "certificate-of-origin-status":
      return "Menşe Belgesi Durumu";
    default:
      return documentType.replace(/-/g, " ");
  }
}

export function formatCustomsNumber(value: number, fractionDigits = 0): string {
  return value.toLocaleString("tr-TR", {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  });
}
