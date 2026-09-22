import type { SerializableNavigationGroup } from "../../src/lib/navigation/navigation-types";
import { CONTEXTUAL_NAVIGATION_SECTIONS, WORKFLOW_PRIMARY_HOMES } from "../../src/lib/navigation/workflow-primary-navigation";

export const workflowNavigationFixture: readonly SerializableNavigationGroup[] = [
  {
    label: "Çalışma Alanı",
    items: [...WORKFLOW_PRIMARY_HOMES],
  },
  {
    label: "Fabrika",
    items: [...CONTEXTUAL_NAVIGATION_SECTIONS[1].items],
  },
];
