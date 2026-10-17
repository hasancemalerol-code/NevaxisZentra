/**
 * Customer-safe display names for values that originate in seeded or integrated data and arrive in English
 * ("Machine Simulator Gateway", role names, tenant names). The canonical value is never changed: wrap only the text that
 * is rendered, never a `value=`, `key=`, `href` or comparison.
 */

const KNOWN: Readonly<Record<string, string>> = {
  "machine simulator gateway": "Makine simülatörü ağ geçidi",
  "opc ua read-only injection line endpoint": "OPC UA salt okunur enjeksiyon hattı bağlantı noktası",
  "edge agent relay — injection floor": "Saha ajanı aktarıcısı — enjeksiyon sahası",
  "edge agent relay - injection floor": "Saha ajanı aktarıcısı — enjeksiyon sahası",
  "mqtt push endpoint (suspended pilot)": "MQTT itme bağlantı noktası (askıya alınmış pilot)",
  "development pilot tenant": "Pilot organizasyon",
  "dev-pilot-tenant": "Pilot organizasyon",
  "development tenant owner": "Organizasyon sahibi",
  "development active user": "Aktif kullanıcı",
  "development deactivated user": "Pasif kullanıcı",
  "development no membership user": "Üyeliği olmayan kullanıcı",
  "production planning reader": "Üretim planlama okuyucusu",
  "production planning reviewer": "Üretim planlama inceleyicisi",
  "production execution operator": "Üretim yürütme operatörü",
  "quality authz negative test (dev only)": "Kalite yetki testi",
  "quality authz negative test": "Kalite yetki testi",
  "virgin polimer": "Ham polimer",
  "virgin polymer": "Ham polimer",
  "human-in-the-loop": "İnsan Onayı",
  "last 30 days": "Son 30 gün",
  routine: "Rutin",
  information: "Bilgi",
};

const GLOSSARY: Readonly<Record<string, string>> = {
  gateway: "ağ geçidi",
  endpoint: "bağlantı noktası",
  injection: "enjeksiyon",
  floor: "saha",
  line: "hat",
  machine: "makine",
  simulator: "simülatör",
  suspended: "askıda",
  "read-only": "salt okunur",
  push: "itme",
  reader: "okuyucu",
  reviewer: "inceleyici",
  operator: "operatör",
  production: "üretim",
  planning: "planlama",
  quality: "kalite",
  tenant: "organizasyon",
  owner: "sahibi",
  state: "durumu",
  running: "çalışıyor",
  counter: "sayacı",
  cycle: "çevrim",
  temperature: "sıcaklık",
  pressure: "basınç",
  speed: "hız",
  alarm: "alarm",
};

function normalize(value: string): string {
  return value.trim().toLowerCase().replace(/\s+/g, " ");
}

function upperFirst(text: string): string {
  return text.charAt(0).toLocaleUpperCase("tr-TR") + text.slice(1);
}

/** A seeded/integrated English name as a Turkish customer label; unknown values pass through unchanged. */
export function displayDataName(value: string | null | undefined): string {
  let text = (value ?? "").trim();
  if (!text) return "";
  const known = KNOWN[normalize(text)];
  if (known) return known;
  for (const [pattern, replacement] of PHRASES) text = text.replace(pattern, replacement);
  // Keep codes, ids and acronyms intact; translate only whole glossary words in an otherwise English phrase.
  const words = text.split(/(\s+)/);
  let translated = 0;
  let unknownEnglish = 0;
  const out = words.map((token) => {
    if (/^\s+$/.test(token)) return token;
    const key = token.toLowerCase().replace(/[(),—-]+$/g, "").replace(/^[(]+/g, "");
    const hit = GLOSSARY[key];
    if (hit) {
      translated += 1;
      return token.replace(new RegExp(key, "i"), hit);
    }
    if (/^[A-Za-z]{4,}$/.test(token) && !/^[A-Z0-9-]+$/.test(token)) unknownEnglish += 1;
    return token;
  });
  return translated > 0 && unknownEnglish === 0 ? upperFirst(out.join("")) : text;
}

export function displayTenantName(value: string | null | undefined): string {
  return displayDataName(value);
}

export function displayAccountName(value: string | null | undefined): string {
  return displayDataName(value);
}

const ROLE_KEYS: Readonly<Record<string, string>> = {
  "production-planning-reader": "Üretim planlama okuyucusu",
  "production-planning-reviewer": "Üretim planlama inceleyicisi",
  "production-execution-operator": "Üretim yürütme operatörü",
  "quality-authz-negative-test": "Kalite yetki testi",
};

/** Role label: explicit key map first, then the display name through the data-name mapper. */
export function displayRoleName(roleKey: string | null | undefined, displayName?: string | null): string {
  const mapped = ROLE_KEYS[(roleKey ?? "").trim().toLowerCase()];
  if (mapped) return mapped;
  return displayDataName(displayName ?? roleKey ?? "");
}

const REFERENCE_PREFIX = /^(machine|mold|material|product|demand|line|station|resource|order|asset)[-_](.+)$/i;
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Customer-facing reference for a business record that only has a technical slug ("machine-im-90" -> "IM-90").
 * Opaque ids (GUIDs) return an empty string so callers can fall back to a plain word instead of printing the id.
 */
export function displayReferenceCode(value: string | null | undefined): string {
  const text = (value ?? "").trim();
  if (!text || UUID.test(text)) return "";
  const match = REFERENCE_PREFIX.exec(text);
  return (match ? match[2] : text).toLocaleUpperCase("en-US");
}

const PHRASES: ReadonlyArray<readonly [RegExp, string]> = [
  [/injection machine/gi, "Enjeksiyon makinesi"],
  [/(\d+)-cavity mold/gi, "$1 gözlü kalıp"],
  [/\bpp cap\b/gi, "PP kapak"],
  [/pilot injection facility/gi, "Pilot Enjeksiyon Tesisi"],
  [/\bcycle time\b/gi, "Çevrim süresi"],
  [/production data \/ cycle-time trend/gi, "üretim verisi / çevrim süresi eğilimi"],
  [/\bshift-([a-z])\b/gi, ""],
  [/production behind plan/gi, "Üretim plandan geride"],
  [/missing operator assignment/gi, "Operatör ataması eksik"],
  [/unresolved execution exceptions/gi, "Çözülmemiş yürütme istisnaları"],
  [/shipment deadline risk \(planning logistics\)/gi, "Sevkiyat termin riski (planlama lojistiği)"],
  [/stale downtime/gi, "Süresi aşan duruş"],
  [/required quantity/gi, "Gereken miktar"],
  [/repeat failure threshold reached/gi, "Tekrarlayan arıza eşiğine ulaşıldı"],
  [/preventive maintenance heavily overdue/gi, "Periyodik bakım ciddi şekilde gecikti"],
  [/no active deterministic maintenance risk/gi, "Etkin bir bakım riski yok"],
  [/human-in-the-loop/gi, "insan onaylı"],
  [/push endpoint/gi, "İtme bağlantı noktası"],
  [/edge agent relay/gi, "Saha ajanı aktarıcısı"],
  [/read-only injection line/gi, "Salt okunur enjeksiyon hattı"],
  [/suspended pilot/gi, "Askıya alınmış pilot"],
  [/development erp simulator/gi, "Örnek ERP bağlantısı"],
  [/development logo test transport/gi, "Örnek Logo bağlantısı"],
  [/coordinator/gi, "Koordinatör"],
  [/omnichannel/gi, "Çok kanallı"],
];

/** Phrase-level presentation mapping for backend-generated English fragments inside otherwise Turkish text. */
export function localizeDataText(value: string | null | undefined): string {
  return displayDataName(value);
}
