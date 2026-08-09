import Link from "next/link";

const NAV_GROUPS = [
  {
    label: "Genel",
    items: [
      { href: "/erp-entegrasyonlari/baglanti", label: "Bağlantı" },
      { href: "/erp-entegrasyonlari/senkronizasyonlar", label: "Senkronizasyon" },
    ],
  },
  {
    label: "Gelen Veri",
    items: [
      { href: "/erp-entegrasyonlari/baglanti#erp-musteriler", label: "Müşteriler" },
      { href: "/erp-entegrasyonlari/baglanti#erp-urunler", label: "Ürünler" },
      { href: "/erp-entegrasyonlari/siparisler", label: "Siparişler" },
      { href: "/erp-entegrasyonlari/eslestirme", label: "Eşleştirme" },
    ],
  },
  {
    label: "Operasyonel Görünürlük",
    items: [
      { href: "/erp-entegrasyonlari/stok", label: "Stok & Depolar" },
      { href: "/erp-entegrasyonlari/satin-alma", label: "Satın Alma" },
      { href: "/erp-entegrasyonlari/uretim", label: "Üretim" },
      { href: "/erp-entegrasyonlari/belgeler", label: "Belgeler" },
    ],
  },
  {
    label: "Kontrollü Çıkış",
    items: [{ href: "/erp-entegrasyonlari/onayli-islemler", label: "Onaylı İşlemler" }],
  },
  {
    label: "İzleme",
    items: [
      { href: "/erp-entegrasyonlari/saglik", label: "Entegrasyon Sağlığı" },
      { href: "/erp-entegrasyonlari/hata-kuyrugu", label: "Hata Kuyruğu" },
    ],
  },
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
  return false;
}

export function ErpWorkspaceNav({ activeHref }: ErpWorkspaceNavProps) {
  return (
    <nav className="erp-workspace-nav" aria-label="ERP workspace">
      {NAV_GROUPS.map((group) => (
        <div key={group.label} className="erp-workspace-nav__group">
          <p className="erp-workspace-nav__label">{group.label}</p>
          {group.items.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              aria-current={isNavItemActive(item.href, activeHref) ? "page" : undefined}
            >
              {item.label}
            </Link>
          ))}
        </div>
      ))}
    </nav>
  );
}
