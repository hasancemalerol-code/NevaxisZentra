import { SubNav } from "@/components/ui/sub-nav";

import { isErpNavItemActive } from "../presentation/erp-nav-active";

const PRIMARY_NAV = [
  { href: "/erp-entegrasyonlari/baglanti", label: "Bağlantı" },
  { href: "/erp-entegrasyonlari/senkronizasyonlar", label: "Eşitleme" },
  { href: "/erp-entegrasyonlari/siparisler", label: "Siparişler" },
  { href: "/erp-entegrasyonlari/stok", label: "Stok ve Depolar" },
  { href: "/erp-entegrasyonlari/satin-alma", label: "Satın Alma" },
  { href: "/erp-entegrasyonlari/uretim", label: "Üretim" },
] as const;

const SECONDARY_NAV = [
  { href: "/erp-entegrasyonlari/baglanti#erp-musteriler", label: "Müşteriler" },
  { href: "/erp-entegrasyonlari/baglanti#erp-urunler", label: "Ürünler" },
  { href: "/erp-entegrasyonlari/eslestirme", label: "Eşleştirme" },
  { href: "/erp-entegrasyonlari/belgeler", label: "Belgeler" },
  { href: "/erp-entegrasyonlari/onayli-islemler", label: "Onaylı İşlemler" },
] as const;

const MONITORING_NAV = [
  { href: "/erp-entegrasyonlari/saglik", label: "Bağlantı Sağlığı" },
  { href: "/erp-entegrasyonlari/hata-kuyrugu", label: "Hata Kuyruğu" },
] as const;

type ErpWorkspaceNavProps = Readonly<{
  activeHref: string;
}>;

/** Section name for a list route, used by breadcrumbs. */
export function getErpSectionLabel(href: string): string | undefined {
  return [...PRIMARY_NAV, ...SECONDARY_NAV, ...MONITORING_NAV].find((item) => item.href === href)?.label;
}

function toItems(items: readonly { href: string; label: string }[], activeHref: string) {
  return items.map((item) => ({ ...item, active: isErpNavItemActive(item.href, activeHref) }));
}

export function ErpWorkspaceNav({ activeHref }: ErpWorkspaceNavProps) {
  return (
    <SubNav
      label="ERP entegrasyonu bölümleri"
      groups={[
        { label: "Veri akışı", items: toItems(PRIMARY_NAV, activeHref) },
        { label: "Kayıtlar", items: toItems(SECONDARY_NAV, activeHref) },
        { label: "İzleme", items: toItems(MONITORING_NAV, activeHref) },
      ]}
    />
  );
}
