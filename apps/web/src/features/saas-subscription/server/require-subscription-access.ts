import "./saas-subscription-server-boundary";

import { headers } from "next/headers";
import { redirect } from "next/navigation";

import {
  formatGraceWarning,
  isGraceWarningActive,
  isSubscriptionRecoveryRoute,
  resolveSubscriptionAccessPathname,
} from "../domain/saas-subscription-labels";
import type {
  SubscriptionAccessDenialReason,
  SubscriptionAccessEvaluation,
  SubscriptionAccessFailureKind,
} from "../domain/saas-subscription-types";
import { fetchCurrentTenantSubscription } from "./saas-subscription-queries";
import { fetchSubscriptionAccessStatus } from "./saas-subscription-api-client";

type SubscriptionApiFailure = Readonly<{
  status: "failure";
  message: string;
  code?: string;
  httpStatus?: number;
}>;

function classifySubscriptionApiFailure(
  result: SubscriptionApiFailure,
): SubscriptionAccessFailureKind | "service-unavailable" {
  if (result.httpStatus === 401) {
    return "session-expired";
  }

  if (result.httpStatus === 403) {
    return "forbidden";
  }

  return "service-unavailable";
}

function buildDeniedEvaluation(
  input: Readonly<{
    denialReason: SubscriptionAccessDenialReason;
    subscription?: SubscriptionAccessEvaluation["subscription"];
    graceWarning?: boolean;
    failureKind?: SubscriptionAccessFailureKind;
  }>,
): SubscriptionAccessEvaluation {
  return {
    allowed: false,
    graceWarning: input.graceWarning ?? false,
    denialReason: input.denialReason,
    subscription: input.subscription ?? null,
    failureKind: input.failureKind ?? null,
  };
}

export async function resolveRequestPathname(): Promise<string> {
  const headerList = await headers();
  return headerList.get("x-pathname") ?? "/";
}

export function resolveSubscriptionAccessRedirect(
  evaluation: SubscriptionAccessEvaluation,
): string {
  if (evaluation.failureKind === "session-expired") {
    return "/giris?reason=session-expired";
  }

  if (evaluation.failureKind === "forbidden") {
    return "/giris?auth=denied";
  }

  const reason = evaluation.denialReason ?? "service-unavailable";
  return `/abonelik-erisimi?reason=${reason}`;
}

export async function evaluateSubscriptionAccess(
  pathname: string,
): Promise<SubscriptionAccessEvaluation> {
  const subscriptionPathname = resolveSubscriptionAccessPathname(pathname);

  if (isSubscriptionRecoveryRoute(pathname)) {
    const subscriptionResult = await fetchCurrentTenantSubscription();
    const subscription =
      subscriptionResult.status === "success" ? subscriptionResult.value : null;
    return {
      allowed: true,
      graceWarning: isGraceWarningActive(subscription),
      denialReason: null,
      subscription,
      failureKind: null,
    };
  }

  const accessStatus = await fetchSubscriptionAccessStatus(subscriptionPathname);
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
      failureKind: null,
    };
  }

  const accessFailureKind = classifySubscriptionApiFailure(accessStatus);
  if (accessFailureKind === "session-expired") {
    return buildDeniedEvaluation({
      denialReason: null,
      failureKind: "session-expired",
    });
  }

  if (accessFailureKind === "forbidden") {
    return buildDeniedEvaluation({
      denialReason: null,
      failureKind: "forbidden",
    });
  }

  return buildDeniedEvaluation({
    denialReason: "service-unavailable",
  });
}

export async function requireSubscriptionAccess(
  pathname?: string,
): Promise<SubscriptionAccessEvaluation> {
  const resolvedPathname = pathname ?? await resolveRequestPathname();
  const evaluation = await evaluateSubscriptionAccess(resolvedPathname);

  if (!evaluation.allowed) {
    redirect(resolveSubscriptionAccessRedirect(evaluation));
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
