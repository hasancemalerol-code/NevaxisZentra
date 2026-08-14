type AuthFeedbackProps = Readonly<{
  tone: "error" | "success" | "info";
  message: string;
}>;

export function AuthFeedback({ tone, message }: AuthFeedbackProps) {
  return (
    <p
      className={`auth-feedback auth-feedback--${tone}`}
      role={tone === "error" ? "alert" : "status"}
    >
      {message}
    </p>
  );
}
