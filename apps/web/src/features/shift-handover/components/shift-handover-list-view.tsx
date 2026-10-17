import Link from "next/link";

import { DataTable } from "@/components/ui/data-table";
import { EmptyPanel } from "@/components/ui/empty-panel";
import { Panel } from "@/components/ui/panel";
import { formatDate } from "@/lib/presentation/format";

import {
  shiftHandoverDepartmentLabel,
  shiftHandoverPeriodLabel,
  shiftHandoverStatusLabel,
} from "../domain/shift-handover-constants";
import type { RawShiftHandoverListResponse } from "../server/shift-handover-api-client";
import { ShiftHandoverCreateForm } from "./shift-handover-create-form";

export function ShiftHandoverListView({ list }: Readonly<{ list: RawShiftHandoverListResponse }>) {
  return (
    <>
      <Panel title="Devir kayıtları" subtitle={list.items.length === 0 ? undefined : "Bu tesis için hazırlanan vardiya devir kayıtları."}>
        {list.items.length === 0 ? (
          <EmptyPanel
            compact
            data-testid="shift-handover-empty"
            title="Henüz vardiya devri yok"
            description={list.capabilities.canManage
              ? "Aşağıdaki Yeni devir taslağı bölümünden ilk taslağı başlatabilirsiniz."
              : "Devir taslağı oluşturulduğunda burada listelenir."}
          />
        ) : (
          <DataTable label="Vardiya devir kayıtları" minWidth={640}>
              <thead>
                <tr>
                  <th scope="col">Referans</th>
                  <th scope="col">Durum</th>
                  <th scope="col">Vardiya</th>
                  <th scope="col">Bölüm</th>
                  <th scope="col">Sorumlu</th>
                  <th className="shift-handover-num" scope="col">Açık kalem</th>
                </tr>
              </thead>
              <tbody>
                {list.items.map((item) => (
                  <tr key={item.id}>
                    <td><Link href={`/vardiya-devir/${item.id}`}>{item.handoverReference}</Link></td>
                    <td><span className="shift-handover-status" data-status={item.status}>{shiftHandoverStatusLabel(item.status)}</span></td>
                    <td>{formatDate(item.shiftDate)} · {shiftHandoverPeriodLabel(item.shiftPeriod)}</td>
                    <td>{shiftHandoverDepartmentLabel(item.department)}</td>
                    <td>{item.outgoingLabel}</td>
                    <td className="shift-handover-num">{item.unresolvedItemCount}</td>
                  </tr>
                ))}
              </tbody>
          </DataTable>
        )}
      </Panel>
      {list.capabilities.canManage ? (
        <Panel
          title="Yeni devir taslağı"
          subtitle="Vardiya tarihini, periyodu ve bölümü belirterek taslak başlatın."
          className="shift-handover-create-panel"
        >
          <details className="shift-handover-disclosure" open={list.items.length === 0}>
            <summary>Taslak başlat</summary>
            <ShiftHandoverCreateForm />
          </details>
        </Panel>
      ) : null}
    </>
  );
}
