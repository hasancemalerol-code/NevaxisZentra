import Link from "next/link";

const PRIMARY_NAV = [
  { href: "/erp-entegrasyonlari/baglanti", label: "Bağlantı" },
  { href: "/erp-entegrasyonlari/senkronizasyonlar", label: "Senkronizasyon" },
  { href: "/erp-entegrasyonlari/siparisler", label: "Siparişler" },
  { href: "/erp-entegrasyonlari/stok", label: "Stok & Depolar" },
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
  { href: "/erp-entegrasyonlari/saglik", label: "Entegrasyon Sağlığı" },
  { href: "/erp-entegrasyonlari/hata-kuyrugu", label: "Hata Kuyruğu" },
] as const;

type ErpWorkspaceNavProps = Readonly<{
  activeHref: string;
}>;

function isNavItemActive(itemHref: string, activeHref: string): boolean {
  if (itemHref === activeHref) {
    return true;
  }
  if (itemHref.includes("#")) {
    return activeHref === itemHref.split("#")[0];
  }
  return activeHref.startsWith(itemHref) && itemHref !== "/erp-entegrasyonlari/baglanti";
}

function NavLink({
  href,
  label,
  activeHref,
}: Readonly<{ href: string; label: string; activeHref: string }>) {
  return (
    <Link
      href={href}
      className="erp-workspace-nav__link"
      aria-current={isNavItemActive(href, activeHref) ? "page" : undefined}
    >
      {label}
    </Link>
  );
}

export function ErpWorkspaceNav({ activeHref }: ErpWorkspaceNavProps) {
  return (
    <nav className="erp-workspace-nav erp-workspace-nav--compact" aria-label="ERP workspace">
      <div className="erp-workspace-nav__band">
        <div className="erp-workspace-nav__cluster" aria-label="Birincil ERP gezintisi">
          {PRIMARY_NAV.map((item) => (
            <NavLink key={item.href} {...item} activeHref={activeHref} />
          ))}
        </div>
        <span className="erp-workspace-nav__separator" aria-hidden="true" />
        <div className="erp-workspace-nav__cluster" aria-label="Destekleyici ERP gezintisi">
          {SECONDARY_NAV.map((item) => (
            <NavLink key={item.href} {...item} activeHref={activeHref} />
          ))}
        </div>
        <span className="erp-workspace-nav__separator" aria-hidden="true" />
        <div className="erp-workspace-nav__cluster" aria-label="İzleme gezintisi">
          {MONITORING_NAV.map((item) => (
            <NavLink key={item.href} {...item} activeHref={activeHref} />
          ))}
        </div>
      </div>
    </nav>
  );
}
