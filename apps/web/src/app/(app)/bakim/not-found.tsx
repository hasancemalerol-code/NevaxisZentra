import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { StatePage, StatePageLink } from "@/components/ui/state-page";
import "@/features/maintenance/components/maintenance-workspace.css";

/** Missing maintenance record: the same header as the create pages (back link, eyebrow, title, subtitle) above one calm card. */
export default function MaintenanceNotFound() {
  return (
    <div className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <Link className="maintenance-back-link" href="/bakim">
          <ArrowLeft aria-hidden="true" />
          Bakım ana sayfası
        </Link>
        <div className="maintenance-page-header__intro">
          <p>BAKIM VE ARIZA</p>
          <h1>Kayıt Bulunamadı</h1>
          <span>Bu bakım kaydı mevcut değil, kaldırılmış ya da yetki alanınızın dışında.</span>
        </div>
      </header>
      <StatePage
        inline
        kind="not-found"
        title="Bu kayıt listede yok"
        description="Adresi kontrol edin ya da bakım ana sayfasından kaydı yeniden bulun."
        actions={
          <StatePageLink href="/bakim" primary>
            Bakım ana sayfasına dön
          </StatePageLink>
        }
      />
    </div>
  );
}
