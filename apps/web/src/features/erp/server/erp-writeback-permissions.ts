import "./erp-server-boundary";

import type { ProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import type { ErpWritebackPermissions } from "../domain/erp";

const NO_PERMISSIONS: ErpWritebackPermissions = {
  canRequest: false,
  canApprove: false,
  canExecute: false,
};

export function resolveErpWritebackPermissions(
  config: ProductionExecutionRuntimeConfig,
): ErpWritebackPermissions {
  if (config.mode === "development-preview") {
    return NO_PERMISSIONS;
  }

  if (config.mode === "development-api" && config.developmentAuth) {
    const subject = config.developmentAuth.subject;

    if (subject === "planning-pilot-reviewer") {
      return { canRequest: false, canApprove: true, canExecute: false };
    }

    if (subject === "production-pilot-operator") {
      return { canRequest: true, canApprove: true, canExecute: true };
    }

    return { canRequest: true, canApprove: false, canExecute: false };
  }

  if (config.mode === "api") {
    return { canRequest: true, canApprove: true, canExecute: true };
  }

  return NO_PERMISSIONS;
}
