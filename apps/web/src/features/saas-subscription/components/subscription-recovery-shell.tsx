"use client";

import type { ReactNode } from "react";

import { AccountMenu } from "@/features/saas-identity/components/account-menu";

import "./subscription-recovery-shell.css";

type SubscriptionRecoveryShellProps = Readonly<{
  children: ReactNode;
  accountDisplayName?: string | null;
  emailMasked?: string | null;
  organizationLabel?: string | null;
  settingsHref?: string | null;
}>;

export function SubscriptionRecoveryShell({
  children,
  accountDisplayName = null,
  emailMasked = null,
  organizationLabel = null,
  settingsHref = null,
}: SubscriptionRecoveryShellProps) {
  return (
    <div className="subscription-recovery-shell">
      <a className="skip-link" href="#main-content">
        Ana içeriğe geç
      </a>
      <header className="subscription-recovery-shell__header">
        <p className="subscription-recovery-shell__brand">NevaxisFactOS</p>
        <AccountMenu
          displayName={accountDisplayName?.trim() || "Hesap"}
          emailMasked={emailMasked}
          organizationLabel={organizationLabel}
          settingsHref={settingsHref}
        />
      </header>
      <main className="subscription-recovery-shell__main" id="main-content" tabIndex={-1}>
        {children}
      </main>
    </div>
  );
}
