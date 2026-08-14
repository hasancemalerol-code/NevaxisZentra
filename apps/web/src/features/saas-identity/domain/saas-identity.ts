export type SaasIdentitySignInOutcome = "authenticated" | "mfa_required";

export type SaasIdentityAccountSummary = Readonly<{
  accountReference: string;
  displayName: string;
  emailMasked: string;
  organizationLabel: string;
  sessionReference: string;
  mfaPending: boolean;
}>;

export type SaasIdentitySessionState =
  | Readonly<{ status: "unauthenticated" }>
  | Readonly<{ status: "mfa_pending"; displayName?: string }>
  | Readonly<{ status: "authenticated"; account: SaasIdentityAccountSummary }>;

export type SaasIdentityApiFailureCode =
  | "invalid-credentials"
  | "account-locked"
  | "account-deactivated"
  | "membership-denied"
  | "mfa-required"
  | "invalid-token"
  | "token-expired"
  | "invalid-request"
  | "csrf-denied"
  | "unauthenticated"
  | "configuration-error"
  | "source-unavailable";

export type SaasIdentityApiFailure = Readonly<{
  status: "failure";
  code: SaasIdentityApiFailureCode;
  message: string;
  retryable: boolean;
}>;

export type SaasIdentityApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | SaasIdentityApiFailure;

export type SaasIdentitySignInRequest = Readonly<{
  organizationContext: string;
  email: string;
  password: string;
}>;

export type SaasIdentitySignInResponse = Readonly<{
  outcome: SaasIdentitySignInOutcome;
}>;

export type SaasIdentityMfaVerifyRequest = Readonly<{
  totpCode?: string;
  recoveryCode?: string;
}>;

export type SaasIdentityForgotPasswordRequest = Readonly<{
  organizationContext: string;
  email: string;
}>;

export type SaasIdentityResetPasswordRequest = Readonly<{
  token: string;
  newPassword: string;
}>;

export type SaasIdentityInvitationAcceptRequest = Readonly<{
  token: string;
  displayName: string;
  password: string;
}>;
