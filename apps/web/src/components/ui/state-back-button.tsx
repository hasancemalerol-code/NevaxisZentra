"use client";

import { ArrowLeft } from "lucide-react";
import { useRouter } from "next/navigation";

/** Safe way out of a missing record when the page has no explicit list to return to. */
export function StateBackButton() {
  const router = useRouter();
  return (
    <button type="button" className="button button-medium button-primary" onClick={() => router.back()}>
      <ArrowLeft aria-hidden="true" size={16} />
      Geri dön
    </button>
  );
}
