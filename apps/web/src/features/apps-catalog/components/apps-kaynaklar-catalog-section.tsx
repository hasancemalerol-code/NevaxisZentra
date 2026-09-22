import Link from "next/link";
import { ArrowUpRight, Boxes, Cpu } from "lucide-react";

import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";

const KAYNAKLAR_LINKS = [
  {
    label: "Makineler & Üretim Kaynakları",
    href: "/uretim/kaynaklar",
    permissionKey: requiredRoutePermission("kaynaklar"),
    icon: Cpu,
    testId: "apps-kaynaklar-production-resources",
  },
  {
    label: "Kalıp & Takımlar",
    href: "/kaliplar",
    permissionKey: "mold.read",
    icon: Boxes,
    testId: "apps-kaynaklar-molds",
  },
];

export async function AppsKaynaklarCatalogSection() {
  const access = await fetchCurrentEffectiveAccess();
  const visible = KAYNAKLAR_LINKS.filter((item) => hasGrantedPermission(access, item.permissionKey));

  if (visible.length === 0) {
    return null;
  }

  return (
    <section aria-label="Kaynaklar" className="fact-section" data-testid="apps-kaynaklar-catalog-section">
      <header className="fact-section__header">
        <h2 className="fact-section__title">Kaynaklar</h2>
      </header>
      <div className="fact-section__body apps-catalog-grid">
        {visible.map((item) => {
          const Icon = item.icon;
          return (
            <Link key={item.href} className="apps-catalog-card" data-testid={item.testId} href={item.href}>
              <Icon aria-hidden="true" size={16} />
              <div>
                <h3>{item.label}</h3>
                <p>{item.href}</p>
              </div>
              <ArrowUpRight aria-hidden="true" size={16} />
            </Link>
          );
        })}
      </div>
    </section>
  );
}
