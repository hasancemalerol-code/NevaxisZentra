import { formatGoldenDemoNumber } from "./golden-demo-p1-labels";

export type GoldenDemoAgentPresentationContext = Readonly<{
  recoveryGoodParts: number;
  cycleLossSeconds?: number;
  referenceCycleSeconds?: number | null;
  currentCycleSeconds?: number | null;
  topContributorLabel?: string;
  topContributorDeviation?: number;
  kwhPerGoodPart?: number | null;
  hasBaseline?: boolean;
}>;

export function getGoldenDemoEvidenceSourceLabel(agentCode: string): string {
  switch (agentCode) {
    case "production-performance":
      return "Üretim Performansı";
    case "machine-performance":
      return "Makine Performansı";
    case "process-optimization":
      return "Proses Optimizasyonu";
    case "mold-health":
      return "Kalıp Sağlığı";
    case "maintenance":
      return "Bakım";
    case "energy":
      return "Enerji";
    default:
      return agentCode.replace(/-/g, " ");
  }
}

export function parseGoldenDemoEvidenceAgentCode(reference: string): string | null {
  const [code] = reference.split(":");
  if (!code?.trim()) {
    return null;
  }

  return code.trim();
}

export function getGoldenDemoDigitalWorkerLabel(agentCode: string): string {
  switch (agentCode) {
    case "production-performance":
      return "Üretim Performansı Dijital Çalışanı";
    case "machine-performance":
      return "Makine Performansı Dijital Çalışanı";
    case "process-optimization":
      return "Proses Optimizasyonu Dijital Çalışanı";
    case "mold-health":
      return "Kalıp Sağlığı Dijital Çalışanı";
    case "maintenance":
      return "Bakım Dijital Çalışanı";
    case "energy":
      return "Enerji Dijital Çalışanı";
    case "orchestrator":
      return "FactOS Orchestrator";
    default:
      return agentCode.replace(/-/g, " ");
  }
}

export function getGoldenDemoEvidenceStatusChip(
  agentCode: string,
  classification: string,
): string {
  switch (agentCode) {
    case "machine-performance":
      return "Tespit Edildi";
    case "mold-health":
      return "Analiz Edildi";
    case "maintenance":
      return "Hazır";
    case "energy":
      return "Ölçüldü";
    default:
      break;
  }

  switch (classification.toLowerCase()) {
    case "calculated":
      return "Hesaplandı";
    case "measured":
      return "Ölçüldü";
    case "verified":
      return "Doğrulandı";
    case "estimated":
      return "Tahmini";
    default:
      return translateGoldenDemoApiText(classification);
  }
}

export function getGoldenDemoAgentCardSummary(
  agentCode: string,
  context: GoldenDemoAgentPresentationContext,
): string {
  switch (agentCode) {
    case "production-performance":
      return `+${formatGoldenDemoNumber(context.recoveryGoodParts)} iyi parça/vardiya geri kazanım fırsatı hesaplandı.`;
    case "machine-performance":
      if (!context.hasBaseline || context.referenceCycleSeconds == null || context.currentCycleSeconds == null) {
        return "Referans çevrim verisi yetersiz; karşılaştırma bekleniyor.";
      }
      return `Çevrim süresi referansın ${formatGoldenDemoNumber(context.currentCycleSeconds - context.referenceCycleSeconds, 1)} sn üzerinde.`;
    case "process-optimization":
      if (context.topContributorLabel && context.topContributorDeviation != null) {
        return `En yüksek kayıp katkısı: ${context.topContributorLabel} +${formatGoldenDemoNumber(context.topContributorDeviation, 1)} sn.`;
      }
      return "Baskın proses faz kaybı tespit edilmedi.";
    case "mold-health":
      return "Kalıp çevrim ve bakım geçmişi kurtarma analizine dahil edildi.";
    case "maintenance":
      return "Teknik müdahale adımları kanıta dayalı olarak hazırlandı.";
    case "energy":
      if (context.kwhPerGoodPart != null) {
        return `Enerji yoğunluğu ${formatGoldenDemoNumber(context.kwhPerGoodPart, 3)} kWh / iyi parça.`;
      }
      return "Enerji yoğunluğu verisi sınırlı.";
    default:
      return "Analiz tamamlandı.";
  }
}

export function translateGoldenDemoApiText(text: string, recoveryGoodParts?: number): string {
  if (!text?.trim()) {
    return text;
  }

  const normalized = text.trim();
  const recovery = recoveryGoodParts ?? extractRecoveryGoodParts(normalized);

  const turkishDigitalWorkerFromLegacyAjan: Record<string, string> = {
    "Üretim Performans Ajanı": "Üretim Performansı Dijital Çalışanı",
    "Makine Performans Ajanı": "Makine Performansı Dijital Çalışanı",
    "Proses Optimizasyon Ajanı": "Proses Optimizasyonu Dijital Çalışanı",
    "Kalıp Sağlık Ajanı": "Kalıp Sağlığı Dijital Çalışanı",
    "Bakım Ajanı": "Bakım Dijital Çalışanı",
    "Enerji Ajanı": "Enerji Dijital Çalışanı",
  };

  if (turkishDigitalWorkerFromLegacyAjan[normalized]) {
    return turkishDigitalWorkerFromLegacyAjan[normalized];
  }

  const exact: Record<string, string> = {
    "Current losses indicate an opportunity to return the machine to its validated stable operating band.":
      "Mevcut kayıplar, makinenin doğrulanmış kararlı operasyon bandına dönmesi için bir fırsat gösterir.",
    "Cycle loss continues; shift target gap persists.":
      "Çevrim kaybı sürüyor; vardiya hedefi açığı devam ediyor.",
    "Losses accumulate until end-of-shift intervention.":
      "Kayıplar vardiya sonu müdahalesine kadar birikiyor.",
    "Controlled technical intervention with validation run.":
      "Doğrulama koşusu ile kontrollü teknik müdahale.",
    "Evidence supports recoverable cycle loss within validated operating band.":
      "Kanıtlar, doğrulanmış operasyon bandında kurtarılabilir çevrim kaybını destekliyor.",
    "Operational risk if losses continue.":
      "Kayıplar sürerse operasyonel risk artar.",
    "Insufficient baseline for machine cycle comparison.":
      "Makine çevrim karşılaştırması için referans yetersiz.",
    "No dominant process phase loss detected.":
      "Baskın proses faz kaybı tespit edilmedi.",
    "Mold/tool cycle and maintenance context included in recovery assessment.":
      "Kalıp/makine bağlamı kurtarma değerlendirmesine dahil edildi.",
    "Technical recovery actions mapped to evidence-backed inspection steps.":
      "Teknik kurtarma aksiyonları kanıta dayalı kontrol adımlarına eşlendi.",
    "INSUFFICIENT BASELINE": "Referans yetersiz",
    "INSUFFICIENT MOLD EVIDENCE": "Kalıp kanıtı yetersiz",
    "ENERGY DATA INSUFFICIENT": "Enerji verisi yetersiz",
    "Human approval required before intervention execution.":
      "Müdahale yürütülmeden önce insan onayı gerekli.",
    "FactOS coordinated the technical intervention; authorized role executes physical work.":
      "FactOS teknik müdahaleyi koordine etti; yetkili rol fiziksel işi uygular.",
    "Post-intervention observation window recorded.":
      "Müdahale sonrası gözlem penceresi kaydedildi.",
    "Production gap calculated": "Üretim açığı hesaplandı",
    "Machine cycle loss decomposed": "Makine çevrim kaybı ayrıştırıldı",
    "Process contributors evaluated": "Proses katkıda bulunanları değerlendirildi",
    "Mold evidence reviewed": "Kalıp kanıtı incelendi",
    "Energy intensity reviewed": "Enerji yoğunluğu incelendi",
    "Maintenance recovery work mapped": "Bakım kurtarma işi eşlendi",
    "Specialist analysis completed": "Uzman analizi tamamlandı",
    "Recovery plan created": "Kurtarma planı oluşturuldu",
    "Approval requested": "Onay talep edildi",
    "Approval granted": "Onay verildi",
    "Intervention started": "Müdahale başlatıldı",
    "Intervention completed": "Müdahale tamamlandı",
    "Validation run started": "Doğrulama koşusu başlatıldı",
    "Outcome verified": "Sonuç doğrulandı",
    completed: "Tamamlandı",
    pending: "Beklemede",
    approved: "Onaylandı",
    "in-progress": "Devam ediyor",
    calculated: "Hesaplandı",
    measured: "Ölçüldü",
    verified: "Doğrulandı",
    estimated: "Tahmini",
    high: "Yüksek",
    medium: "Orta",
    low: "Düşük",
    process: "Proses",
    machine: "Makine",
    maintenance: "Bakım",
    "Cycle time improved": "Çevrim süresi iyileşti",
    "Good parts/hour improved": "İyi parça/saat iyileşti",
    "Micro-stops decreased": "Mikro-duruşlar azaldı",
    "Reject rate stable or improved": "Red oranı stabil veya iyileşti",
    "Availability stable or improved": "Kullanılabilirlik stabil veya iyileşti",
    "kWh/good part stable or improved": "kWh/iyi parça stabil veya iyileşti",
    "Post-intervention observation window not available.":
      "Müdahale sonrası gözlem penceresi mevcut değil.",
    passed: "Geçti",
    failed: "Başarısız",
    partial: "Kısmi",
    "FactOS Digital Factory Team": "FactOS Dijital Çalışan Ekibi",
    "planning-pilot-reviewer / production-manager":
      "planlama pilot inceleyici / üretim yöneticisi",
    "production-manager / planning-pilot-reviewer":
      "üretim yöneticisi / planlama pilot inceleyici",
    "INSUFFICIENT BASELINE — reliable historical reference not available for this machine/product/mold combination.":
      "Referans yetersiz — bu makine/ürün/kalıp kombinasyonu için güvenilir geçmiş referans mevcut değil.",
    "Baseline unavailable — recovery opportunity cannot be calculated.":
      "Referans mevcut değil — kurtarma fırsatı hesaplanamıyor.",
    "Recovery assumes restoring validated stable operating band, not unsafe acceleration.":
      "Kurtarma, güvenli olmayan hızlandırma değil doğrulanmış stabil operasyon bandının geri kazanımını varsayar.",
    "Micro-stop equivalent loss is tracked separately from phase loss.":
      "Mikro-duruş eşdeğer kaybı, faz kaybından ayrı izlenir.",
    "Current shift observation window": "Mevcut vardiya gözlem penceresi",
    "probable-contributor": "olası katkı",
    "likely-contributor": "muhtemel katkı",
    "process-technician": "proses teknisyeni",
    "maintenance-technician": "bakım teknisyeni",
    "production-supervisor": "üretim süpervizörü",
    "production-pilot-operator": "üretim pilot operatörü",
    "production-manager": "üretim yöneticisi",
    "planning-pilot-reviewer": "planlama pilot inceleyici",
    reviewer: "inceleyici",
    "Referans cycle": "Referans çevrim",
    "Mevcut cycle": "Mevcut çevrim",
    "Cycle faz ayrıştırması": "Çevrim faz ayrıştırması",
    "Related technical finding unavailable.": "İlgili teknik bulgu mevcut değil.",
    "Plan Created": "Plan Oluşturuldu",
    "Approval Required": "Onay Gerekli",
    Approved: "Onaylandı",
    "Intervention Started": "Müdahale Başlatıldı",
    "In Progress": "Devam Ediyor",
    "Intervention Completed": "Müdahale Tamamlandı",
    "Validation Run": "Doğrulama Koşusu",
    "Partially Verified": "Kısmen Doğrulandı",
    "Not Verified": "Doğrulanmadı",
    "Verification In Progress": "Doğrulama Devam Ediyor",
    current: "Güncel",
    "not-required": "Gerekmez",
    "not-created": "Oluşturulmadı",
    active: "Aktif",
    "intervention-in-progress": "Müdahale devam ediyor",
    "validation-run": "Doğrulama koşusu",
    "waiting-for-validation": "Doğrulama bekleniyor",
    "collecting-data": "Veri toplanıyor",
    "ready-for-verification": "Doğrulamaya hazır",
    "partially-verified": "Kısmen doğrulandı",
    "not-verified": "Doğrulanmadı",
    "insufficient-data": "Yetersiz veri",
    "Waiting For Validation": "Doğrulama Bekleniyor",
    "Collecting Data": "Veri Toplanıyor",
    "Ready For Verification": "Doğrulamaya Hazır",
    Verified: "Doğrulandı",
    "Insufficient Data": "Yetersiz Veri",
    "PRODUCTION RECOVERED": "ÜRETİM KURTARILDI",
    "PARTIAL RECOVERY": "KISMİ KURTARMA",
    "OUTCOME NOT VERIFIED": "SONUÇ DOĞRULANMADI",
    "INSUFFICIENT DATA": "YETERSİZ VERİ",
    "PRODUCTION RECOVERED — improvement persisted across validation window.":
      "ÜRETİM KURTARILDI — iyileşme doğrulama penceresi boyunca sürdü.",
    "Partial improvement observed; continue monitoring validation window.":
      "Kısmi iyileşme gözlemlendi; doğrulama penceresi izlenmeye devam edilmeli.",
    "Expected production recovery not fully verified.":
      "Beklenen üretim kurtarması tam olarak doğrulanmadı.",
    "FactOS compared pre-intervention baseline against post-intervention validation evidence.":
      "FactOS, müdahale öncesi referansı müdahale sonrası doğrulama kanıtlarıyla karşılaştırdı.",
    "Measurable improvement exists, but not all verification checks passed.":
      "Ölçülebilir iyileşme var, ancak tüm doğrulama kontrolleri geçmedi.",
    "Intervention completed, but production recovery was not verified.":
      "Müdahale tamamlandı, ancak üretim kurtarması doğrulanmadı.",
    "Validation evidence is not yet sufficient for outcome verification.":
      "Sonuç doğrulaması için doğrulama kanıtı henüz yeterli değil.",
    "FactOS is waiting for intervention completion before validating post-intervention production evidence.":
      "FactOS, müdahale sonrası üretim kanıtlarını doğrulamadan önce müdahalenin tamamlanmasını bekliyor.",
    "FactOS is validating the intervention against post-intervention production evidence.":
      "FactOS müdahaleyi müdahale sonrası üretim kanıtlarına karşı doğruluyor.",
    "Validation window recorded. Outcome verification can be executed against measured post-intervention data.":
      "Doğrulama penceresi kaydedildi. Sonuç doğrulaması ölçülen müdahale sonrası veriye karşı yürütülebilir.",
    "Production recovery verified against validation window evidence.":
      "Üretim kurtarması doğrulama penceresi kanıtlarına karşı doğrulandı.",
    "Partial improvement observed; secondary goals not fully confirmed.":
      "Kısmi iyileşme gözlemlendi; ikincil hedefler tam olarak teyit edilmedi.",
    "Required validation evidence is insufficient.":
      "Gerekli doğrulama kanıtı yetersiz.",
    "Current operating window": "Mevcut operasyon penceresi",
    "Post-intervention validation window": "Müdahale sonrası doğrulama penceresi",
    "Post-intervention validation window missing": "Müdahale sonrası doğrulama penceresi eksik",
    "Validation requires additional operating time": "Doğrulama ek operasyon süresi gerektiriyor",
    "No production recorded in validation window": "Doğrulama penceresinde üretim kaydı yok",
    "Energy telemetry unavailable for intensity comparison":
      "Yoğunluk karşılaştırması için enerji telemetrisi mevcut değil",
    "Cycle Time": "Çevrim Süresi",
    Throughput: "İyi Parça / Saat",
    "Micro-Stops": "Mikro Duruş",
    "Reject Rate": "Red Oranı",
    Availability: "Kullanılabilirlik",
    "Good Parts / Hour": "İyi Parça / Saat",
    "Projected Good Parts / Shift": "Vardiya Projeksiyonu (İyi Parça)",
    "kWh / Good Part": "kWh / İyi Parça",
    "+X Good Parts / Shift": "Kurtarılan İyi Parça / Vardiya",
    "Validation Window": "Doğrulama Penceresi",
    PASS: "Geçti",
    FAIL: "Başarısız",
    improved: "İyileşti",
    stable: "Stabil",
    regressed: "Geriledi",
    unavailable: "Mevcut değil",
    "Stable / Improved": "Stabil / İyileşti",
    Regressed: "Geriledi",
    "Stable / improved reject guardrail": "Stabil / iyileşmiş red koruma bandı",
    "Reduced micro-stop exposure": "Azaltılmış mikro-duruş maruziyeti",
    "Associated improvement": "İlişkili iyileşme",
    "Insufficient data": "Yetersiz veri",
    Unavailable: "Mevcut değil",
    "Production Performance Loss": "Üretim Performans Kaybı",
    "Downtime / Micro-stop Exposure": "Duruş / Mikro-duruş Maruziyeti",
    "Quality / Scrap Loss": "Kalite / Hurda Kaybı",
    "Energy Inefficiency": "Enerji Verimsizliği",
    "Capacity Loss": "Kapasite Kaybı",
    "ENERGY IMPACT — Insufficient data": "ENERJİ ETKİSİ — Yetersiz veri",
    "Energy intensity comparison unavailable without kWh / good part telemetry.":
      "kWh / iyi parça telemetrisi olmadan enerji yoğunluğu karşılaştırması yapılamaz.",
    "Observed relationship: post-intervention energy intensity improved alongside production recovery.":
      "Gözlemlenen ilişki: müdahale sonrası enerji yoğunluğu üretim kurtarmasıyla birlikte iyileşti.",
    "Associated pattern: energy intensity moved with post-intervention operating conditions.":
      "İlişkili örüntü: enerji yoğunluğu müdahale sonrası operasyon koşullarıyla birlikte hareket etti.",
    "Agent finding → orchestrator decision → approved action → intervention → validation → outcome":
      "Dijital Çalışan bulgusu → koordinasyon kararı → onaylı aksiyon → müdahale → doğrulama → sonuç",
  };

  if (exact[normalized]) {
    return exact[normalized];
  }

  let translated = normalized;

  if (recovery != null) {
    translated = translated.replace(
      /^If the identified recoverable losses are reduced, the system estimates (\d+) additional good parts per shift within the validated operating range\.$/i,
      (_, count: string) =>
        `Tespit edilen kurtarılabilir kayıplar azaltılırsa, sistem doğrulanmış operasyon aralığında vardiya başına ${count} ek iyi parça öngörür.`,
    );
    translated = translated.replace(
      /^Shift gap analysis: \+(\d+) recoverable good parts estimated\.$/i,
      (_, count: string) => `Vardiya açığı analizi: +${count} kurtarılabilir iyi parça hesaplandı.`,
    );
  }

  translated = translated.replace(
    /^Cycle loss ([0-9.]+)s vs reference ([0-9.]+)s\.$/i,
    (_, loss: string, reference: string) =>
      `Çevrim kaybı ${loss} sn; referans ${reference} sn.`,
  );
  translated = translated.replace(
    /^Top process contributor: (.+) \(([0-9.]+)s\)\.$/i,
    (_, label: string, seconds: string) =>
      `En yüksek proses katkısı: ${label} (${seconds} sn).`,
  );
  translated = translated.replace(
    /^Energy intensity ([0-9.]+) kWh\/good part observed\.$/i,
    (_, value: string) => `Enerji yoğunluğu ${value} kWh / iyi parça gözlemlendi.`,
  );
  translated = translated.replace(/good parts/gi, "iyi parça");
  translated = translated.replace(/good part/gi, "iyi parça");
  translated = translated.replace(/per shift/gi, "vardiya başına");
  translated = translated.replace(
    /^Before ([0-9.]+)s → After ([0-9.]+)s$/i,
    (_, before: string, after: string) => `Önce ${before} sn → Sonra ${after} sn`,
  );
  translated = translated.replace(
    /^Before ([0-9.]+) → After ([0-9.]+)$/i,
    (_, before: string, after: string) => `Önce ${before} → Sonra ${after}`,
  );
  translated = translated.replace(
    /^Before ([0-9.]+)% → After ([0-9.]+)%$/i,
    (_, before: string, after: string) => `Önce %${before} → Sonra %${after}`,
  );
  translated = translated.replace(
    /^Before ([0-9.]+) kWh\/good part · After ([0-9.]+) kWh\/good part · Change ([0-9.-]+)%$/i,
    (_, before: string, after: string, change: string) =>
      `Önce ${before} kWh/iyi parça · Sonra ${after} kWh/iyi parça · Değişim %${change}`,
  );
  translated = translated.replace(
    /^Yield factor ([0-9.,%]+) and availability ([0-9.,]+)% applied\.$/i,
    (_, yieldFactor: string, availability: string) =>
      `Verim faktörü ${yieldFactor} ve kullanılabilirlik %${availability} uygulandı.`,
  );
  translated = translated.replace(
    /(\d+)s cycle/gi,
    (_, seconds: string) => `${seconds} sn çevrim`,
  );
  translated = translated.replace(
    /\+(\d+) good parts \/ shift/gi,
    (_, count: string) => `+${count} iyi parça / vardiya`,
  );
  translated = translated.replace(
    /\+(\d+) protected throughput \/ shift/gi,
    (_, count: string) => `+${count} korunan verim / vardiya`,
  );

  for (const [legacy, label] of Object.entries(turkishDigitalWorkerFromLegacyAjan)) {
    translated = translated.replaceAll(legacy, label);
  }

  translated = translated.replace(/ — INSUFFICIENT BASELINE$/i, " — Referans yetersiz");
  translated = translated.replace(/ — INSUFFICIENT MOLD EVIDENCE$/i, " — Kalıp kanıtı yetersiz");
  translated = translated.replace(/ — ENERGY DATA INSUFFICIENT$/i, " — Enerji verisi yetersiz");

  translated = translated.replace(/Cycle loss/gi, "Çevrim kaybı");
  translated = translated.replace(/cycle time/gi, "çevrim süresi");
  translated = translated.replace(/good parts/gi, "iyi parça");
  translated = translated.replace(/Agent/gi, "Dijital Çalışan");
  translated = translated.replace(/Ajanı/g, "Dijital Çalışanı");
  translated = translated.replace(/Ajan/g, "Dijital Çalışan");

  return translated;
}

export function translateGoldenDemoFieldLabel(key: string): string {
  const labels: Record<string, string> = {
    referenceCycleSeconds: "Referans çevrim (sn)",
    currentCycleSeconds: "Mevcut çevrim (sn)",
    cycleLossSeconds: "Çevrim kaybı (sn)",
    shiftTargetGoodParts: "Vardiya hedefi",
    currentExpectedShiftGoodParts: "Mevcut beklenen vardiya çıktısı",
    recoveredExpectedShiftGoodParts: "Kurtarılmış beklenen vardiya çıktısı",
    productionRecoveryGoodParts: "Kurtarma fırsatı (iyi parça)",
    productionRecoveryPercent: "Kurtarma oranı (%)",
    microStopEquivalentLossSeconds: "Mikro-duruş eşdeğer kaybı (sn)",
    topContributor: "En yüksek katkı",
    measuredDeviationSeconds: "Ölçülen sapma (sn)",
    energyIntensityDeviationPercent: "Enerji yoğunluğu sapması (%)",
  };

  return labels[key] ?? key.replace(/([A-Z])/g, " $1").replace(/^./, (c) => c.toUpperCase());
}

function extractRecoveryGoodParts(text: string): number | undefined {
  const match = text.match(/\+?(\d+)\s+(?:recoverable good parts|additional good parts|iyi parça)/i);
  return match ? Number(match[1]) : undefined;
}
