"use client";

import { CalendarClock } from "lucide-react";

import type { ShiftAssignmentContextOptions } from "../../shift-assignment/domain/shift-assignment-types";
import { LOCAL_PILOT_DISCLAIMER } from "../domain/shift-workforce-labels";

type ShiftPlanPanelProps = Readonly<{
  contextOptions: ShiftAssignmentContextOptions;
}>;

export function ShiftPlanPanel({ contextOptions }: ShiftPlanPanelProps) {
  const shiftRows = contextOptions.shifts.map((shift) => ({
    shift,
    orders: contextOptions.productionOrders,
    machines: contextOptions.machines,
  }));

  return (
    <div className="shift-plan-panel">
      <p className="shift-workforce-pilot-banner" role="status">
        {LOCAL_PILOT_DISCLAIMER}
      </p>

      <section className="shift-workforce-registry" aria-label="Vardiya planı">
        <div className="shift-workforce-registry__header">
          <h2 className="shift-workforce-registry__title">Vardiya Planı</h2>
          <span className="shift-workforce-registry__count">{shiftRows.length} vardiya</span>
        </div>

        {shiftRows.length === 0 ? (
          <div className="shift-workforce-empty">
            <CalendarClock aria-hidden="true" size={28} />
            <h3>Vardiya tanımı bulunamadı</h3>
            <p>Planlama bağlamı için kayıtlı vardiya bulunmuyor.</p>
          </div>
        ) : (
          <div className="shift-workforce-registry__table-wrap">
            <table className="shift-workforce-table">
              <thead>
                <tr>
                  <th>Vardiya</th>
                  <th>Üretim emirleri</th>
                  <th>Makineler</th>
                  <th>Plan durumu</th>
                </tr>
              </thead>
              <tbody>
                {shiftRows.map(({ shift, orders, machines }) => (
                  <tr key={shift.value}>
                    <td>
                      <span className="shift-workforce-cell-primary">{shift.label}</span>
                    </td>
                    <td>
                      {orders.length > 0
                        ? orders.map((order) => order.label).join(" · ")
                        : "—"}
                    </td>
                    <td>
                      {machines.length > 0
                        ? machines.map((machine) => machine.label).join(" · ")
                        : "—"}
                    </td>
                    <td>
                      <span className="shift-workforce-badge shift-workforce-badge--active">
                        Planlanabilir
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <p className="shift-workforce-empty-inline">
          Vardiya planı salt okunurdur; üretim emri vardiya ataması Üretim modülünden yönetilir.
        </p>
      </section>
    </div>
  );
}
