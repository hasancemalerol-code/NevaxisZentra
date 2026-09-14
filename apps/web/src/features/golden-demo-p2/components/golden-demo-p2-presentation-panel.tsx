"use client";

import { useRouter } from "next/navigation";
import { useTransition, useState } from "react";

import { getGoldenDemoEventStatusLabel } from "@/features/golden-demo-p1/domain/golden-demo-p1-labels";
import type { GoldenDemoP2PresentationStatusApi } from "../server/golden-demo-p2-api-client";
import { goldenDemoResetPilotEventAction } from "../server/golden-demo-p2-actions";

export function GoldenDemoP2PresentationPanel({
  presentation,
  eventId,
}: Readonly<{
  presentation: GoldenDemoP2PresentationStatusApi;
  eventId: string;
}>) {
  const router = useRouter();
  const [message, setMessage] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function resetDemo() {
    const confirmed = window.confirm(
      "Altın Demo akışını sıfırlamak istiyor musunuz? Onay, plan ve doğrulama durumu başlangıç noktasına döner.",
    );
    if (!confirmed) {
      return;
    }

    setMessage(null);
    startTransition(async () => {
      const result = await goldenDemoResetPilotEventAction(eventId);
      if (result.status === "success") {
        setMessage("Altın Demo sıfırlandı. Sunuma başlayabilirsiniz.");
        router.refresh();
      } else {
        setMessage(result.message ?? "Sıfırlama başarısız.");
      }
    });
  }

  const isReadyToStart = presentation.eventStatus === "active";

  return (
    <section className="commint-panel" data-testid="golden-demo-sales-control">
      <h2>Altın Demo Kontrolü</h2>
      <p data-testid="golden-demo-p2-step">
        Adım {presentation.stepNumber}/{presentation.totalSteps}: {presentation.presentationStepLabel}
      </p>
      <p>
        Durum: {getGoldenDemoEventStatusLabel(presentation.eventStatus)}
        {isReadyToStart ? " · Sunum başlangıç noktası hazır" : null}
      </p>
      <p>+{presentation.productionRecoveryGoodParts} iyi parça / vardiya fırsatı</p>
      <div className="commint-action-row">
        <button
          type="button"
          disabled={pending}
          data-testid="golden-demo-p2-reset"
          onClick={resetDemo}
        >
          {pending ? "Sıfırlanıyor…" : "Altın Demo Sıfırla"}
        </button>
      </div>
      {message ? (
        <p role="status" data-testid="golden-demo-reset-status">
          {message}
        </p>
      ) : null}
      <p className="commint-table__secondary">
        Sunumdan önce bu sayfayı açın: /golden-demo/uretim-kurtarma
      </p>
    </section>
  );
}
