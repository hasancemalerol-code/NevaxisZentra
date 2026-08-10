import { buildOperationalAttentionItems } from "../presentation/edge-attention";
import type { EdgeDeliveryHealthReadModel } from "../presentation/edge-reliability-fixtures";
import type {
  EdgeMachineDetailBundle,
  MachineRegistryReadModel,
} from "../domain/edge-types";

interface EdgeOperationalAttentionProps {
  machines: MachineRegistryReadModel[];
  machineDetails: Record<string, EdgeMachineDetailBundle>;
  deliveryHealth: EdgeDeliveryHealthReadModel;
}

export function EdgeOperationalAttention({
  machines,
  machineDetails,
  deliveryHealth,
}: EdgeOperationalAttentionProps) {
  const items = buildOperationalAttentionItems(
    machines,
    machineDetails,
    deliveryHealth,
  );

  return (
    <section className="edge-attention-panel" data-testid="edge-operational-attention">
      <h2 className="edge-section-title edge-section-title--compact">Operasyonel Dikkat</h2>
      {items.length === 0 ? (
        <p className="edge-attention-panel__empty">
          Şu anda kritik operasyonel uyarı bulunmuyor.
        </p>
      ) : (
        <ul className="edge-attention-list">
          {items.map((item) => (
            <li
              key={item.id}
              className={`edge-attention-item edge-attention-item--${item.severity}`}
            >
              <span className="edge-attention-item__label">{item.label}</span>
              {item.machineCode && (
                <span className="edge-attention-item__machine">{item.machineCode}</span>
              )}
              {item.detail && (
                <span className="edge-attention-item__detail">{item.detail}</span>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
