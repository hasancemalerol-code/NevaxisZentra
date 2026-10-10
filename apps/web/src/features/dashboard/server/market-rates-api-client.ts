import { refusalCode, SESSION_REQUIRED_MESSAGE, SUBSCRIPTION_REQUIRED_MESSAGE } from "@/lib/api/api-failure-classifier";
import "./dashboard-server-boundary";

import { fetchTrustedApi } from "@/features/saas-identity/server/authenticated-server-fetch";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { DashboardRequestContext } from "./market-rates-request-context";

export type MarketRatesApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "subscription-required"
    | "invalid-request";
  message: string;
  retryable: boolean;
}>;

export type MarketRatesApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | MarketRatesApiFailure;

export type RawMarketRateQuote = Readonly<{
  pairLabel: string;
  baseCurrency: string;
  quoteCurrency: string;
  rate: number | null;
  changePercent: number | null;
  changePercentAvailable: boolean;
}>;

export type RawMarketRateSummary = Readonly<{
  providerKey: string;
  sourceLabel: string;
  sourceAttributionUrl: string | null;
  providerObservedAtUtc: string | null;
  synchronizedAtUtc: string | null;
  freshnessState: string;
  freshnessLabel: string;
  availabilityState: string;
  availabilityLabel: string;
  configurationMessage: string | null;
  missingConfigurationKeys: readonly string[];
  lastFetchFailureCode: string | null;
  disclaimer: string;
  quotes: readonly RawMarketRateQuote[];
}>;

function buildSessionPath(
  facilityId: string,
  path: string,
): string {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId);
  return `${path}?${params.toString()}`;
}

function buildDevelopmentUrl(
  context: ProductionOrderRequestContext,
  path: string,
): string {
  const url = new URL(path, context.apiBaseUrl);
  url.searchParams.set("facilityId", context.facilityId);
  return url.toString();
}

function failureMessage(status: number, payload: unknown): string {
  if (payload && typeof payload === "object" && "detail" in payload) {
    const detail = (payload as { detail?: unknown }).detail;
    if (typeof detail === "string" && detail.trim()) {
      return detail;
    }
  }

  if (status === 401) return "Kimlik doğrulama gerekli.";
  if (status === 403) {
    const cause = refusalCode(status, payload);
    if (cause === "subscription-required") return SUBSCRIPTION_REQUIRED_MESSAGE;
    if (cause === "unauthenticated") return SESSION_REQUIRED_MESSAGE;
    return "Piyasa kuru özeti için yetkiniz yok.";
  }
  return "Piyasa kuru özeti alınamadı.";
}

function mapFailureStatus(status: number, payload: unknown): MarketRatesApiFailure["code"] {
  if (status === 401) return "unauthenticated";
  if (status === 403) return refusalCode(status, payload);
  if (payload && typeof payload === "object" && "failureCode" in payload) {
    return "invalid-request";
  }
  return "source-unavailable";
}

async function getJson<T>(
  context: DashboardRequestContext,
  path: string,
  parser: (payload: unknown) => T,
): Promise<MarketRatesApiResult<T>> {
  try {
    if (context.mode === "session") {
      const fetchResult = await fetchTrustedApi(
        buildSessionPath(context.facilityId, path),
        { method: "GET" },
      );
      if (fetchResult.status === "failure") {
        return {
          status: "failure",
          code: "source-unavailable",
          message: fetchResult.message,
          retryable: fetchResult.code === "network-error",
        };
      }

      const response = fetchResult.response;
      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        return {
          status: "failure",
          code: mapFailureStatus(response.status, payload),
          message: failureMessage(response.status, payload),
          retryable: response.status >= 500,
        };
      }

      return { status: "success", value: parser(payload) };
    }

    const response = await fetch(buildDevelopmentUrl(context.context, path), {
      headers: context.context.requestHeaders,
      cache: "no-store",
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      return {
        status: "failure",
        code: mapFailureStatus(response.status, payload),
        message: failureMessage(response.status, payload),
        retryable: response.status >= 500,
      };
    }

    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Piyasa kuru özeti alınamadı.",
      retryable: true,
    };
  }
}

function parseSummary(payload: unknown): RawMarketRateSummary {
  return payload as RawMarketRateSummary;
}

export async function getMarketRatesSummary(
  context: DashboardRequestContext,
): Promise<MarketRatesApiResult<RawMarketRateSummary>> {
  return getJson(context, "/api/dashboard/market-rates", parseSummary);
}
