export const FACTORY_BLOCKER_LABELS: Record<string, string> = {
  none: "Bloker yok",
  "waiting-maintenance": "Bakım bekleniyor",
  "waiting-material": "Malzeme bekleniyor",
  "waiting-approval": "Onay bekleniyor",
  "waiting-quality": "Kalite bekleniyor",
  "waiting-supplier": "Tedarikçi bekleniyor",
  "waiting-machine-verification": "Makine doğrulaması bekleniyor",
  "waiting-human": "İnsan aksiyonu bekleniyor",
  "waiting-external-system": "Dış sistem bekleniyor",
  "waiting-data": "Veri bekleniyor",
  deadlock: "Bağımlılık döngüsü",
  conflict: "Veri çakışması",
};

export function getFactoryBlockerLabel(code: string): string {
  return FACTORY_BLOCKER_LABELS[code] ?? code;
}
