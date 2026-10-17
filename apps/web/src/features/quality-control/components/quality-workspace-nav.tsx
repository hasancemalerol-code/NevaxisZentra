import { SubNav } from "@/components/ui/sub-nav";

type QualityWorkspaceNavProps = Readonly<{
  active: "overview" | "ncr" | "follow-up" | "reporting";
}>;

const NAV_ITEMS = [
  { id: "overview" as const, href: "/kalite", label: "Genel Bakış" },
  { id: "ncr" as const, href: "/kalite/ncr", label: "NCR" },
  { id: "follow-up" as const, href: "/kalite/takip-ve-gorevler", label: "Takip ve Görevler" },
  { id: "reporting" as const, href: "/kalite/raporlama", label: "Raporlama" },
];

export function QualityWorkspaceNav({ active }: QualityWorkspaceNavProps) {
  return (
    <SubNav
      label="Kalite çalışma alanı"
      groups={[{ items: NAV_ITEMS.map((item) => ({ href: item.href, label: item.label, active: active === item.id })) }]}
    />
  );
}
