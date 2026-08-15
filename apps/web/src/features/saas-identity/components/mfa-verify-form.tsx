"use client";

import { useCallback, useState, type FormEvent } from "react";

import { Button } from "@/components/ui/button";
import { verifySaasIdentityMfaBrowser } from "../client/saas-identity-browser-client";
import {
  mapSaasIdentityFailureMessage,
  SAAS_IDENTITY_MESSAGES,
} from "../presentation/saas-identity-messages";
import { AuthFeedback } from "./auth-feedback";

type MfaMode = "totp" | "recovery";

export function MfaVerifyForm() {
  const [mode, setMode] = useState<MfaMode>("totp");
  const [totpCode, setTotpCode] = useState("");
  const [recoveryCode, setRecoveryCode] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      setPending(true);
      setError(null);

      const result = await verifySaasIdentityMfaBrowser(
        mode === "totp"
          ? { totpCode: totpCode.trim() }
          : { recoveryCode: recoveryCode.trim() },
      );

      if (result.status === "failure") {
        setError(
          mapSaasIdentityFailureMessage(
            result.code,
            mode === "totp"
              ? SAAS_IDENTITY_MESSAGES.mfaInvalidCode
              : SAAS_IDENTITY_MESSAGES.mfaRecoveryInvalid,
          ),
        );
        setPending(false);
        return;
      }

      window.location.assign("/dashboard");
    },
    [mode, recoveryCode, totpCode],
  );

  return (
    <form
      className="auth-form"
      method="post"
      noValidate
      onSubmit={handleSubmit}
    >
      <div className="auth-form__mode-switch" role="tablist" aria-label="Doğrulama yöntemi">
        <button
          type="button"
          role="tab"
          aria-selected={mode === "totp"}
          className={mode === "totp" ? "is-active" : undefined}
          onClick={() => {
            setMode("totp");
            setError(null);
          }}
        >
          Doğrulama kodu
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={mode === "recovery"}
          className={mode === "recovery" ? "is-active" : undefined}
          onClick={() => {
            setMode("recovery");
            setError(null);
          }}
        >
          Kurtarma kodu
        </button>
      </div>

      {mode === "totp" ? (
        <div className="auth-form__field">
          <label htmlFor="mfa-totp">Kimlik doğrulama kodu</label>
          <input
            id="mfa-totp"
            name="totpCode"
            inputMode="numeric"
            autoComplete="one-time-code"
            required
            value={totpCode}
            onChange={(event) => setTotpCode(event.target.value)}
            disabled={pending}
          />
        </div>
      ) : (
        <div className="auth-form__field">
          <label htmlFor="mfa-recovery">Kurtarma kodu</label>
          <input
            id="mfa-recovery"
            name="recoveryCode"
            autoComplete="off"
            required
            value={recoveryCode}
            onChange={(event) => setRecoveryCode(event.target.value)}
            disabled={pending}
          />
        </div>
      )}

      {error ? <AuthFeedback tone="error" message={error} /> : null}

      <Button
        className="auth-form__submit"
        disabled={pending}
        type="submit"
        variant="primary"
      >
        {pending ? "Doğrulanıyor…" : "Doğrula"}
      </Button>
    </form>
  );
}
