export type OrderApprovalRole =
  | "sales-representative"
  | "sales-and-order-manager"
  | "planning-manager"
  | "plant-manager"
  | "system-administrator";

export const ORDER_APPROVAL_ROLE_LABELS: Readonly<
  Record<OrderApprovalRole, string>
> = {
  "sales-representative": "Satış Temsilcisi",
  "sales-and-order-manager":
    "Satış ve Sipariş Yöneticisi",
  "planning-manager": "Planlama Yöneticisi",
  "plant-manager": "Fabrika Yöneticisi",
  "system-administrator": "Sistem Yöneticisi",
};

export const ORDER_APPROVAL_ROLE_DESCRIPTIONS: Readonly<
  Record<OrderApprovalRole, string>
> = {
  "sales-representative":
    "Müşteri talebini ve sipariş taslağındaki ticari bilgileri inceleyebilir.",
  "sales-and-order-manager":
    "Sipariş şartlarını, müşteri eşleşmesini ve ticari koşulları onaylayabilir.",
  "planning-manager":
    "Termin, kapasite ve üretim uygunluğunu değerlendirebilir.",
  "plant-manager":
    "Yüksek etkili veya istisnai sipariş kararlarını onaylayabilir.",
  "system-administrator":
    "Teknik entegrasyon ve sistem aktarım sorunlarını yönetebilir.",
};

export function isOrderApprovalRole(
  role: string | undefined,
): role is OrderApprovalRole {
  return (
    role === "sales-representative" ||
    role === "sales-and-order-manager" ||
    role === "planning-manager" ||
    role === "plant-manager" ||
    role === "system-administrator"
  );
}

export function getOrderApprovalRoleLabel(
  role: string | undefined,
): string {
  if (!role) {
    return "Rol tanımlanmadı";
  }

  if (!isOrderApprovalRole(role)) {
    return role;
  }

  return ORDER_APPROVAL_ROLE_LABELS[role];
}

export function getOrderApprovalRoleDescription(
  role: string | undefined,
): string {
  if (!role) {
    return "Bu sipariş için gerekli onay rolü tanımlanmadı.";
  }

  if (!isOrderApprovalRole(role)) {
    return "Bu rol için açıklama bulunmuyor.";
  }

  return ORDER_APPROVAL_ROLE_DESCRIPTIONS[role];
}