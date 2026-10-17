import { resolveAiTaskRequestContextFromIncomingRequest } from "./ai-task-request-context";
import {
  listAiTaskDeliveries,
  listAiTaskDeliveryChannels,
  listAiTaskSecureAccessGrants,
  listDeliveryManagement,
  AiTaskDeliveryApiError,
} from "./ai-task-delivery-api-client";

async function requireContext() {
  const result = await resolveAiTaskRequestContextFromIncomingRequest();
  if (result.status === "failure") {
    throw new Error(result.message);
  }
  return result.context;
}

export async function readAiTaskDeliveryWorkspace(taskId: string) {
  const context = await requireContext();
  const [deliveries, grants, channels] = await Promise.all([
    listAiTaskDeliveries(context, taskId),
    listAiTaskSecureAccessGrants(context, taskId),
    listAiTaskDeliveryChannels(context),
  ]);
  return { deliveries, grants, channels, canManage: true };
}

export type AiTaskDeliveryManagementResult =
  | Readonly<{
      status: "success";
      items: Awaited<ReturnType<typeof listDeliveryManagement>>["items"];
      totalCount: number;
      channels: Awaited<ReturnType<typeof listAiTaskDeliveryChannels>>;
    }>
  | Readonly<{ status: "failure"; code: string }>;

/** Never throws: a refusal, missing licence, unavailable provider or session problem is returned as a classified failure. */
export async function readAiTaskDeliveryManagement(query: string): Promise<AiTaskDeliveryManagementResult> {
  const contextResult = await resolveAiTaskRequestContextFromIncomingRequest();
  if (contextResult.status === "failure") {
    return { status: "failure", code: "code" in contextResult && typeof contextResult.code === "string" ? contextResult.code : "source-unavailable" };
  }

  try {
    const [result, channels] = await Promise.all([
      listDeliveryManagement(contextResult.context, query),
      listAiTaskDeliveryChannels(contextResult.context),
    ]);
    return { status: "success", ...result, channels };
  } catch (error) {
    return {
      status: "failure",
      code: error instanceof AiTaskDeliveryApiError ? error.failureCode : "source-unavailable",
    };
  }
}
