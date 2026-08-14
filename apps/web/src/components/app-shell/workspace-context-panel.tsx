"use client";

import { Building2 } from "lucide-react";

type WorkspaceContextPanelProps = Readonly<{
  organizationLabel: string | null;
  panelId: string;
}>;

export function WorkspaceContextPanel({
  organizationLabel,
  panelId,
}: WorkspaceContextPanelProps) {
  const displayOrganization = organizationLabel?.trim() || "Bağlı kuruluş";

  return (
    <div
      className="shell-popover shell-popover--workspace"
      id={panelId}
      role="dialog"
      aria-labelledby={`${panelId}-title`}
    >
      <div className="shell-popover__header">
        <Building2 aria-hidden="true" />
        <div>
          <p className="shell-popover__eyebrow">Çalışma alanı</p>
          <h2 className="shell-popover__title" id={`${panelId}-title`}>
            Mevcut bağlam
          </h2>
        </div>
      </div>

      <dl className="shell-popover__facts">
        <div>
          <dt>Kuruluş</dt>
          <dd>{displayOrganization}</dd>
        </div>
        <div>
          <dt>Tesis</dt>
          <dd>Oturum bağlamındaki aktif tesis</dd>
        </div>
      </dl>

      <p className="shell-popover__notice">
        Kuruluş veya tesis değiştirme henüz kullanıma hazır değil. Bu panel yalnızca
        mevcut oturum bağlamını gösterir; başka kuruluş veya tesis seçilemez.
      </p>
    </div>
  );
}
