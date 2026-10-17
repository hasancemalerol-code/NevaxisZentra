import { CircleCheck, ShieldAlert } from "lucide-react";
import Link from "next/link";

import {
  formatAccessDeniedHeadline,
  formatAccessDeniedSummary,
  formatSubscriptionStatus,
  isSubscriptionHealthy,
} from "../domain/saas-subscription-labels";
import type {
  SubscriptionAccessDenialReason,
  TenantSubscriptionCurrent,
} from "../domain/saas-subscription-types";
import "./subscription-access-denied.css";

type SubscriptionAccessDeniedViewProps = Readonly<{
  subscription: TenantSubscriptionCurrent | null;
  denialReason?: SubscriptionAccessDenialReason;
  canAccessSettings?: boolean;
}>;

export function SubscriptionAccessDeniedView({
  subscription,
  denialReason = "paid-application",
  canAccessSettings = false,
}: SubscriptionAccessDeniedViewProps) {
  const healthy = isSubscriptionHealthy(subscription);
  const statusLabel = formatSubscriptionStatus(subscription?.subscription.subscriptionStatus);

  return (
    <section className="subscription-access-denied" aria-labelledby="subscription-access-denied-title">
      <article className="subscription-access-denied__card">
        <p className="subscription-access-denied__eyebrow">ABONELİK ERİŞİMİ</p>
        <h1 id="subscription-access-denied-title">
          {formatAccessDeniedHeadline(denialReason, subscription)}
        </h1>
        <p className="subscription-access-denied__summary">
          {formatAccessDeniedSummary(subscription, denialReason)}
        </p>
        {subscription ? (
          <p
            className={
              healthy
                ? "subscription-access-denied__status subscription-access-denied__status--ok"
                : "subscription-access-denied__status"
            }
            role="status"
          >
            {healthy ? (
              <CircleCheck aria-hidden="true" size={16} />
            ) : (
              <ShieldAlert aria-hidden="true" size={16} />
            )}
            Abonelik durumu: {statusLabel}
          </p>
        ) : null}
        <div className="subscription-access-denied__actions">
          <Link
            className="subscription-access-denied__link subscription-access-denied__link--primary"
            href="/uygulamalar"
          >
            Yeniden Dene
          </Link>
          {canAccessSettings ? (
            <Link className="subscription-access-denied__link" href="/ayarlar">
              Abonelik Ayarları
            </Link>
          ) : null}
        </div>
        <p className="subscription-access-denied__hint">
          Oturumu kapatmak için sağ üstteki hesap menüsünü kullanabilirsiniz.
        </p>
      </article>
    </section>
  );
}
