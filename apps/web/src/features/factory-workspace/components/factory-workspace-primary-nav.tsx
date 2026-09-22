import Link from "next/link";

const PRIMARY_LINKS = [
  { href: "/fabrika", label: "Genel Bakış", exact: true },
  { href: "/uretim/hatlar", label: "Hatlar" },
  { href: "/uretim/kaynaklar", label: "Makineler & Kaynaklar" },
] as const;

type FactoryWorkspacePrimaryNavProps = Readonly<{
  pathname: string;
}>;

export function FactoryWorkspacePrimaryNav({ pathname }: FactoryWorkspacePrimaryNavProps) {
  return (
    <nav
      aria-label="Fabrika çalışma alanı"
      className="factory-workspace-primary-nav"
      data-testid="factory-workspace-primary-nav"
    >
      {PRIMARY_LINKS.map((link) => {
        const active =
          "exact" in link && link.exact ? pathname === link.href : pathname.startsWith(link.href);
        return (
          <Link
            key={link.href}
            href={link.href}
            className={`factory-workspace-primary-nav__link${active ? " factory-workspace-primary-nav__link--active" : ""}`}
            aria-current={active ? "page" : undefined}
          >
            {link.label}
          </Link>
        );
      })}
    </nav>
  );
}
