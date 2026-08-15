import "./saas-subscription-server-boundary";

import { fetchCurrentSubscription } from "./saas-subscription-api-client";

export async function fetchCurrentTenantSubscription() {
  return fetchCurrentSubscription();
}
