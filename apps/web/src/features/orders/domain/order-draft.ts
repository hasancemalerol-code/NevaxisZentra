export type OrderIntakeSource =
  | "email"
  | "whatsapp"
  | "erp"
  | "customer-portal"
  | "sales-representative"
  | "web-form"
  | "file-upload";

export type OrderDraftStatus =
  | "collecting"
  | "needs-review"
  | "ready-for-approval"
  | "approved"
  | "rejected"
  | "transferring"
  | "transferred"
  | "transfer-failed";

export type OrderMatchConfidence =
  | "low"
  | "medium"
  | "high";

export type OrderFieldIssueType =
  | "missing"
  | "conflicting"
  | "low-confidence"
  | "invalid"
  | "outdated";

export type OrderApprovalStatus =
  | "not-required"
  | "pending"
  | "approved"
  | "rejected"
  | "expired";

export type OrderTransferStatus =
  | "not-started"
  | "pending"
  | "in-progress"
  | "completed"
  | "failed";

export type OrderSourceReference = Readonly<{
  id: string;
  source: OrderIntakeSource;
  externalReferenceId?: string;
  senderName?: string;
  senderAddress?: string;
  subject?: string;
  receivedAt: string;
  messagePreview?: string;
  attachmentNames?: readonly string[];
}>;

export type OrderCustomerMatch = Readonly<{
  customerId?: string;
  customerCode?: string;
  customerName?: string;
  taxNumber?: string;
  email?: string;
  phone?: string;
  confidence: number;
  confidenceLevel: OrderMatchConfidence;
  matchedFrom: readonly string[];
}>;

export type OrderProductMatch = Readonly<{
  lineId: string;
  productId?: string;
  productCode?: string;
  productName?: string;
  variant?: string;
  requestedDescription: string;
  quantity?: number;
  unit?: string;
  unitPrice?: number;
  currency?: string;
  requestedDeliveryDate?: string;
  confidence: number;
  confidenceLevel: OrderMatchConfidence;
  matchedFrom: readonly string[];
}>;

export type OrderDeliveryDetails = Readonly<{
  requestedDeliveryDate?: string;
  deliveryAddress?: string;
  deliveryMethod?: string;
  specialInstructions?: string;
}>;

export type OrderCommercialTerms = Readonly<{
  currency?: string;
  paymentTerm?: string;
  discountRate?: number;
  totalAmount?: number;
  creditLimitChecked: boolean;
  priceListChecked: boolean;
}>;

export type OrderFieldIssue = Readonly<{
  id: string;
  field: string;
  type: OrderFieldIssueType;
  message: string;
  sourceReferenceIds: readonly string[];
  blocking: boolean;
}>;

export type OrderDraftApproval = Readonly<{
  status: OrderApprovalStatus;
  requiredRole?: string;
  requestedAt?: string;
  approvedAt?: string;
  approvedBy?: string;
  rejectedAt?: string;
  rejectedBy?: string;
  rejectionReason?: string;
}>;

export type OrderDraftTransfer = Readonly<{
  status: OrderTransferStatus;
  targetSystem?: "erp" | "mes" | "crevafactos";
  externalOrderId?: string;
  startedAt?: string;
  completedAt?: string;
  errorCode?: string;
  errorMessage?: string;
}>;

export type OrderDraft = Readonly<{
  id: string;
  tenantId: string;
  status: OrderDraftStatus;
  title: string;
  createdAt: string;
  updatedAt: string;
  sources: readonly OrderSourceReference[];
  customer: OrderCustomerMatch;
  lines: readonly OrderProductMatch[];
  delivery: OrderDeliveryDetails;
  commercialTerms: OrderCommercialTerms;
  notes?: string;
  overallConfidence: number;
  confidenceLevel: OrderMatchConfidence;
  issues: readonly OrderFieldIssue[];
  approval: OrderDraftApproval;
  transfer: OrderDraftTransfer;
  /** Deterministic export signal for EXPORT-1L. Not inferred from free text. */
  isExport: boolean;
  /** ISO-3166 alpha-2 destination country when isExport is true. */
  destinationCountryCode?: string;
  /** ISO-3166 alpha-2 origin country when known. */
  originCountryCode?: string;
}>;

export function hasBlockingOrderIssues(
  orderDraft: OrderDraft,
): boolean {
  return orderDraft.issues.some(
    (issue) => issue.blocking,
  );
}

export function canSubmitOrderDraftForApproval(
  orderDraft: OrderDraft,
): boolean {
  return (
    orderDraft.lines.length > 0 &&
    Boolean(orderDraft.customer.customerId) &&
    !hasBlockingOrderIssues(orderDraft) &&
    orderDraft.status === "ready-for-approval"
  );
}

export function canTransferOrderDraft(
  orderDraft: OrderDraft,
): boolean {
  return (
    orderDraft.status === "approved" &&
    orderDraft.approval.status === "approved" &&
    !hasBlockingOrderIssues(orderDraft) &&
    orderDraft.transfer.status === "not-started"
  );
}