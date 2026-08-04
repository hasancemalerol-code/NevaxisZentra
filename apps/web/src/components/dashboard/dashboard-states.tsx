import {
  AlertTriangle,
  DatabaseZap,
  RefreshCcw,
  SearchX,
} from "lucide-react";

interface DashboardStateProps {
  title?: string;
  description?: string;
}

interface DashboardErrorStateProps extends DashboardStateProps {
  retryLabel?: string;
}

export function DashboardLoadingState() {
  return (
    <section
      aria-busy="true"
      aria-label="Dashboard yükleniyor"
      className="dashboard-state dashboard-state--loading"
    >
      <div className="dashboard-state__header">
        <div className="dashboard-state__skeleton dashboard-state__skeleton--eyebrow" />
        <div className="dashboard-state__skeleton dashboard-state__skeleton--title" />
        <div className="dashboard-state__skeleton dashboard-state__skeleton--subtitle" />
      </div>

      <div className="dashboard-state__metric-grid">
        {Array.from({ length: 6 }).map((_, index) => (
          <div
            className="dashboard-state__metric-skeleton"
            key={`metric-skeleton-${index}`}
          >
            <div className="dashboard-state__skeleton dashboard-state__skeleton--small" />
            <div className="dashboard-state__skeleton dashboard-state__skeleton--value" />
            <div className="dashboard-state__skeleton dashboard-state__skeleton--line" />
          </div>
        ))}
      </div>

      <div className="dashboard-state__panel-grid">
        <div className="dashboard-state__panel-skeleton">
          <div className="dashboard-state__skeleton dashboard-state__skeleton--panel-title" />

          {Array.from({ length: 4 }).map((_, index) => (
            <div
              className="dashboard-state__skeleton dashboard-state__skeleton--panel-line"
              key={`panel-line-${index}`}
            />
          ))}
        </div>

        <div className="dashboard-state__panel-skeleton">
          <div className="dashboard-state__skeleton dashboard-state__skeleton--panel-title" />

          {Array.from({ length: 3 }).map((_, index) => (
            <div
              className="dashboard-state__skeleton dashboard-state__skeleton--panel-line"
              key={`secondary-panel-line-${index}`}
            />
          ))}
        </div>
      </div>

      <span className="visually-hidden">
        Operasyon Merkezi önizleme verileri hazırlanıyor.
      </span>
    </section>
  );
}

export function DashboardEmptyState({
  title = "Henüz operasyon verisi bulunmuyor",
  description = "Entegrasyonlar etkinleştirildiğinde üretim, risk, makine ve bakım sinyalleri bu alanda görüntülenecektir.",
}: DashboardStateProps) {
  return (
    <section
      aria-labelledby="dashboard-empty-state-title"
      className="dashboard-state dashboard-state--empty"
    >
      <div aria-hidden="true" className="dashboard-state__icon">
        <SearchX size={28} strokeWidth={1.7} />
      </div>

      <div className="dashboard-state__copy">
        <p className="dashboard-state__eyebrow">
          Operasyon görünümü
        </p>

        <h2
          className="dashboard-state__title"
          id="dashboard-empty-state-title"
        >
          {title}
        </h2>

        <p className="dashboard-state__description">
          {description}
        </p>
      </div>

      <div className="dashboard-state__note">
        <DatabaseZap aria-hidden="true" size={16} strokeWidth={1.8} />

        <span>
          Gerçek API ve entegrasyon bağlantıları sonraki paketlerde
          eklenecektir.
        </span>
      </div>
    </section>
  );
}

export function DashboardErrorState({
  title = "Operasyon görünümü yüklenemedi",
  description = "Dashboard verileri şu anda görüntülenemiyor. Bu durum yalnızca arayüz önizlemesidir ve gerçek bir servis hatasını temsil etmez.",
  retryLabel = "Yeniden dene",
}: DashboardErrorStateProps) {
  return (
    <section
      aria-labelledby="dashboard-error-state-title"
      className="dashboard-state dashboard-state--error"
      role="alert"
    >
      <div aria-hidden="true" className="dashboard-state__icon">
        <AlertTriangle size={28} strokeWidth={1.7} />
      </div>

      <div className="dashboard-state__copy">
        <p className="dashboard-state__eyebrow">
          Görüntüleme sorunu
        </p>

        <h2
          className="dashboard-state__title"
          id="dashboard-error-state-title"
        >
          {title}
        </h2>

        <p className="dashboard-state__description">
          {description}
        </p>
      </div>

      <button
        className="dashboard-state__retry"
        disabled
        title="Gerçek yeniden deneme davranışı API entegrasyonu sonrasında etkinleştirilecektir."
        type="button"
      >
        <RefreshCcw aria-hidden="true" size={16} strokeWidth={1.8} />
        {retryLabel}
      </button>
    </section>
  );
}
