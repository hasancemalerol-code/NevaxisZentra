"use client";

import { LogOut, UserRound } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback, useState } from "react";

import { signOutSaasIdentityBrowser } from "../client/saas-identity-browser-client";
import { SAAS_IDENTITY_MESSAGES } from "../presentation/saas-identity-messages";

type AccountMenuProps = Readonly<{
  displayName: string;
}>;

export function AccountMenu({ displayName }: AccountMenuProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSignOut = useCallback(async () => {
    setPending(true);
    setError(null);

    const result = await signOutSaasIdentityBrowser();
    setPending(false);

    if (result.status === "failure") {
      setError(SAAS_IDENTITY_MESSAGES.unexpectedError);
      return;
    }

    router.push("/giris");
    router.refresh();
  }, [router]);

  return (
    <div className="account-menu">
      <button
        className="account-placeholder"
        type="button"
        aria-label={`Hesap: ${displayName}`}
        aria-haspopup="menu"
        disabled={pending}
      >
        <span className="account-icon" aria-hidden="true">
          <UserRound />
        </span>
        <span className="account-copy">
          <strong>{displayName}</strong>
          <small>Oturum açık</small>
        </span>
      </button>
      <button
        className="account-sign-out"
        type="button"
        onClick={() => void handleSignOut()}
        disabled={pending}
        aria-label={SAAS_IDENTITY_MESSAGES.signOutLabel}
      >
        <LogOut aria-hidden="true" />
        <span>{pending ? "Kapatılıyor…" : SAAS_IDENTITY_MESSAGES.signOutLabel}</span>
      </button>
      {error ? (
        <p className="account-menu__error" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}
