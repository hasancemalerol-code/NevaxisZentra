export type BillingAccount = Readonly<{
  billingAccountId: string;
  legalName: string;
  displayName: string;
  billingContactMasked: string;
  billingAddressSnapshot: string;
  taxCountryCode: string | null;
  taxNumberMasked: string | null;
  preferredBillingCadence: string;
  preferredPaymentMethod: string | null;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

export type BillingInvoiceLine = Readonly<{
  lineId: string;
  descriptionSnapshot: string;
  componentType: string;
  quantity: number;
  unitAmount: number;
  lineSubtotalAmount: number;
  sourceProvenance: string;
  sortOrder: number;
}>;

export type BillingInvoiceSummary = Readonly<{
  invoiceId: string;
  invoiceNumber: string;
  invoiceStatus: string;
  currencyCode: string;
  totalAmount: number;
  periodStartUtc: string;
  periodEndUtc: string;
  dueAtUtc: string | null;
  paidAtUtc: string | null;
}>;

export type BillingInvoiceDetail = Readonly<{
  invoiceId: string;
  invoiceNumber: string;
  billingAccountId: string;
  subscriptionId: string;
  commercialPlanVersionId: string;
  periodStartUtc: string;
  periodEndUtc: string;
  currencyCode: string;
  subtotalAmount: number;
  taxAmount: number | null;
  totalAmount: number;
  invoiceStatus: string;
  issuedAtUtc: string | null;
  dueAtUtc: string | null;
  paidAtUtc: string | null;
  voidedAtUtc: string | null;
  sourceProvenance: string;
  version: number;
  lines: readonly BillingInvoiceLine[];
}>;

export type BillingPaymentAttempt = Readonly<{
  paymentAttemptId: string;
  invoiceId: string;
  paymentMethod: string;
  providerKey: string | null;
  externalReferenceMasked: string | null;
  amount: number;
  currencyCode: string;
  paymentStatus: string;
  failureCode: string | null;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

export type BillingProviderStatus = Readonly<{
  providerKey: string;
  isProduction: boolean;
  cardCheckoutAvailable: boolean;
  cardCheckoutStatus: string;
  supportedPaymentMethods: readonly string[];
}>;

export type BillingWorkspaceSummary = Readonly<{
  billingAccount: BillingAccount | null;
  invoices: readonly BillingInvoiceSummary[];
  recentPaymentAttempts: readonly BillingPaymentAttempt[];
  providerStatus: BillingProviderStatus;
  openInvoiceCount: number;
  overdueInvoiceCount: number;
  awaitingVerificationCount: number;
}>;

export type BillingProviderCapability = Readonly<{
  provider: BillingProviderStatus;
  providerKey: string;
  isProduction: boolean;
}>;
