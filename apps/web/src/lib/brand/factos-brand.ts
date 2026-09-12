export const FACTOS_BRAND = {
  productName: "FactOS",
  endorsement: "by NEVAXIS",
  documentTitleSuffix: "Nevaxis",
  heroImagePath: "/brand/factos-brand-hero.jpg",
  lockupImagePath: "/brand/factos-brand-lockup.png",
  heroBackgroundColor: "#e9eef2",
  heroAlt:
    "FactOS by NEVAXIS — Üretimi Anlar. Önceden Görür. Harekete Geçirir.",
  lockupAlt: "FactOS by NEVAXIS",
} as const;

export function buildFactosDocumentTitle(pageTitle?: string): string {
  if (pageTitle && pageTitle.trim().length > 0) {
    return `${pageTitle} | ${FACTOS_BRAND.documentTitleSuffix}`;
  }

  return `${FACTOS_BRAND.productName} | ${FACTOS_BRAND.documentTitleSuffix}`;
}

export const DEVELOPMENT_DEMO_CREDENTIALS = {
  organizationContext: "dev-pilot-tenant",
  email: "admin@demo.nevaxisfactos.local",
} as const;
