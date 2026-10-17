"use client";

import Link from "next/link";
import { useCallback, useState, type FormEvent } from "react";

import { Button } from "@/components/ui/button";
import { forgotSaasIdentityPasswordBrowser } from "../client/saas-identity-browser-client";
import { SAAS_IDENTITY_MESSAGES } from "../presentation/saas-identity-messages";
import { AuthFeedback } from "./auth-feedback";

export function ForgotPasswordForm() {
  const [organizationContext, setOrganizationContext] = useState("");
  const [email, setEmail] = useState("");
  const [pending, setPending] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      setPending(true);
      setError(null);

      const result = await forgotSaasIdentityPasswordBrowser({
        organizationContext: organizationContext.trim(),
        email: email.trim(),
      });

      setPending(false);

      if (result.status === "failure") {
        setError(SAAS_IDENTITY_MESSAGES.unexpectedError);
        return;
      }

      setSubmitted(true);
    },
    [email, organizationContext],
  );

  if (submitted) {
    return (
      <AuthFeedback
        tone="success"
        message={SAAS_IDENTITY_MESSAGES.forgotPasswordSubmitted}
      />
    );
  }

  return (
    <form className="auth-form" onSubmit={handleSubmit} noValidate>
      <div className="auth-form__field">
        <label htmlFor="forgot-organization">Kuruluş kodu <span className="zx-required" aria-hidden="true">*</span></label>
        <input
          id="forgot-organization"
          name="organizationContext"
          autoComplete="organization"
          required
          value={organizationContext}
          onChange={(event) => setOrganizationContext(event.target.value)}
          disabled={pending}
        />
      </div>

      <div className="auth-form__field">
        <label htmlFor="forgot-email">E-posta <span className="zx-required" aria-hidden="true">*</span></label>
        <input
          id="forgot-email"
          name="email"
          type="email"
          autoComplete="username"
          inputMode="email"
          required
          value={email}
          onChange={(event) => setEmail(event.target.value)}
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
        {pending ? "Gönderiliyor…" : "Sıfırlama bağlantısı iste"}
      </Button>

      <p className="auth-form__links">
        <Link href="/giris">Oturum açmaya dön</Link>
      </p>
    </form>
  );
}
