import type { EntityMetadata } from "../crud/types";
export interface MeterReading extends EntityMetadata {
  id: string; meterId: string; timestamp: string; value: number;
  readingType: string; qualityFlag: string; intervalSeconds: number | null;
}
export interface MeterReadingWriteModel {
  meterId: string; timestamp: string; value: number; readingType: string;
  qualityFlag: string; intervalSeconds: number | null; rowVersion: number; reason: string | null;
}
