export const SAAS_IDENTITY_MESSAGES = {
  genericInvalidCredentials:
    "E-posta, kuruluş kodu veya parola hatalı.",
  accountLocked:
    "Hesabınız geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyin.",
  accountDeactivated:
    "Bu hesap devre dışı bırakılmıştır. Destek ekibinizle iletişime geçin.",
  membershipDenied:
    "Bu kuruluş için oturum açma yetkiniz bulunmuyor.",
  forgotPasswordSubmitted:
    "Talebiniz alındı. E-posta adresiniz kayıtlıysa sıfırlama bağlantısı gönderilecektir.",
  resetPasswordSuccess:
    "Parolanız güncellendi. Yeni parolanızla oturum açabilirsiniz.",
  resetPasswordExpired:
    "Sıfırlama bağlantısının süresi dolmuş veya daha önce kullanılmış. Yeni bir talep oluşturun.",
  invitationExpired:
    "Davet bağlantısının süresi dolmuş veya daha önce kullanılmış.",
  invitationSuccess:
    "Davet kabul edildi. Oturum açmaya yönlendiriliyorsunuz.",
  mfaInvalidCode:
    "Doğrulama kodu geçersiz. Lütfen tekrar deneyin.",
  mfaRecoveryInvalid:
    "Kurtarma kodu geçersiz veya daha önce kullanılmış.",
  unexpectedError:
    "İşlem tamamlanamadı. Lütfen tekrar deneyin.",
  configurationError:
    "Kimlik doğrulama yapılandırması eksik.",
  signOutLabel: "Oturumu kapat",
  sessionExpired:
    "Oturumunuzun süresi doldu. Lütfen yeniden giriş yapın.",
  signInTitle: "Oturum aç",
  forgotPasswordTitle: "Parolamı unuttum",
  resetPasswordTitle: "Parolayı sıfırla",
  invitationTitle: "Daveti kabul et",
  mfaTitle: "Çok faktörlü doğrulama",
  mfaSwitchAccount: "Başka hesapla giriş yap",
  mfaCancelSession: "İptal et ve oturumu kapat",
  mfaExitPending: "Oturum kapatılıyor…",
  mfaExitFailed:
    "Bekleyen oturum kapatılamadı. Lütfen tekrar deneyin.",
} as const;

export function mapSaasIdentityFailureMessage(
  code: string | undefined,
  fallback: string = SAAS_IDENTITY_MESSAGES.unexpectedError,
): string {
  switch (code) {
    case "invalid-credentials":
      return SAAS_IDENTITY_MESSAGES.genericInvalidCredentials;
    case "account-locked":
      return SAAS_IDENTITY_MESSAGES.accountLocked;
    case "account-deactivated":
      return SAAS_IDENTITY_MESSAGES.accountDeactivated;
    case "membership-denied":
      return SAAS_IDENTITY_MESSAGES.membershipDenied;
    case "token-expired":
      return SAAS_IDENTITY_MESSAGES.resetPasswordExpired;
    case "invalid-token":
      return SAAS_IDENTITY_MESSAGES.resetPasswordExpired;
    default:
      return fallback;
  }
}
