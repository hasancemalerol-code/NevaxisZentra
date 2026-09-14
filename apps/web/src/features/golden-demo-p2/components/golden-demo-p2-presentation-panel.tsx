"use client";

import { useTransition, useState } from "react";

import type { GoldenDemoP2PresentationStatusApi } from "../server/golden-demo-p2-api-client";
import { goldenDemoResetPilotEventAction } from "../server/golden-demo-p2-actions";

export function GoldenDemoP2PresentationPanel({
  presentation,
  eventId,
}: Readonly<{
  presentation: GoldenDemoP2PresentationStatusApi;
  eventId: string;
}>) {
  const [message, setMessage] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  return (
    <section className="commint-panel" data-testid="golden-demo-p2-presentation">
      <h2>Altın Demo Sunumu</h2>
      <p data-testid="golden-demo-p2-step">
        Adım {presentation.stepNumber}/{presentation.totalSteps}: {presentation.presentationStepLabel}
      </p>
      <p>Olay durumu: {presentation.eventStatus}</p>
      <p>+{presentation.productionRecoveryGoodParts} iyi parça / vardiya fırsatı</p>
      {presentation.canReset ? (
        <button
          type="button"
          disabled={pending}
          data-testid="golden-demo-p2-reset"
          onClick={() => {
            setMessage(null);
            startTransition(async () => {
              const result = await goldenDemoResetPilotEventAction(eventId);
              setMessage(result.status === "success" ? "Demo sıfırlandı." : result.message);
            });
          }}
        >
          Demo Akışını Sıfırla
        </button>
      ) : null}
      {message ? <p role="status">{message}</p> : null}
    </section>
  );
}
