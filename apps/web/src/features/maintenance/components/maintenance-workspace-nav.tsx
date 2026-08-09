import Link from "next/link";

type MaintenanceWorkspaceNavProps = Readonly<{
  active: "overview" | "work-orders" | "requests" | "preventive" | "planning" | "reporting";
}>;

const NAV_ITEMS = [
  { id: "overview" as const, href: "/bakim", label: "Genel Bakış" },
  { id: "work-orders" as const, href: "/bakim#aktif-bakim-isleri", label: "Bakım İşleri" },
  { id: "requests" as const, href: "/bakim#ariza-kayitlari", label: "Arıza Kayıtları" },
  {
    id: "preventive" as const,
    href: "/bakim#periyodik-bakim-planlari",
    label: "Periyodik Bakım",
  },
  { id: "planning" as const, href: "/bakim/planlama", label: "Bakım Planlama" },
  { id: "reporting" as const, href: "/bakim/raporlama", label: "Raporlama" },
];

export function MaintenanceWorkspaceNav({ active }: MaintenanceWorkspaceNavProps) {
  return (
    <nav className="maintenance-workspace-nav" aria-label="Bakım çalışma alanı">
      {NAV_ITEMS.map((item) => (
        <Link
          key={item.id}
          href={item.href}
          aria-current={active === item.id ? "page" : undefined}
          className={
            active === item.id
              ? "maintenance-workspace-nav__link maintenance-workspace-nav__link--active"
              : "maintenance-workspace-nav__link"
          }
        >
          {item.label}
        </Link>
      ))}
    </nav>
  );
}
