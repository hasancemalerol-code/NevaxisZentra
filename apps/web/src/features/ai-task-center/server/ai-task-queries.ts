import "./ai-task-server-boundary";

import { calculateAiTaskKpis } from "../application/ai-task-query";
import type { AiTask } from "../domain/ai-task";
import {
  getAiTask,
  listAiTasks,
  type RawAiTaskListResponse,
} from "./ai-task-api-client";
import {
  mapRawAiTaskDetailViewModel,
  mapRawAiTaskSummary,
  type AiTaskDetailViewModel,
} from "./ai-task-mapper";
import { parseMembershipIdFromMemberReference } from "./ai-task-membership";
import { resolveAiTaskRequestContextFromIncomingRequest } from "./ai-task-request-context";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";
import { getTeamsChannelDetail, listTeamsChannels } from "../../teams/server/teams-api-client";
import { resolveTeamsRequestContext } from "../../teams/server/teams-request-context";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type AiTaskAssignmentCandidate = Readonly<{
  tenantMembershipId: string;
  displayLabel: string;
}>;

export type AiTaskTeamChannelOption = Readonly<{
  id: string;
  name: string;
}>;

export type AiTaskOperationCapabilities = Readonly<{
  canAssign: boolean;
  canAcknowledge: boolean;
  canEscalate: boolean;
  canManage: boolean;
  canReview: boolean;
  canManageEvidence: boolean;
  canRequestClosure: boolean;
  canApproveClosure: boolean;
  canReopen: boolean;
  currentMembershipId: string | null;
  membershipCandidates: readonly AiTaskAssignmentCandidate[];
  teamChannels: readonly AiTaskTeamChannelOption[];
  teamChannelLabels: Readonly<Record<string, string>>;
}>;

export type AiTaskWorkspacePayload = Readonly<{
  tasks: readonly AiTask[];
  totalCount: number;
  page: number;
  pageSize: number;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  canCreate: boolean;
  canReview: boolean;
  kpis: ReturnType<typeof calculateAiTaskKpis>;
}>;

export type AiTaskWorkspaceResult =
  | Readonly<{ status: "success"; workspace: AiTaskWorkspacePayload }>
  | ReadFailure;

export type AiTaskDetailResult =
  | Readonly<{
      status: "success";
      detail: AiTaskDetailViewModel;
      canReview: boolean;
      operations: AiTaskOperationCapabilities;
    }>
  | ReadFailure;

function mapFailureMessage(code: string | undefined, message: string): ReadFailure {
  if (code === "unauthenticated") {
    return { status: "failure", message: "Oturum doğrulanamadı.", code };
  }

  if (code === "permission-denied") {
    return { status: "failure", message: "AI görevlerini görüntüleme yetkiniz bulunmuyor.", code };
  }

  if (code === "subscription-required") {
    return {
      status: "failure",
      message: "AI görevleri bu kurulumun lisansı kapsamında etkin değil. Lisans durumunu sistem yöneticinizle kontrol edin.",
      code,
    };
  }

  return { status: "failure", message, code };
}

function buildWorkspace(list: RawAiTaskListResponse): AiTaskWorkspacePayload {
  const tasks = list.items.map(mapRawAiTaskSummary);

  return {
    tasks,
    totalCount: list.totalCount,
    page: list.page,
    pageSize: list.pageSize,
    ruleSetVersion: list.ruleSetVersion,
    provenanceDisclaimer: list.provenanceDisclaimer,
    canCreate: list.capabilities.canCreate,
    canReview: list.capabilities.canReview,
    kpis: calculateAiTaskKpis(tasks),
  };
}

export async function readAiTaskWorkspace(
  filters?: Readonly<{
    status?: string;
    category?: string;
    priority?: string;
    sourceModule?: string;
    page?: number;
    pageSize?: number;
  }>,
): Promise<AiTaskWorkspaceResult> {
  const authorization = await denyUnlessGranted("ai-task.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = await resolveAiTaskRequestContextFromIncomingRequest();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await listAiTasks(contextResult.context, filters);
  if (result.status === "failure") {
    return mapFailureMessage(result.code, result.message);
  }

  return { status: "success", workspace: buildWorkspace(result.value) };
}

async function resolveOperationCapabilities(): Promise<AiTaskOperationCapabilities> {
  const access = await fetchEffectiveAccessCapabilities();
  const canAssign = hasGrantedPermission(access, "ai-task.assign");
  const canAcknowledge = hasGrantedPermission(access, "ai-task.acknowledge");
  const canEscalate = hasGrantedPermission(access, "ai-task.escalate");
  const canManage = hasGrantedPermission(access, "ai-task.manage");
  const canReview = hasGrantedPermission(access, "ai-task.review");
  const canManageEvidence = hasGrantedPermission(access, "ai-task.evidence.manage");
  const canRequestClosure = hasGrantedPermission(access, "ai-task.closure.request");
  const canApproveClosure = hasGrantedPermission(access, "ai-task.closure.approve");
  const canReopen = hasGrantedPermission(access, "ai-task.reopen");
  const currentMembershipId = parseMembershipIdFromMemberReference(
    access?.memberReference ?? undefined,
  );

  const membershipCandidates = new Map<string, AiTaskAssignmentCandidate>();
  const teamChannels: AiTaskTeamChannelOption[] = [];
  const teamChannelLabels: Record<string, string> = {};

  if (hasGrantedPermission(access, "teams.read")) {
    const teamsContext = await resolveTeamsRequestContext();
    if (teamsContext.status === "success") {
      const channelsResult = await listTeamsChannels(teamsContext.context);
      if (channelsResult.status === "success") {
        for (const channel of channelsResult.value.items) {
          teamChannels.push({ id: channel.id, name: channel.name });
          teamChannelLabels[channel.id] = channel.name;
        }

        const primaryChannelId = channelsResult.value.items[0]?.id;
        if (primaryChannelId) {
          const detailResult = await getTeamsChannelDetail(
            teamsContext.context,
            primaryChannelId,
          );
          if (detailResult.status === "success") {
            for (const member of detailResult.value.members) {
              membershipCandidates.set(member.tenantMembershipId, {
                tenantMembershipId: member.tenantMembershipId,
                displayLabel: member.displayLabel,
              });
            }
          }
        }
      }
    }
  }

  return {
    canAssign,
    canAcknowledge,
    canEscalate,
    canManage,
    canReview,
    canManageEvidence,
    canRequestClosure,
    canApproveClosure,
    canReopen,
    currentMembershipId,
    membershipCandidates: [...membershipCandidates.values()],
    teamChannels,
    teamChannelLabels,
  };
}

export async function readAiTaskDetail(taskId: string): Promise<AiTaskDetailResult> {
  const authorization = await denyUnlessGranted("ai-task.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = await resolveAiTaskRequestContextFromIncomingRequest();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const listResult = await listAiTasks(contextResult.context, { page: 1, pageSize: 1 });
  const canReview = listResult.status === "success"
    ? listResult.value.capabilities.canReview
    : false;

  const operations = await resolveOperationCapabilities();

  const result = await getAiTask(contextResult.context, taskId);
  if (result.status === "failure") {
    if (result.code === "not-found") {
      return { status: "failure", message: "AI görevi bulunamadı.", code: result.code };
    }

    return mapFailureMessage(result.code, result.message);
  }

  return {
    status: "success",
    detail: mapRawAiTaskDetailViewModel(result.value),
    canReview,
    operations,
  };
}
