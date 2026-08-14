"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import type { convertMaintenanceRequestAction } from "../server/maintenance-writer";

type MaintenanceRequestConvertFormProps = Readonly<{
  requestId: string;
  expectedVersion: number;
  onConvert: typeof convertMaintenanceRequestAction;
}>;

export function MaintenanceRequestConvertForm({
  requestId,
  expectedVersion,
  onConvert,
}: MaintenanceRequestConvertFormProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  return (
    <form
      className="maintenance-form-stack"
      method="post"
      onSubmit={(event) => {
        event.preventDefault();
        setPending(true);
        setMessage(null);
        void onConvert({ requestId, expectedVersion }).then((result) => {
          if (result.status === "failure") {
            setPending(false);
            setMessage(result.message ?? "Bakım emrine dönüştürülemedi.");
            return;
          }

          router.push(`/bakim/emirler/${result.id}`);
          router.refresh();
        });
      }}
    >
      {message ? (
        <div className="maintenance-error-panel" role="alert">
          <p>{message}</p>
        </div>
      ) : null}

      <div className="maintenance-form-actions">
        <Button disabled={pending} type="submit" variant="primary">
          {pending ? "Dönüştürülüyor…" : "Bakım emrine dönüştür"}
        </Button>
      </div>
    </form>
  );
}
