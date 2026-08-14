import { Bell, Building2, ChevronDown, Search } from "lucide-react";

import { IconButton } from "@/components/ui/button";
import { AccountMenu } from "@/features/saas-identity/components/account-menu";

type TopBarProps = Readonly<{
  accountDisplayName?: string | null;
}>;

export function TopBar({ accountDisplayName = null }: TopBarProps) {
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
        {accountDisplayName ? (
          <AccountMenu displayName={accountDisplayName} />
        ) : (
          <span className="account-copy account-copy--placeholder">
            <strong>Hesap</strong>
            <small>Oturum bağlamı</small>
          </span>
        )}
      </div>
    </header>
  );
}
