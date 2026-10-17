"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { customerMessage } from "@/lib/presentation/customer-message";
import type {
  completeQualityCorrectiveActionAction,
  startQualityCorrectiveActionAction,
} from "../server/quality-control-writer";

export type QualityCorrectiveActionControlsProps = Readonly<{
  actionId: string;
  actionTitle: string;
  status: string;
  version: number;
  onStart: typeof startQualityCorrectiveActionAction;
  onComplete: typeof completeQualityCorrectiveActionAction;
}>;

/** Start / complete buttons that live on the corrective action row itself, so each action is listed exactly once. */
export function QualityCorrectiveActionControls({
  actionId,
  actionTitle,
  status,
  version,
  onStart,
  onComplete,
}: QualityCorrectiveActionControlsProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  async function run(action: () => Promise<{ status: string; message?: string }>) {
    setPending(true);
    setMessage(null);
    const result = await action();
    setPending(false);
    if (result.status === "failure") {
      setMessage(result.message ?? "İşlem başarısız.");
      return;
    }
    router.refresh();
  }

  if (status !== "open" && status !== "in-progress") {
    return null;
  }

  return (
    <div className="quality-ca-item__controls">
      {status === "open" ? (
        <Button
          type="button"
          variant="secondary"
          size="small"
          disabled={pending}
          aria-label={`${actionTitle} aksiyonunu başlat`}
          onClick={() => void run(() => onStart(actionId, version))}
        >
          Başlat
        </Button>
      ) : (
        <Button
          type="button"
          variant="primary"
          size="small"
          disabled={pending}
          aria-label={`${actionTitle} aksiyonunu tamamla`}
          onClick={() => void run(() => onComplete(actionId, version, "Tamamlandı"))}
        >
          Tamamla
        </Button>
      )}
      {message ? (
        <p role="alert" className="quality-ca-item__error">
          {customerMessage(message)}
        </p>
      ) : null}
    </div>
  );
}
