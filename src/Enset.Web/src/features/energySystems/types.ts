import type { EntityMetadata } from "../crud/types";

export interface EnergySystem extends EntityMetadata {
  id: string; energySystemNumber: string; name: string; type: string;
  buildingId: string; ratedPowerKw: number | null; commissionedAt: string | null;
  decommissionedAt: string | null; isActive: boolean;
}
export interface EnergySystemWriteModel {
  energySystemNumber: string; name: string; type: string; buildingId: string;
  ratedPowerKw: number | null; commissionedAt: string | null;
  decommissionedAt: string | null; rowVersion: number;
}
