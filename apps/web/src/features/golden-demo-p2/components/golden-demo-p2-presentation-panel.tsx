"use client";

import { useRouter } from "next/navigation";
import { useTransition, useState } from "react";

import type { GoldenDemoP2PresentationStatusApi } from "../server/golden-demo-p2-api-client";
import { goldenDemoResetPilotEventAction } from "../server/golden-demo-p2-actions";
import { customerMessage } from "@/lib/presentation/customer-message";

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
        setMessage("Senaryo sıfırlandı.");
        router.refresh();
      } else {
        setMessage(result.message ?? "Sıfırlama başarısız.");
      }
    });
  }

  return (
    <header className="golden-demo-status-bar" data-testid="golden-demo-sales-control">
      <div className="golden-demo-status-bar__intro">
        <p className="commint-executive__eyebrow">ÜRETİM KURTARMA · ALTIN DEMO</p>
        <p className="golden-demo-status-bar__step" data-testid="golden-demo-p2-step">
          Adım {presentation.stepNumber} / {presentation.totalSteps}
        </p>
        <span className="golden-demo-status-bar__active">Aktif</span>
      </div>
      <div className="golden-demo-status-bar__actions">
        <button
          type="button"
          className="commint-button"
          disabled={pending}
          data-testid="golden-demo-p2-reset"
          onClick={resetDemo}
        >
          {pending ? "Sıfırlanıyor…" : "Senaryoyu Sıfırla"}
        </button>
      </div>
      {message ? (
        <p
          role="status"
          className={`golden-demo-feedback ${message.includes("başarısız") ? "golden-demo-feedback--error" : "golden-demo-feedback--success"}`}
          data-testid="golden-demo-reset-status"
        >
          {customerMessage(message)}
        </p>
      ) : null}
    </header>
  );
}
