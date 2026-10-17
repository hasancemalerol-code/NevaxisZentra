"use client";

import { usePathname } from "next/navigation";
import type { ReactNode } from "react";

/** The module a route belongs to, in the product's own words: the eyebrow of the shared page header. */
const MODULE_LABELS: Readonly<Record<string, string>> = {
  ayarlar: "Ayarlar",
  bakim: "Bakım ve Arıza",
  "ai-gorevleri": "Görev Merkezi",
  "erp-entegrasyonlari": "ERP Entegrasyonları",
  "fabrika-karar-merkezi": "Karar Merkezi",
  ihracat: "İhracat",
  kaliplar: "Kalıp Yönetimi",
  kalite: "Kalite Kontrol",
  malzemeler: "Malzeme Yönetimi",
  operasyon: "Operasyon Yönetimi",
  planlama: "Üretim Planlama",
  siparisler: "Sipariş Yönetimi",
  teams: "Operasyon İletişimi",
  "ticari-zeka": "Ticari Zekâ",
  uretim: "Üretim",
  vardiyalar: "Vardiya Operasyonları",
  "vardiya-devir": "Vardiya Operasyonları",
};

export function resolveModuleLabel(pathname: string | null): string | undefined {
  const segment = (pathname ?? "").split("/").filter(Boolean)[0];
  return segment ? MODULE_LABELS[segment] : undefined;
}

/**
 * One layout for every full-page state: the page header (module eyebrow + title) first, then the card. When the route
 * belongs to no known module the title moves into the card, so there is always exactly one h1.
 */
export function StatePageLayout({
  title,
  moduleLabel,
  cardWithTitle,
  cardWithoutTitle,
}: Readonly<{ title: string; moduleLabel?: string; cardWithTitle: ReactNode; cardWithoutTitle: ReactNode }>) {
  const pathname = usePathname();
  const label = moduleLabel ?? resolveModuleLabel(pathname);
  return (
    <>
      {label ? (
        <header className="zx-state-header zx-page-header">
          <div className="zx-page-header__copy">
            <p className="zx-page-header__eyebrow">{label}</p>
            <h1 id="zx-state-title">{title}</h1>
          </div>
        </header>
      ) : null}
      {label ? cardWithoutTitle : cardWithTitle}
    </>
  );
}
