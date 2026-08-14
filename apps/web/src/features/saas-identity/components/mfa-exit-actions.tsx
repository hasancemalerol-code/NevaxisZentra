"use client";

import { useState } from "react";

import { SAAS_IDENTITY_MESSAGES } from "../presentation/saas-identity-messages";
import { AuthFeedback } from "./auth-feedback";

type MfaExitActionsProps = Readonly<{
  initialError?: string | null;
}>;

export function MfaExitActions({ initialError = null }: MfaExitActionsProps) {
  const [pendingAction, setPendingAction] = useState<"switch" | "cancel" | null>(
    null,
  );
  const [error, setError] = useState<string | null>(initialError);
  const pending = pendingAction !== null;

  return (
    <div className="auth-form__exit">
      {error ? <AuthFeedback tone="error" message={error} /> : null}

      <form
        action="/mfa-dogrula/cikis"
        className="auth-form__exit-form"
        method="post"
        onSubmit={() => {
          setError(null);
          setPendingAction("switch");
        }}
      >
        <input name="intent" type="hidden" value="switch" />
        <button
          className="auth-form__exit-button"
          disabled={pending}
          type="submit"
        >
          {pendingAction === "switch"
            ? SAAS_IDENTITY_MESSAGES.mfaExitPending
            : SAAS_IDENTITY_MESSAGES.mfaSwitchAccount}
        </button>
      </form>

      <form
        action="/mfa-dogrula/cikis"
        className="auth-form__exit-form"
        method="post"
        onSubmit={() => {
          setError(null);
          setPendingAction("cancel");
        }}
      >
        <input name="intent" type="hidden" value="cancel" />
        <button
          className="auth-form__exit-button auth-form__exit-button--quiet"
          disabled={pending}
          type="submit"
        >
          {pendingAction === "cancel"
            ? SAAS_IDENTITY_MESSAGES.mfaExitPending
            : SAAS_IDENTITY_MESSAGES.mfaCancelSession}
        </button>
      </form>
    </div>
  );
}
