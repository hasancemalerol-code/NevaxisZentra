import type {
  ProductionPlan,
} from "../domain/production-plan";

export interface ProductionPlanRepositoryScope {
  tenantId: string;
  facilityId: string;
}

export interface ProductionPlanRepositoryListInput {
  scope: ProductionPlanRepositoryScope;
}

export interface ProductionPlanRepositoryFindByIdInput {
  scope: ProductionPlanRepositoryScope;
  productionPlanId: string;
}

export type ProductionPlanRepositoryFailureCode =
  | "invalid-scope"
  | "permission-denied"
  | "source-unavailable"
  | "unexpected-error";

export interface ProductionPlanRepositoryFailure {
  status: "failure";
  code: ProductionPlanRepositoryFailureCode;
  message: string;
  retryable: boolean;
}

export interface ProductionPlanRepositoryListSuccess {
  status: "success";
  productionPlans: readonly ProductionPlan[];
}

export type ProductionPlanRepositoryListResult =
  | ProductionPlanRepositoryListSuccess
  | ProductionPlanRepositoryFailure;

export interface ProductionPlanRepositoryFindByIdSuccess {
  status: "success";
  productionPlan: ProductionPlan;
}

export interface ProductionPlanRepositoryFindByIdNotFound {
  status: "not-found";
  productionPlan: null;
}

export type ProductionPlanRepositoryFindByIdResult =
  | ProductionPlanRepositoryFindByIdSuccess
  | ProductionPlanRepositoryFindByIdNotFound
  | ProductionPlanRepositoryFailure;

export interface ProductionPlanRepository {
  list(
    input: ProductionPlanRepositoryListInput,
  ): Promise<ProductionPlanRepositoryListResult>;

  findById(
    input: ProductionPlanRepositoryFindByIdInput,
  ): Promise<ProductionPlanRepositoryFindByIdResult>;
}