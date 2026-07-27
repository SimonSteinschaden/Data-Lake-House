export type CurationTaskStatus = "Open" | "Accepted" | "Customized" | "Rejected";
export type CurationSource = "Import" | "User" | "EnsetSuggestion" | "System";

export interface CurationTask {
  id: string; entityType: string; entityId: string; entityDisplayName: string;
  fieldName: string; originalValue: string | null; suggestedValue: string;
  confidencePercent: number; reasoning: string; status: CurationTaskStatus;
  curatedValue: string | null; source: CurationSource;
}
export interface CurationDecision {
  id: string; userId: string; decidedAtUtc: string; decision: CurationTaskStatus;
  originalValue: string | null; suggestedValue: string; newValue: string | null;
  source: CurationSource; confidencePercent: number; reason: string | null;
}
export interface CurationTaskDetail { task: CurationTask; decisions: CurationDecision[] }
export interface CurationTaskGroup { entityType: string; fieldName: string; count: number }
export interface CurationStatistics {
  bronze: number; silver: number; gold: number; openTasks: number;
  taskGroups: CurationTaskGroup[];
}
