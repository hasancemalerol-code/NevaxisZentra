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

type CheckoutReturnViewProps = Readonly<{
  paymentAttemptId: string | null;
  checkoutSessionToken: string | null;
}>;

export function CheckoutReturnView({ paymentAttemptId, checkoutSessionToken }: CheckoutReturnViewProps) {
  const hasReturnParams = Boolean(paymentAttemptId && checkoutSessionToken);
  const [message, setMessage] = useState(
    hasReturnParams ? CHECKOUT_VERIFICATION_MESSAGE : "Ödeme dönüş bilgisi eksik.",
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

  return (
    <section className="saas-admin-panel" aria-live="polite">
      <h2>Ödeme dönüşü</h2>
      <p role="status">
        {isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : null} {message}
      </p>
      {status ? (
        <p className={paymentStatusBadgeClass(status)}>{formatPaymentStatus(status)}</p>
      ) : null}
      {failureCode ? <p className="saas-admin-billing-muted">İşlem tamamlanamadı.</p> : null}
    </section>
  );
}
