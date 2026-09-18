export const ZENTRA_BRAND = {
  productName: "Zentra",
  fullProductName: "Nevaxis Zentra",
  endorsement: "by NEVAXIS",
  descriptor: "Production Intelligence & Autonomous Operations",
  documentTitleSuffix: "Nevaxis Zentra",
  heroBackgroundColor: "#e9eef2",
  heroAlt:
    "Nevaxis Zentra — Üretimi Anlar. Önceden Görür. Harekete Geçirir.",
  lockupAlt: "Nevaxis Zentra",
} as const;

export function buildZentraDocumentTitle(pageTitle?: string): string {
  if (pageTitle && pageTitle.trim().length > 0) {
    return `${pageTitle} | ${ZENTRA_BRAND.documentTitleSuffix}`;
  }

  return ZENTRA_BRAND.documentTitleSuffix;
}

/** @deprecated Use ZENTRA_BRAND */
export const FACTOS_BRAND = ZENTRA_BRAND;

/** @deprecated Use buildZentraDocumentTitle */
export const buildFactosDocumentTitle = buildZentraDocumentTitle;

export const DEVELOPMENT_DEMO_CREDENTIALS = {
  organizationContext: "dev-pilot-tenant",
  email: "admin@demo.nevaxiszentra.local",
} as const;
