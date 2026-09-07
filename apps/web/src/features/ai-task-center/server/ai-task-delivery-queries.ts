import { resolveAiTaskRequestContextFromIncomingRequest } from "./ai-task-request-context";
import {
  listAiTaskDeliveries,
  listAiTaskDeliveryChannels,
  listAiTaskSecureAccessGrants,
  listDeliveryManagement,
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

export async function readAiTaskDeliveryManagement(query: string) {
  const context = await requireContext();
  const [result, channels] = await Promise.all([
    listDeliveryManagement(context, query),
    listAiTaskDeliveryChannels(context),
  ]);
  return { ...result, channels };
}
