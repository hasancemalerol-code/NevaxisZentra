"use client";

import { useEffect, useState } from "react";

import { isSessionExpired } from "@/components/app-shell/session-expiry-guard";
import { StatePage, StatePageLink } from "@/components/ui/state-page";

/** Route-level error boundary: customers get a calm retry state with an opaque support reference, never the exception text. */
export default function ApplicationError({ error, reset }: Readonly<{ error: Error & { digest?: string }; reset: () => void }>) {
  const [expired, setExpired] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void isSessionExpired().then((value) => {
      if (!cancelled) setExpired(value);
    });
    return () => {
      cancelled = true;
    };
  }, [error]);

  if (expired) {
    return (
      <StatePage
        kind="forbidden"
        title="Oturumunuzun süresi doldu"
        description="Güvenliğiniz için oturumunuz sonlandırıldı. Devam etmek için yeniden giriş yapın."
        actions={<StatePageLink href="/giris?reason=session-expired" primary>Yeniden giriş yap</StatePageLink>}
      />
    );
  }

  return (
    <StatePage
      kind="unavailable"
      title="Bu sayfa şu anda yüklenemedi"
      description="Gerekli verilere ulaşılamadı veya beklenmeyen bir sorun oluştu. Sayfayı yeniden deneyin; sorun sürerse destek ekibine aşağıdaki referansı iletin."
      reference={error.digest}
      actions={
        <>
          <button type="button" className="button button-medium button-primary" onClick={() => reset()}>
            Yeniden Dene
          </button>
          <StatePageLink href="/">Bugün ekranına dön</StatePageLink>
        </>
      }
    />
  );
}
