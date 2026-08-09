import Link from "next/link";

const NAV_ITEMS = [
  { href: "/erp-entegrasyonlari/baglanti", label: "Bağlantı" },
  { href: "/erp-entegrasyonlari/senkronizasyonlar", label: "Senkronizasyon" },
  { href: "/erp-entegrasyonlari/siparisler", label: "Siparişler" },
  { href: "/erp-entegrasyonlari/stok", label: "Stok & Depolar" },
  { href: "/erp-entegrasyonlari/satin-alma", label: "Satın Alma" },
  { href: "/erp-entegrasyonlari/eslestirme", label: "Eşleştirme" },
  { href: "/erp-entegrasyonlari/hata-kuyrugu", label: "Hata Kuyruğu" },
  { href: "/erp-entegrasyonlari/saglik", label: "Entegrasyon Sağlığı" },
] as const;

type ErpWorkspaceNavProps = Readonly<{
  activeHref: string;
}>;

export function ErpWorkspaceNav({ activeHref }: ErpWorkspaceNavProps) {
  return (
    <nav className="erp-workspace-nav" aria-label="ERP workspace">
      {NAV_ITEMS.map((item) => (
        <Link
          key={item.href}
          href={item.href}
          aria-current={item.href === activeHref ? "page" : undefined}
        >
          {item.label}
        </Link>
      ))}
    </nav>
  );
}
