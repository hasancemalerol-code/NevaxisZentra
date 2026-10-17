"use client";

import Link from "next/link";
import { LogIn } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { usePathname } from "next/navigation";

/** Reports whether the signed-in session is gone, using the same identity endpoint the shell itself relies on. */
export async function isSessionExpired(probe: typeof fetch = fetch): Promise<boolean> {
  try {
    const response = await probe("/api/identity/me", { cache: "no-store", credentials: "same-origin" });
    return response.status === 401;
  } catch {
    // The probe could not reach the service: that is an outage, not an expired session.
    return false;
  }
}

/**
 * A client-side server-action call whose session cookie is gone fails with a network-level error instead of a
 * result. This guard turns that into one calm, actionable notice (sign in again) instead of a broken screen.
 */
export function SessionExpiryGuard() {
  const pathname = usePathname();
  const [expired, setExpired] = useState(false);

  const check = useCallback(async () => {
    if (await isSessionExpired()) {
      setExpired(true);
    }
  }, []);

  useEffect(() => {
    const onRejection = () => {
      void check();
    };
    window.addEventListener("unhandledrejection", onRejection);
    return () => window.removeEventListener("unhandledrejection", onRejection);
  }, [check]);

  if (!expired) {
    return null;
  }

  return (
    <div className="zx-session-expired" role="alertdialog" aria-modal="false" aria-labelledby="session-expired-title" data-testid="session-expired-notice">
      <div className="zx-session-expired__card">
        <span className="zx-state-icon" aria-hidden="true"><LogIn /></span>
        <div className="zx-session-expired__copy">
          <h2 id="session-expired-title">Oturumunuzun süresi doldu</h2>
          <p>Güvenliğiniz için oturumunuz sonlandırıldı. Devam etmek için yeniden giriş yapın; bu sayfaya geri döneceksiniz.</p>
        </div>
        <Link
          className="button button-medium button-primary"
          href={`/giris?reason=session-expired&next=${encodeURIComponent(pathname || "/")}`}
        >
          Yeniden giriş yap
        </Link>
      </div>
    </div>
  );
}
