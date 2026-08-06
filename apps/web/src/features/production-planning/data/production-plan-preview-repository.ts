import type { ProductionPlan } from "../domain/production-plan";
import type {
  ProductionPlanRepository,
  ProductionPlanRepositoryFailure,
  ProductionPlanRepositoryFindByIdInput,
  ProductionPlanRepositoryFindByIdResult,
  ProductionPlanRepositoryListInput,
  ProductionPlanRepositoryListResult,
  ProductionPlanRepositoryScope,
} from "../application/production-plan-repository";

export type CreateProductionPlanPreviewRepositoryInput =
  Readonly<{
    productionPlans: readonly ProductionPlan[];
  }>;

function createInvalidScopeFailure(
  message: string,
): ProductionPlanRepositoryFailure {
  return {
    status: "failure",
    code: "invalid-scope",
    message,
    retryable: false,
  };
}

function createUnexpectedFailure(
  message: string,
): ProductionPlanRepositoryFailure {
  return {
    status: "failure",
    code: "unexpected-error",
    message,
    retryable: false,
  };
}

function normalizeScopeValue(
  value: string | undefined,
): string {
  return value?.trim() ?? "";
}

function validateScope(
  scope: ProductionPlanRepositoryScope,
): ProductionPlanRepositoryFailure | null {
  const tenantId = normalizeScopeValue(
    scope.tenantId,
  );
  const facilityId = normalizeScopeValue(
    scope.facilityId,
  );

  if (!tenantId || !facilityId) {
    return createInvalidScopeFailure(
      "tenantId and facilityId are required.",
    );
  }

  return null;
}

function filterProductionPlansByScope(
  productionPlans: readonly ProductionPlan[],
  scope: ProductionPlanRepositoryScope,
): readonly ProductionPlan[] {
  const tenantId = normalizeScopeValue(
    scope.tenantId,
  );
  const facilityId = normalizeScopeValue(
    scope.facilityId,
  );

  return productionPlans.filter(
    (productionPlan) =>
      productionPlan.tenantId === tenantId &&
      productionPlan.facilityId === facilityId,
  );
}

function findDuplicateProductionPlanIds(
  productionPlans: readonly ProductionPlan[],
): readonly string[] {
  const seenIds = new Set<string>();
  const duplicateIds = new Set<string>();

  for (const productionPlan of productionPlans) {
    if (seenIds.has(productionPlan.id)) {
      duplicateIds.add(productionPlan.id);
      continue;
    }

    seenIds.add(productionPlan.id);
  }

  return [...duplicateIds];
}

export function createProductionPlanPreviewRepository({
  productionPlans,
}: CreateProductionPlanPreviewRepositoryInput): ProductionPlanRepository {
  const sourceProductionPlans =
    productionPlans;

  return {
    async list(
      input: ProductionPlanRepositoryListInput,
    ): Promise<ProductionPlanRepositoryListResult> {
      const scopeFailure = validateScope(
        input.scope,
      );

      if (scopeFailure) {
        return scopeFailure;
      }

      const scopedProductionPlans =
        filterProductionPlansByScope(
          sourceProductionPlans,
          input.scope,
        );

      const duplicateIds =
        findDuplicateProductionPlanIds(
          scopedProductionPlans,
        );

      if (duplicateIds.length > 0) {
        return createUnexpectedFailure(
          [
            "Production plan identity violation.",
            `Multiple plans use id "${duplicateIds[0]}" within the same facility scope.`,
          ].join(" "),
        );
      }

      return {
        status: "success",
        productionPlans: scopedProductionPlans,
      };
    },

    async findById(
      input: ProductionPlanRepositoryFindByIdInput,
    ): Promise<ProductionPlanRepositoryFindByIdResult> {
      const scopeFailure = validateScope(
        input.scope,
      );

      if (scopeFailure) {
        return scopeFailure;
      }

      const productionPlanId =
        normalizeScopeValue(
          input.productionPlanId,
        );

      if (!productionPlanId) {
        return createInvalidScopeFailure(
          "productionPlanId is required.",
        );
      }

      const scopedProductionPlans =
        filterProductionPlansByScope(
          sourceProductionPlans,
          input.scope,
        );

      const matchingProductionPlans =
        scopedProductionPlans.filter(
          (productionPlan) =>
            productionPlan.id ===
            productionPlanId,
        );

      if (matchingProductionPlans.length > 1) {
        return createUnexpectedFailure(
          [
            "Production plan identity violation.",
            `Multiple plans use id "${productionPlanId}" within the same facility scope.`,
          ].join(" "),
        );
      }

      const productionPlan =
        matchingProductionPlans[0];

      if (!productionPlan) {
        return {
          status: "not-found",
          productionPlan: null,
        };
      }

      return {
        status: "success",
        productionPlan,
      };
    },
  };
}
