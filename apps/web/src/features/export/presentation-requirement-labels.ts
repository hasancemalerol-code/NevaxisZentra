/**
 * Customer wording for export readiness requirement titles. The API sends stable English keys/titles; the canonical
 * value is never changed, only the rendered text.
 */
const REQUIREMENT_LABELS: Readonly<Record<string, string>> = {
  "source order linked": "Kaynak sipariş bağlı",
  "destination country known": "Hedef ülke belirli",
  "origin country known": "Menşe ülke belirli",
  "commercial-invoice": "Ticari fatura",
  "commercial invoice": "Ticari fatura",
  "packing-list": "Çeki listesi",
  "packing list": "Çeki listesi",
  "packing data complete": "Paketleme verisi tamam",
  "packing quantity reconciled": "Paketleme miktarı mutabık",
  "shipment transport mode known": "Sevkiyat taşıma modu belirli",
  "broker corrections clear": "Broker düzeltmeleri kapalı",
  "country compliance": "Ülke uygunluğu",
  "certificate-of-origin": "Menşe şahadetnamesi",
  "bill-of-lading": "Konşimento",
  "customs-declaration": "Gümrük beyannamesi",
  "export-license": "İhracat lisansı",
};

export function exportRequirementLabel(label: string | null | undefined): string {
  const text = (label ?? "").trim();
  if (!text) return "Gereksinim";
  return REQUIREMENT_LABELS[text.toLowerCase()] ?? text;
}
