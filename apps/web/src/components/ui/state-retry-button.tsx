"use client";

import { RefreshCw } from "lucide-react";
import { useRouter } from "next/navigation";
import { useTransition } from "react";

/** Re-requests the current route's server data. Offered only on states where trying again can help. */
export function StateRetryButton({ primary = true }: Readonly<{ primary?: boolean }>) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  return (
    <button
      type="button"
      className={`button button-medium ${primary ? "button-primary" : "button-secondary"}`}
      disabled={pending}
      aria-busy={pending}
      onClick={() => startTransition(() => router.refresh())}
    >
      <RefreshCw aria-hidden="true" size={16} />
      {pending ? "Yenileniyor…" : "Yeniden Dene"}
    </button>
  );
}
