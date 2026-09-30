import type { OrderDraft } from "../domain/order-draft";

// Isolated preview fixture data for orders workspace development.

export const ORDER_DRAFT_PREVIEW_DATA: readonly OrderDraft[] = [];

export function findOrderDraftById(
  orderDraftId: string,
): OrderDraft | undefined {
  return ORDER_DRAFT_PREVIEW_DATA.find((draft) => draft.id === orderDraftId);
}
