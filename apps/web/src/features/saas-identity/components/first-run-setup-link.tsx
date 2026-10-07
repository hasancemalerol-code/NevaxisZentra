"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { fetchFirstRunAvailability } from "../lib/first-run-enrollment";

/** Shown on the sign-in page only while the one-time first-account enrollment is open on this device. */
export function FirstRunSetupLink() {
  const [available, setAvailable] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void fetchFirstRunAvailability().then((value) => {
      if (!cancelled) {
        setAvailable(value);
      }
    });
    return () => {
      cancelled = true;
    };
  }, []);

  if (!available) {
    return null;
  }

  return (
    <p className="auth-form__links">
      <Link href="/kurulum">İlk kurulum</Link>
    </p>
  );
}
