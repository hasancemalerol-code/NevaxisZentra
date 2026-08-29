import Link from "next/link";

type QualityWorkspaceNavProps = Readonly<{
  active: "overview" | "ncr" | "follow-up" | "reporting";
}>;

const NAV_ITEMS = [
  { id: "overview" as const, href: "/kalite", label: "Genel Bakış" },
  { id: "ncr" as const, href: "/kalite/ncr", label: "NCR" },
  { id: "follow-up" as const, href: "/kalite/takip-ve-gorevler", label: "Takip ve Görevler" },
  { id: "reporting" as const, href: "/kalite/raporlama", label: "Raporlama" },
];

export function QualityWorkspaceNav({ active }: QualityWorkspaceNavProps) {
  return (
    <nav className="quality-workspace-nav" aria-label="Kalite çalışma alanı">
      {NAV_ITEMS.map((item) => (
        <Link
          key={item.id}
          href={item.href}
          aria-current={active === item.id ? "page" : undefined}
          className={
            active === item.id
              ? "quality-workspace-nav__link quality-workspace-nav__link--active"
              : "quality-workspace-nav__link"
          }
        >
          {item.label}
        </Link>
      ))}
    </nav>
  );
}
