"use client";

import Link from "next/link";
import { useEffect, useState, type FormEvent } from "react";

import { Button } from "@/components/ui/button";
import {
  FIRST_RUN_TOKEN_FILE_HINT,
  fetchFirstRunAvailability,
  submitFirstRunEnrollment,
} from "../lib/first-run-enrollment";
import { AuthFeedback } from "./auth-feedback";

type Phase = "checking" | "available" | "unavailable" | "done";

export function FirstRunEnrollmentForm() {
  const [phase, setPhase] = useState<Phase>("checking");
  const [pending, setPending] = useState(false);
  const [messages, setMessages] = useState<readonly string[]>([]);
  const [tenantKey, setTenantKey] = useState<string | null>(null);
  const [passwordMismatch, setPasswordMismatch] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void fetchFirstRunAvailability().then((available) => {
      if (!cancelled) {
        setPhase(available ? "available" : "unavailable");
      }
    });
    return () => {
      cancelled = true;
    };
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) {
      return;
    }

    const form = new FormData(event.currentTarget);
    const read = (name: string) => String(form.get(name) ?? "");
    if (read("password") !== read("passwordConfirm")) {
      setPasswordMismatch(true);
      setMessages([]);
      return;
    }

    setPasswordMismatch(false);
    setPending(true);
    setMessages([]);
    const outcome = await submitFirstRunEnrollment({
      token: read("token").trim(),
      organizationCode: read("organizationCode").trim(),
      organizationName: read("organizationName").trim(),
      ownerDisplayName: read("ownerDisplayName").trim(),
      ownerEmail: read("ownerEmail").trim(),
      password: read("password"),
    });
    setPending(false);

    if (outcome.status === "enrolled") {
      setTenantKey(outcome.tenantKey);
      setPhase("done");
      return;
    }

    setMessages(outcome.messages);
    if (outcome.unavailable) {
      setPhase("unavailable");
    }
  }

  if (phase === "checking") {
    return <AuthFeedback tone="info" message="Kurulum durumu denetleniyor…" />;
  }

  if (phase === "unavailable") {
    return (
      <div className="auth-form">
        <AuthFeedback
          tone="info"
          message="İlk kurulum bu cihazda kullanılamıyor. Hesabınız zaten oluşturulmuşsa oturum açın."
        />
        {messages.map((message) => (
          <AuthFeedback key={message} tone="error" message={message} />
        ))}
        <p className="auth-form__links">
          <Link href="/giris">Oturum aç</Link>
        </p>
      </div>
    );
  }

  if (phase === "done") {
    return (
      <div className="auth-form">
        <AuthFeedback
          tone="info"
          message={`Kurulum tamamlandı. Oturum açarken kuruluş kodu olarak "${tenantKey ?? ""}" kullanın.`}
        />
        <p className="auth-form__links">
          <Link href="/giris">Oturum aç</Link>
        </p>
      </div>
    );
  }

  return (
    <form className="auth-form" onSubmit={handleSubmit} noValidate>
      <p>
        Kurulum anahtarı bu bilgisayardaki <code>{FIRST_RUN_TOKEN_FILE_HINT}</code> dosyasındadır.
        Anahtar tek kullanımlıktır ve süresi sınırlıdır.
      </p>

      <div className="auth-form__field">
        <label htmlFor="enroll-token">Kurulum anahtarı</label>
        <input id="enroll-token" name="token" autoComplete="off" required disabled={pending} />
      </div>
      <div className="auth-form__field">
        <label htmlFor="enroll-org-code">Kuruluş kodu</label>
        <input id="enroll-org-code" name="organizationCode" autoComplete="off" required disabled={pending} />
      </div>
      <div className="auth-form__field">
        <label htmlFor="enroll-org-name">Kuruluş adı</label>
        <input id="enroll-org-name" name="organizationName" autoComplete="organization" required disabled={pending} />
      </div>
      <div className="auth-form__field">
        <label htmlFor="enroll-owner-name">Yönetici adı</label>
        <input id="enroll-owner-name" name="ownerDisplayName" autoComplete="name" required disabled={pending} />
      </div>
      <div className="auth-form__field">
        <label htmlFor="enroll-owner-email">Yönetici e-posta</label>
        <input id="enroll-owner-email" name="ownerEmail" type="email" autoComplete="username" required disabled={pending} />
      </div>
      <div className="auth-form__field">
        <label htmlFor="enroll-password">Parola</label>
        <input id="enroll-password" name="password" type="password" autoComplete="new-password" required disabled={pending} />
      </div>
      <div className="auth-form__field">
        <label htmlFor="enroll-password-confirm">Parola (tekrar)</label>
        <input id="enroll-password-confirm" name="passwordConfirm" type="password" autoComplete="new-password" required disabled={pending} />
      </div>

      {passwordMismatch ? <AuthFeedback tone="error" message="Parolalar eşleşmiyor." /> : null}
      {messages.map((message) => (
        <AuthFeedback key={message} tone="error" message={message} />
      ))}

      <Button className="auth-form__submit" disabled={pending} aria-busy={pending} type="submit" variant="primary">
        {pending ? "Kurulum yapılıyor…" : "Kurulumu tamamla"}
      </Button>
    </form>
  );
}
