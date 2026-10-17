"use client";

import { useEffect, useMemo, useState, useTransition } from "react";
import { LoaderCircle } from "lucide-react";

import {
  CHECKOUT_VERIFICATION_MESSAGE,
  formatPaymentStatus,
  paymentStatusBadgeClass,
} from "@/features/saas-billing/domain/saas-billing-labels";
import {
  loadPaymentAttemptDetailAction,
  verifyCheckoutReturnAction,
} from "@/features/saas-billing/server/saas-billing-actions";
import { PageFrame, PageHeader } from "@/components/ui/page";
import { StatePage, StatePageLink } from "@/components/ui/state-page";

type CheckoutReturnViewProps = Readonly<{
  paymentAttemptId: string | null;
  checkoutSessionToken: string | null;
}>;

export function CheckoutReturnView({ paymentAttemptId, checkoutSessionToken }: CheckoutReturnViewProps) {
  const hasReturnParams = Boolean(paymentAttemptId && checkoutSessionToken);
  const [message, setMessage] = useState(
    hasReturnParams ? CHECKOUT_VERIFICATION_MESSAGE : "",
  );
  const [status, setStatus] = useState<string | null>(null);
  const [failureCode, setFailureCode] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const idempotencyKey = useMemo(
    () => `checkout-return-${paymentAttemptId ?? "unknown"}-${checkoutSessionToken?.slice(-4) ?? "none"}`,
    [checkoutSessionToken, paymentAttemptId],
  );

  useEffect(() => {
    if (!hasReturnParams || !paymentAttemptId || !checkoutSessionToken) {
      return;
    }

    startTransition(() => {
      void verifyCheckoutReturnAction(paymentAttemptId, checkoutSessionToken, idempotencyKey).then(async (result) => {
        if (result.status === "failure") {
          setFailureCode(result.code ?? null);
          setMessage(result.message);
          return;
        }

        const detail = await loadPaymentAttemptDetailAction(paymentAttemptId);
        if (detail.status === "success") {
          setStatus(detail.value.paymentStatus);
          if (detail.value.failureCode === "saas.payment.reconciliation-review-required") {
            setMessage("Ödeme alındı ancak manuel inceleme gerekiyor.");
            return;
          }
        }

        setMessage("Ödeme sonucu doğrulandı.");
      });
    });
  }, [checkoutSessionToken, hasReturnParams, idempotencyKey, paymentAttemptId]);

  if (!hasReturnParams) {
    return (
      <PageFrame>
        <PageHeader
          eyebrow="Ayarlar"
          title="Ödeme dönüşü"
          description="Ödeme sağlayıcısından dönen sonuç bu sayfada gösterilir."
        />
        <StatePage
          kind="empty"
          inline
          title="Gösterilecek ödeme sonucu yok"
          description="Bu sayfa yalnızca ödeme sağlayıcısından yönlendirildiğinizde sonuç gösterir."
          actions={<StatePageLink href="/ayarlar" primary>Faturalandırmaya dön</StatePageLink>}
        />
      </PageFrame>
    );
  }

  return (
    <PageFrame>
      <PageHeader
        eyebrow="Ayarlar"
        title="Ödeme dönüşü"
        description="Ödeme sağlayıcısından dönen sonuç doğrulanıyor."
      />
      <section className="zx-panel" aria-live="polite">
        <p role="status" className="zx-text-body">
          {isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : null} {message}
        </p>
        {status ? (
          <p className={paymentStatusBadgeClass(status)}>{formatPaymentStatus(status)}</p>
        ) : null}
        {failureCode ? <p className="saas-admin-billing-muted">İşlem tamamlanamadı.</p> : null}
      </section>
    </PageFrame>
  );
}
