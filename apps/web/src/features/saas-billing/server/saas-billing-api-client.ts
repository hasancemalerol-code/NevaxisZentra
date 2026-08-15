import "./saas-billing-server-boundary";

import { cookies } from "next/headers";

import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";

import type {
  BillingInvoiceDetail,
  BillingInvoiceSummary,
  BillingPaymentAttempt,
  BillingProviderCapability,
  BillingWorkspaceSummary,
} from "../domain/saas-billing-types";

type ApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string; code?: string; httpStatus?: number }>;

async function readCookieHeader(): Promise<string> {
  const cookieStore = await cookies();
  return cookieStore
    .getAll()
    .map((entry) => `${entry.name}=${entry.value}`)
    .join("; ");
}

async function requestJson<T>(
  path: string,
  init: RequestInit = {},
): Promise<ApiResult<T>> {
  const config = resolveSaasIdentityRuntimeConfig();
  if (config.status === "failure") {
    return { status: "failure", message: config.message, code: "configuration-error" };
  }

  try {
    const response = await fetch(`${config.config.apiBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        Cookie: await readCookieHeader(),
        ...(init.headers ?? {}),
      },
      cache: "no-store",
    });

    if (!response.ok) {
      let message = "İstek başarısız oldu.";
      try {
        const payload = (await response.json()) as { message?: string; failureCode?: string };
        message = payload.message ?? message;
        return {
          status: "failure",
          message,
          code: payload.failureCode,
          httpStatus: response.status,
        };
      } catch {
        return { status: "failure", message, httpStatus: response.status };
      }
    }

    const payload = (await response.json()) as T;
    return { status: "success", value: payload };
  } catch {
    return { status: "failure", message: "API bağlantısı kurulamadı.", code: "network-error" };
  }
}

export async function fetchBillingWorkspace(): Promise<ApiResult<BillingWorkspaceSummary>> {
  return requestJson<BillingWorkspaceSummary>("/api/saas-billing/workspace");
}

export async function fetchBillingInvoices(
  status?: string,
  search?: string,
): Promise<ApiResult<readonly BillingInvoiceSummary[]>> {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  if (search) params.set("search", search);
  const query = params.toString();
  return requestJson<readonly BillingInvoiceSummary[]>(
    `/api/saas-billing/invoices${query ? `?${query}` : ""}`,
  );
}

export async function fetchBillingInvoiceDetail(
  invoiceId: string,
): Promise<ApiResult<BillingInvoiceDetail>> {
  return requestJson<BillingInvoiceDetail>(`/api/saas-billing/invoices/${invoiceId}`);
}

export async function fetchBillingPaymentAttempts(
  invoiceId?: string,
): Promise<ApiResult<readonly BillingPaymentAttempt[]>> {
  const query = invoiceId ? `?invoiceId=${encodeURIComponent(invoiceId)}` : "";
  return requestJson<readonly BillingPaymentAttempt[]>(`/api/saas-billing/payment-attempts${query}`);
}

export async function fetchBillingProviderStatus(): Promise<ApiResult<BillingProviderCapability>> {
  return requestJson<BillingProviderCapability>("/api/saas-billing/provider-status");
}
