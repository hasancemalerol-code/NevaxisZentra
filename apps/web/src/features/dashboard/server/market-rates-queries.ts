import "./dashboard-server-boundary";

import { cache } from "react";

import {
  buildAccessRestrictedSummary,
  buildSourceUnavailableSummary,
} from "../domain/market-rates-presentations";
import {
  getMarketRatesSummary,
  type RawMarketRateSummary,
} from "./market-rates-api-client";
import { resolveDashboardRequestContext } from "./market-rates-request-context";

export type MarketRatesReadResult =
  | Readonly<{ status: "success"; summary: RawMarketRateSummary }>;

export async function readMarketRatesSummary(): Promise<MarketRatesReadResult> {
  return readMarketRatesSummaryCached();
}

const readMarketRatesSummaryCached = cache(async (): Promise<MarketRatesReadResult> => {
  const contextResult = await resolveDashboardRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "success",
      summary: buildSourceUnavailableSummary(contextResult.message),
    };
  }

  const summaryResult = await getMarketRatesSummary(contextResult.context);
  if (summaryResult.status === "failure") {
    if (summaryResult.code === "permission-denied") {
      return {
        status: "success",
        summary: buildAccessRestrictedSummary(summaryResult.message),
      };
    }

    if (summaryResult.code === "unauthenticated") {
      return {
        status: "success",
        summary: buildAccessRestrictedSummary("Kimlik doğrulama gerekli."),
      };
    }

    return {
      status: "success",
      summary: buildSourceUnavailableSummary(summaryResult.message),
    };
  }

  return { status: "success", summary: summaryResult.value };
});
