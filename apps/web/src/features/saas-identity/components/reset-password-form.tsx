"use client";

import Link from "next/link";
import { useCallback, useEffect, useState, type FormEvent } from "react";

import { Button } from "@/components/ui/button";
import { resetSaasIdentityPasswordBrowser } from "../client/saas-identity-browser-client";
import { stripSensitiveQueryParam } from "../lib/saas-identity-csrf";
import {
  mapSaasIdentityFailureMessage,
  SAAS_IDENTITY_MESSAGES,
} from "../presentation/saas-identity-messages";
import { AuthFeedback } from "./auth-feedback";

type ResetPasswordFormProps = Readonly<{
  token: string | null;
}>;

export function ResetPasswordForm({ token }: ResetPasswordFormProps) {
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [expired, setExpired] = useState(false);

  useEffect(() => {
    stripSensitiveQueryParam("token");
  }, []);

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!token) {
        setExpired(true);
        return;
      }

      if (newPassword !== confirmPassword) {
        setError("Parolalar eşleşmiyor.");
        return;
      }

      setPending(true);
      setError(null);

      const result = await resetSaasIdentityPasswordBrowser({
        token,
        newPassword,
      });

      setPending(false);

      if (result.status === "failure") {
        if (
          result.code === "invalid-token" ||
          result.code === "token-expired"
        ) {
          setExpired(true);
          return;
        }

        setError(mapSaasIdentityFailureMessage(result.code));
        return;
      }

      setSuccess(true);
    },
    [confirmPassword, newPassword, token],
  );

  if (!token || expired) {
    return (
      <>
        <AuthFeedback
          tone="error"
          message={SAAS_IDENTITY_MESSAGES.resetPasswordExpired}
        />
        <p className="auth-form__links">
          <Link href="/sifremi-unuttum">Yeni sıfırlama talebi oluştur</Link>
        </p>
      </>
    );
  }

  if (success) {
    return (
      <>
        <AuthFeedback
          tone="success"
          message={SAAS_IDENTITY_MESSAGES.resetPasswordSuccess}
        />
        <p className="auth-form__links">
          <Link href="/giris">Oturum aç</Link>
        </p>
      </>
    );
  }

  return (
    <form className="auth-form" onSubmit={handleSubmit} noValidate>
      <div className="auth-form__field">
        <label htmlFor="reset-password">Yeni parola <span className="zx-required" aria-hidden="true">*</span></label>
        <input
          id="reset-password"
          name="newPassword"
          type="password"
          autoComplete="new-password"
          required
          minLength={12}
          value={newPassword}
          onChange={(event) => setNewPassword(event.target.value)}
          disabled={pending}
        />
      </div>

      <div className="auth-form__field">
        <label htmlFor="reset-password-confirm">Yeni parola (tekrar) <span className="zx-required" aria-hidden="true">*</span></label>
        <input
          id="reset-password-confirm"
          name="confirmPassword"
          type="password"
          autoComplete="new-password"
          required
          minLength={12}
          value={confirmPassword}
          onChange={(event) => setConfirmPassword(event.target.value)}
          disabled={pending}
        />
      </div>

      {error ? <AuthFeedback tone="error" message={error} /> : null}

      <Button
        className="auth-form__submit"
        disabled={pending}
        type="submit"
        variant="primary"
      >
        {pending ? "Kaydediliyor…" : "Parolayı güncelle"}
      </Button>
    </form>
  );
}
