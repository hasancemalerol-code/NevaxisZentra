import { CalendarDays, Clock3, RefreshCw } from "lucide-react";

import type { DashboardHeaderData } from "@/lib/mock-data/dashboard";

interface DashboardHeaderProps {
  data: DashboardHeaderData;
}

export function DashboardHeader({ data }: DashboardHeaderProps) {
  return (
    <header className="dashboard-header">
      <div className="dashboard-header__main">
        <div className="dashboard-header__eyebrow">
          <span className="dashboard-header__preview-badge">
            <span
              aria-hidden="true"
              className="dashboard-header__preview-indicator"
            />
            {data.previewLabel}
          </span>

          <span className="dashboard-header__context">
            <CalendarDays aria-hidden="true" size={15} strokeWidth={1.8} />
            {data.reportingDate}
          </span>

          <span className="dashboard-header__context">
            <Clock3 aria-hidden="true" size={15} strokeWidth={1.8} />
            {data.shiftLabel}
          </span>
        </div>

        <div className="dashboard-header__copy">
          <h1 className="dashboard-header__title">{data.title}</h1>

          <p className="dashboard-header__subtitle">{data.subtitle}</p>
        </div>
      </div>

      <div className="dashboard-header__actions">
        <span className="dashboard-header__updated">
          <RefreshCw aria-hidden="true" size={14} strokeWidth={1.8} />
          {data.lastUpdatedLabel}
        </span>

        <button
          className="dashboard-header__review-button"
          disabled
          title="Gerçek plan inceleme akışı sonraki paketlerde etkinleştirilecektir."
          type="button"
        >
          Planı incele
          <span className="dashboard-header__button-label">Yakında</span>
        </button>
      </div>
    </header>
  );
}
