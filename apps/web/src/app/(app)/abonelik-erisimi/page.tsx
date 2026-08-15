import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { SubscriptionAccessDeniedView } from "@/features/saas-subscription/components/subscription-access-denied-view";
import type { SubscriptionAccessDenialReason } from "@/features/saas-subscription/domain/saas-subscription-types";
import { fetchCurrentTenantSubscription } from "@/features/saas-subscription/server/saas-subscription-queries";

function resolveDenialReason(value: string | undefined): SubscriptionAccessDenialReason {
  if (value === "module") {
    return "module";
  }

  return "paid-application";
}

export default async function AbonelikErisimiPage({
  searchParams,
}: Readonly<{
  searchParams: Promise<Readonly<{ reason?: string }>>;
}>) {
  const [{ reason }, subscriptionResult, access] = await Promise.all([
    searchParams,
    fetchCurrentTenantSubscription(),
    fetchCurrentEffectiveAccess(),
  ]);

  return (
    <SubscriptionAccessDeniedView
      canAccessSettings={hasGrantedPermission(access, "admin-access.read")}
      denialReason={resolveDenialReason(reason)}
      subscription={subscriptionResult.status === "success" ? subscriptionResult.value : null}
    />
  );
}
