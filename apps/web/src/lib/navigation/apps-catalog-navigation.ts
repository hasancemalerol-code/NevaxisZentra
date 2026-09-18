import { primaryNavigation } from "./primary-navigation";
import { HIDDEN_PLACEHOLDER_NAV_HREFS } from "./workflow-primary-navigation";
import type { SerializableNavigationGroup, SerializableNavigationItem } from "./navigation-types";

/** Routes surfaced in primary/contextual homes — excluded from Apps catalog. */
const PRIMARY_SURFACE_HREFS = new Set([
  "/",
  "/fabrika",
  "/planlama",
  "/isler",
  "/uygulamalar",
  "/dijital-is-gucu",
  "/dijital-is-gucu/onaylar",
  "/dijital-is-gucu/sonuclar",
  "/teams",
  "/dashboard",
  "/vardiyalar",
  "/fabrika-karar-merkezi",
  "/yonetici-komuta-merkezi",
  "/operasyon/yonetim-merkezi",
  "/fabrika-zekasi",
  "/sunum-modu",
  "/uretim",
  "/uretim/hatlar",
  "/uretim/kaynaklar",
  "/makineler",
  "/proses-oee",
  "/fabrika-agi",
  "/siparisler",
]);

function isAppsCatalogItem(item: SerializableNavigationItem): boolean {
  if (HIDDEN_PLACEHOLDER_NAV_HREFS.has(item.href)) {
    return false;
  }

  if (PRIMARY_SURFACE_HREFS.has(item.href)) {
    return false;
  }

  return true;
}

/** Specialist/admin modules for Apps / Uygulamalar surface. */
export const appsCatalogNavigation: readonly SerializableNavigationGroup[] = primaryNavigation
  .map((group) => ({
    label: group.label,
    items: group.items.filter(isAppsCatalogItem),
  }))
  .filter((group) => group.items.length > 0);

export const appsCatalogItems = appsCatalogNavigation.flatMap((group) => group.items);
