"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  createInitialAiTaskFilterValue,
  type AiTaskFilterPresetId,
  type AiTaskFilterValue,
} from "./ai-task-filter";
import { applyAiTaskFilterPreset } from "./ai-task-filter-query";
import { resolveAiTaskFilterPresetId } from "../presentation/ai-task-filter-preset";
import {
  createAiTaskFilterRelativeUrl,
  hasActiveAiTaskFilters,
  readAiTaskFilterFromLocation,
} from "../presentation/ai-task-filter-url";

export type UseAiTaskFilterStateResult = Readonly<{
  value: AiTaskFilterValue;
  activePresetId?: AiTaskFilterPresetId;
  hasActiveFilters: boolean;
  setValue: (
    value: AiTaskFilterValue,
  ) => void;
  reset: () => void;
  selectPreset: (
    presetId: AiTaskFilterPresetId,
  ) => void;
}>;

export function useAiTaskFilterState():
  UseAiTaskFilterStateResult {
  const [value, setValue] =
    useState<AiTaskFilterValue>(
      createInitialAiTaskFilterValue,
    );

  const [urlReady, setUrlReady] =
    useState(false);

  useEffect(() => {
    function syncFromUrl() {
      setValue(
        readAiTaskFilterFromLocation(
          window.location.search,
        ),
      );
    }

    syncFromUrl();
    setUrlReady(true);

    window.addEventListener(
      "popstate",
      syncFromUrl,
    );

    return () => {
      window.removeEventListener(
        "popstate",
        syncFromUrl,
      );
    };
  }, []);

  useEffect(() => {
    if (!urlReady) {
      return;
    }

    const nextRelativeUrl =
      createAiTaskFilterRelativeUrl(
        window.location.href,
        value,
      );

    const currentRelativeUrl =
      `${window.location.pathname}${window.location.search}${window.location.hash}`;

    if (nextRelativeUrl === currentRelativeUrl) {
      return;
    }

    window.history.replaceState(
      window.history.state,
      "",
      nextRelativeUrl,
    );
  }, [urlReady, value]);

  const activePresetId = useMemo(
    () => resolveAiTaskFilterPresetId(value),
    [value],
  );

  const hasActiveFilters = useMemo(
    () => hasActiveAiTaskFilters(value),
    [value],
  );

  const reset = useCallback(() => {
    setValue(
      createInitialAiTaskFilterValue(),
    );
  }, []);

  const selectPreset = useCallback(
    (presetId: AiTaskFilterPresetId) => {
      setValue(
        applyAiTaskFilterPreset(presetId),
      );
    },
    [],
  );

  return {
    value,
    activePresetId,
    hasActiveFilters,
    setValue,
    reset,
    selectPreset,
  };
}