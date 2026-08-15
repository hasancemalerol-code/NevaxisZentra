import "./saas-subscription-server-boundary";

import { headers } from "next/headers";
import { redirect } from "next/navigation";

import {
  formatGraceWarning,
  isGraceWarningActive,
  isModuleEntitled,
  isSubscriptionRecoveryRoute,
  resolveEntitlementCodeFromPath,
} from "../domain/saas-subscription-labels";
import type { SubscriptionAccessEvaluation } from "../domain/saas-subscription-types";
import { fetchCurrentTenantSubscription } from "./saas-subscription-queries";
import { fetchSubscriptionAccessStatus } from "./saas-subscription-api-client";

export async function resolveRequestPathname(): Promise<string> {
  const headerList = await headers();
  return headerList.get("x-pathname") ?? "/";
}

export async function evaluateSubscriptionAccess(
  pathname: string,
): Promise<SubscriptionAccessEvaluation> {
  if (isSubscriptionRecoveryRoute(pathname)) {
    const subscriptionResult = await fetchCurrentTenantSubscription();
    const subscription =
      subscriptionResult.status === "success" ? subscriptionResult.value : null;
    return {
      allowed: true,
      graceWarning: isGraceWarningActive(subscription),
      denialReason: null,
      subscription,
    };
  }

  const accessStatus = await fetchSubscriptionAccessStatus(pathname);
  if (accessStatus.status === "success") {
    const subscriptionResult = await fetchCurrentTenantSubscription();
    const subscription =
      subscriptionResult.status === "success" ? subscriptionResult.value : null;

    return {
      allowed: accessStatus.value.allowed,
      graceWarning: accessStatus.value.graceWarning,
      denialReason: accessStatus.value.allowed
        ? null
        : (accessStatus.value.denialReason ?? "paid-application"),
      subscription,
    };
  }

  const subscriptionResult = await fetchCurrentTenantSubscription();
  const subscription =
    subscriptionResult.status === "success" ? subscriptionResult.value : null;
  const graceWarning = isGraceWarningActive(subscription);

  if (isSubscriptionRecoveryRoute(pathname)) {
    return {
      allowed: true,
      graceWarning,
      denialReason: null,
      subscription,
    };
  }

  if (!subscription) {
    return {
      allowed: false,
      graceWarning: false,
      denialReason: "paid-application",
      subscription: null,
    };
  }

  if (!subscription.paidApplicationAccess.allowed) {
    return {
      allowed: false,
      graceWarning,
      denialReason: "paid-application",
      subscription,
    };
  }

  const entitlementCode = resolveEntitlementCodeFromPath(pathname);
  if (entitlementCode && !isModuleEntitled(subscription, entitlementCode)) {
    return {
      allowed: false,
      graceWarning,
      denialReason: "module",
      subscription,
    };
  }

  return {
    allowed: true,
    graceWarning,
    denialReason: null,
    subscription,
  };
}

export async function requireSubscriptionAccess(pathname?: string): Promise<SubscriptionAccessEvaluation> {
  const resolvedPathname = pathname ?? await resolveRequestPathname();
  const evaluation = await evaluateSubscriptionAccess(resolvedPathname);

  if (!evaluation.allowed) {
    redirect("/abonelik-erisimi");
  }

  return evaluation;
}

export function resolveSubscriptionGraceBanner(
  evaluation: SubscriptionAccessEvaluation,
): string | null {
  if (!evaluation.graceWarning) {
    return null;
  }

  return formatGraceWarning(evaluation.subscription);
}
