"use client";

import { OperationalBadge, mapOperationalStatus } from "./operational-status";

export type LineTopologyStation = Readonly<{
  id: string;
  code: string;
  name: string;
  state: string;
  /** Optional customer label that replaces the state text (the state still drives the colour). */
  statusLabel?: string;
}>;

type LineTopologyProps = Readonly<{
  stations: readonly LineTopologyStation[];
  selectedStationId?: string | null;
  onSelectStation?: (stationId: string) => void;
}>;

export function LineTopology({ stations, selectedStationId, onSelectStation }: LineTopologyProps) {
  return (
    <div className="fact-line-topology" data-testid="fact-line-topology" role="list">
      {stations.map((station) => {
        const selected = station.id === selectedStationId;
        const interactive = Boolean(onSelectStation);

        if (interactive) {
          return (
            <div key={station.id} className="fact-station-cell" role="listitem">
              <button
                type="button"
                className="fact-station-row"
                data-selected={selected}
                onClick={() => onSelectStation?.(station.id)}
              >
                <span className="fact-station-row__code">{station.code}</span>
                <span className="fact-station-row__name">{station.name}</span>
                <span className="fact-station-row__status">
                  <OperationalBadge status={mapOperationalStatus(station.state)} label={station.statusLabel ?? station.state} />
                </span>
              </button>
            </div>
          );
        }

        return (
          <div key={station.id} className="fact-station-row" data-selected={selected} role="listitem">
            <span className="fact-station-row__code">{station.code}</span>
            <span className="fact-station-row__name">{station.name}</span>
            <span className="fact-station-row__status">
              <OperationalBadge status={mapOperationalStatus(station.state)} label={station.statusLabel ?? station.state} />
            </span>
          </div>
        );
      })}
    </div>
  );
}
