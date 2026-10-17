"use client";

import { usePathname } from "next/navigation";

import { SubNav } from "@/components/ui/sub-nav";

const ITEMS = [
  { href: "/teams", label: "Kanallar", exact: true },
  { href: "/teams/is-kutum", label: "İş kutusu" },
  { href: "/teams/bildirimler", label: "Bildirimler" },
  { href: "/teams/ozel-mesajlar", label: "Özel mesajlar" },
  { href: "/teams/onaylar", label: "Onay istekleri" },
  { href: "/teams/devretme", label: "Vardiya devri", handover: true },
] as const;

/** Teams sub-navigation shared by every Teams screen; the active tab follows the current route. */
export function TeamsSubNav({ canReadHandover }: Readonly<{ canReadHandover: boolean }>) {
  const pathname = usePathname();
  return (
    <SubNav
      label="Operasyon iletişimi bölümleri"
      groups={[
        {
          items: ITEMS.filter((item) => !("handover" in item) || canReadHandover).map((item) => ({
            href: item.href,
            label: item.label,
            active: "exact" in item ? pathname === item.href : pathname === item.href || pathname.startsWith(`${item.href}/`),
          })),
        },
      ]}
    />
  );
}
