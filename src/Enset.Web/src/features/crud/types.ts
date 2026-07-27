export interface EntityMetadata {
  dataOrigin: string;
  createdAtUtc: string;
  createdByUserId: string | null;
  updatedAtUtc: string | null;
  updatedByUserId: string | null;
  isDeleted: boolean;
  rowVersion: number;
}

export interface EntityMutationResult extends EntityMetadata {
  id: string;
}

export interface AuditHistoryItem {
  changedAtUtc: string;
  changedByUserId: string;
  changeType: string;
  fieldName: string | null;
  oldValue: string | null;
  newValue: string | null;
  source: string;
  importId: string | null;
  reason: string | null;
}
