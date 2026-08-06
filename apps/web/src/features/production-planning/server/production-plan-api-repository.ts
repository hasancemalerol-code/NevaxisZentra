import "./production-plan-server-boundary";

import type {
  ProductionPlanRepository,
  ProductionPlanRepositoryFailure,
  ProductionPlanRepositoryFindByIdInput,
  ProductionPlanRepositoryFindByIdResult,
  ProductionPlanRepositoryListInput,
  ProductionPlanRepositoryListResult,
} from "../application/production-plan-repository";
import {
  getProductionPlanById,
  listProductionPlans,
} from "./production-plan-api-client";
import { mapProductionPlanApiFailureToRepositoryFailure } from "./production-plan-api-errors";
import {
  mapProductionPlanDetailApiDtoToDomain,
  mapProductionPlanSummaryApiDtoToDomain,
} from "./production-plan-api-mapper";
import type { ProductionPlanRequestContext } from "./production-plan-request-context";

function mapApiFailure(
  failure: Extract<
    Awaited<
      ReturnType<typeof listProductionPlans>
    >,
    { status: "failure" }
  >,
): ProductionPlanRepositoryFailure {
  const mappedFailure =
    mapProductionPlanApiFailureToRepositoryFailure(
      failure,
    );

  return {
    status: "failure",
    code: mappedFailure.code,
    message: mappedFailure.message,
    retryable: mappedFailure.retryable,
  };
}

export function createProductionPlanApiRepository(
  requestContext: ProductionPlanRequestContext,
): ProductionPlanRepository {
  return {
    async list(
      input: ProductionPlanRepositoryListInput,
    ): Promise<ProductionPlanRepositoryListResult> {
      const facilityId =
        input.scope.facilityId.trim();

      if (!facilityId) {
        return {
          status: "failure",
          code: "invalid-scope",
          message: "facilityId is required.",
          retryable: false,
        };
      }

      const listResult = await listProductionPlans(
        requestContext,
        {
          facilityId,
        },
      );

      if (listResult.status === "failure") {
        return mapApiFailure(listResult);
      }

      const productionPlans = [];

      for (const item of listResult.value.items) {
        const mappedItem =
          mapProductionPlanSummaryApiDtoToDomain(
            item,
          );

        if (mappedItem.status === "failure") {
          return {
            status: "failure",
            code: "invalid-response",
            message: mappedItem.message,
            retryable: false,
          };
        }

        productionPlans.push(
          mappedItem.value,
        );
      }

      return {
        status: "success",
        productionPlans,
      };
    },

    async findById(
      input: ProductionPlanRepositoryFindByIdInput,
    ): Promise<ProductionPlanRepositoryFindByIdResult> {
      const facilityId =
        input.scope.facilityId.trim();
      const productionPlanId =
        input.productionPlanId.trim();

      if (!facilityId) {
        return {
          status: "failure",
          code: "invalid-scope",
          message: "facilityId is required.",
          retryable: false,
        };
      }

      if (!productionPlanId) {
        return {
          status: "failure",
          code: "invalid-scope",
          message:
            "productionPlanId is required.",
          retryable: false,
        };
      }

      const detailResult =
        await getProductionPlanById(
          requestContext,
          productionPlanId,
          facilityId,
        );

      if (detailResult.status === "failure") {
        if (detailResult.code === "not-found") {
          return {
            status: "not-found",
            productionPlan: null,
          };
        }

        return mapApiFailure(detailResult);
      }

      const mappedPlan =
        mapProductionPlanDetailApiDtoToDomain(
          detailResult.value.plan,
        );

      if (mappedPlan.status === "failure") {
        return {
          status: "failure",
          code: "invalid-response",
          message: mappedPlan.message,
          retryable: false,
        };
      }

      return {
        status: "success",
        productionPlan: mappedPlan.value,
      };
    },
  };
}
