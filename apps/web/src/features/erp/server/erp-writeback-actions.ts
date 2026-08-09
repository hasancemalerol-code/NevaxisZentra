"use server";

import type { ErpWritebackActionResult } from "./erp-writeback-writer";
import {
  writeApproveErpWriteback,
  writeExecuteErpWriteback,
  writeRejectErpWriteback,
  writeSubmitErpWriteback,
} from "./erp-writeback-writer";

export async function submitErpWritebackAction(
  writebackId: string,
  expectedVersion: number,
): Promise<ErpWritebackActionResult> {
  return writeSubmitErpWriteback(writebackId, expectedVersion);
}

export async function approveErpWritebackAction(
  writebackId: string,
  expectedVersion: number,
  comment?: string,
): Promise<ErpWritebackActionResult> {
  return writeApproveErpWriteback(writebackId, expectedVersion, comment);
}

export async function rejectErpWritebackAction(
  writebackId: string,
  expectedVersion: number,
  comment?: string,
): Promise<ErpWritebackActionResult> {
  return writeRejectErpWriteback(writebackId, expectedVersion, comment);
}

export async function executeErpWritebackAction(
  writebackId: string,
  expectedVersion: number,
): Promise<ErpWritebackActionResult> {
  return writeExecuteErpWriteback(writebackId, expectedVersion);
}
