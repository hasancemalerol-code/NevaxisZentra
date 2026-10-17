"use client";

import { LogOut, Settings, UserRound } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useId, useState } from "react";

import { signOutSaasIdentityBrowser } from "../client/saas-identity-browser-client";
import { SAAS_IDENTITY_MESSAGES } from "../presentation/saas-identity-messages";
import { useShellPopover } from "@/components/app-shell/use-shell-popover";
import { customerMessage } from "@/lib/presentation/customer-message";

type AccountMenuProps = Readonly<{
  displayName: string;
  emailMasked?: string | null;
  organizationLabel?: string | null;
  settingsHref?: string | null;
}>;

export function AccountMenu({
  displayName,
  emailMasked = null,
  organizationLabel = null,
  settingsHref = null,
}: AccountMenuProps) {
  const router = useRouter();
  const menuId = useId();
  const [open, setOpen] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const containerRef = useShellPopover(open, () => setOpen(false));

  const handleSignOut = useCallback(async () => {
    setPending(true);
    setError(null);

    const result = await signOutSaasIdentityBrowser();
    setPending(false);

    if (result.status === "failure") {
      setError(SAAS_IDENTITY_MESSAGES.unexpectedError);
      return;
    }

    setOpen(false);
    router.push("/giris");
    router.refresh();
  }, [router]);

  const organization = organizationLabel?.trim();

  return (
    <div className="account-menu shell-anchor" ref={containerRef}>
      <button
        aria-controls={menuId}
        aria-expanded={open}
        aria-haspopup="menu"
        aria-label={`Hesap menüsü: ${displayName}`}
        className="account-placeholder"
        disabled={pending}
        onClick={() => setOpen((current) => !current)}
        type="button"
      >
        <span className="account-icon" aria-hidden="true">
          <UserRound />
        </span>
        <span className="account-copy">
          <strong>{displayName}</strong>
          <small>{organization || "Oturum açık"}</small>
        </span>
      </button>

      {open ? (
        <div
          className="shell-popover shell-popover--account"
          id={menuId}
          role="menu"
        >
          <div className="shell-account-menu__identity">
            <strong>{displayName}</strong>
            {emailMasked ? <span>{emailMasked}</span> : null}
            {organization ? <span>{organization}</span> : null}
          </div>

          {settingsHref ? (
            <Link
              className="shell-account-menu__item"
              href={settingsHref}
              onClick={() => setOpen(false)}
              role="menuitem"
            >
              <Settings aria-hidden="true" />
              <span>Ayarlar</span>
            </Link>
          ) : null}

          <button
            className="shell-account-menu__item"
            disabled={pending}
            onClick={() => void handleSignOut()}
            role="menuitem"
            type="button"
          >
            <LogOut aria-hidden="true" />
            <span>
              {pending ? "Kapatılıyor…" : SAAS_IDENTITY_MESSAGES.signOutLabel}
            </span>
          </button>
        </div>
      ) : null}

      {error ? (
        <p className="account-menu__error" role="alert">
          {customerMessage(error)}
        </p>
      ) : null}
    </div>
  );
}
