import { Bell, Building2, ChevronDown, Search, UserRound } from "lucide-react";

import { IconButton } from "@/components/ui/button";

export function TopBar() {
  return (
    <header className="top-bar">
      <div className="workspace-context" aria-label="Çalışma alanı bağlamı">
        <span className="workspace-label">Çalışma alanı</span>
        <button className="workspace-selector" type="button" aria-label="Şirket ve tesis seçimi — işlev ileriki pakette eklenecek">
          <Building2 aria-hidden="true" />
          <span>Şirket / tesis seçimi</span>
          <ChevronDown aria-hidden="true" />
        </button>
      </div>

      <div className="top-bar-actions" aria-label="Uygulama araçları">
        <button className="search-placeholder" type="button" aria-label="Arama — işlev ileriki pakette eklenecek">
          <Search aria-hidden="true" />
          <span>Operasyonlarda ara</span>
          <kbd>⌘ K</kbd>
        </button>
        <IconButton label="Bildirimler — işlev ileriki pakette eklenecek">
          <Bell />
        </IconButton>
        <span className="top-bar-divider" aria-hidden="true" />
        <button className="account-placeholder" type="button" aria-label="Hesap menüsü — kimlik doğrulama arayüzü henüz eklenmedi">
          <span className="account-icon" aria-hidden="true"><UserRound /></span>
          <span className="account-copy"><strong>Hesap</strong><small>Oturum bağlamı</small></span>
          <ChevronDown aria-hidden="true" />
        </button>
      </div>
    </header>
  );
}
