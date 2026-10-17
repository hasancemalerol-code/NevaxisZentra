import { SubNav } from "@/components/ui/sub-nav";

import "./maintenance-workspace.css";

type MaintenanceWorkspaceNavProps = Readonly<{
  active:
    | "overview"
    | "work-orders"
    | "requests"
    | "preventive"
    | "planning"
    | "reporting"
    | "condition"
    | "predictive-risks";
}>;

const NAV_ITEMS = [
  { id: "overview" as const, href: "/bakim", label: "Genel Bakış" },
  { id: "planning" as const, href: "/bakim/planlama", label: "Bakım Planlama" },
  { id: "reporting" as const, href: "/bakim/raporlama", label: "Raporlama" },
  { id: "condition" as const, href: "/bakim/durum-zekasi", label: "Durum Zekâsı" },
  { id: "predictive-risks" as const, href: "/bakim/kestirimci-riskler", label: "Kestirimci Riskler" },
];

export function MaintenanceWorkspaceNav({ active }: MaintenanceWorkspaceNavProps) {
  return (
    <SubNav
      label="Bakım çalışma alanı"
      groups={[{ items: NAV_ITEMS.map((item) => ({ href: item.href, label: item.label, active: active === item.id })) }]}
    />
  );
}
