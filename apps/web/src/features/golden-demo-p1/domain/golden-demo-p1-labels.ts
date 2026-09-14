export const GOLDEN_DEMO_P1_TITLE = "Üretim Kurtarma — Altın Demo";
export const GOLDEN_DEMO_P1_SUBTITLE =
  "Makine zekası, cycle loss ayrıştırma, kurtarma fırsatı ve doğrulama akışı";

export const GOLDEN_DEMO_P1_ROUTE = "/golden-demo/uretim-kurtarma";

export function getGoldenDemoEventStatusLabel(status: string): string {
  switch (status) {
    case "active":
      return "Aktif — hedef risk altında";
    case "intervention-in-progress":
      return "Müdahale devam ediyor";
    case "validation-run":
      return "Doğrulama koşusu";
    case "verified":
      return "Doğrulandı";
    case "closed":
      return "Kapalı";
    default:
      return status;
  }
}

export function getGoldenDemoAgentLabel(agentCode: string): string {
  switch (agentCode) {
    case "production-performance":
      return "Üretim Performans Ajanı";
    case "machine-performance":
      return "Makine Performans Ajanı";
    case "process-optimization":
      return "Proses Optimizasyon Ajanı";
    case "mold-health":
      return "Kalıp Sağlık Ajanı";
    case "maintenance":
      return "Bakım Ajanı";
    case "energy":
      return "Enerji Ajanı";
    case "orchestrator":
      return "FactOS Orchestrator";
    default:
      return agentCode;
  }
}

export function getGoldenDemoVerificationLabel(result: string): string {
  switch (result) {
    case "verified":
      return "DOĞRULANDI";
    case "partially-verified":
      return "KISMEN DOĞRULANDI";
    case "not-verified":
      return "DOĞRULANMADI";
    case "insufficient-data":
      return "YETERSİZ VERİ";
    default:
      return result;
  }
}

export function formatGoldenDemoNumber(value: number, fractionDigits = 0): string {
  return value.toLocaleString("tr-TR", {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  });
}
