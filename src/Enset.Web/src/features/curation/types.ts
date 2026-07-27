export type CurationTaskStatus = "Open" | "Accepted" | "Customized" | "Rejected";
export type CurationSource = "Import" | "User" | "EnsetSuggestion" | "System";

export interface CurationTask {
  id: string; entityType: string; entityId: string; entityDisplayName: string;
  fieldName: string; originalValue: string | null; suggestedValue: string;
  confidencePercent: number; reasoning: string; status: CurationTaskStatus;
  curatedValue: string | null; source: CurationSource;
  ruleId: string; ruleVersion: string; maturityLevel: "Bronze"|"Silver"|"Gold";
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
  rejectedTasks: number; buildingsWithoutUsageType: number;
  buildingsWithoutHeatedArea: number; buildingsWithoutPostalCode: number;
  meteringPointsWithoutUsageType: number; meteringPointsWithoutEnergyCarrier: number;
  meteringPointsWithIncompleteProfiles: number;
  taskGroups: CurationTaskGroup[];
}
export interface CurationTaskPage { items:CurationTask[];page:number;pageSize:number;totalCount:number;totalPages:number }
export interface FieldReadiness { fieldName:string;maturityLevel:"Bronze"|"Silver"|"Gold";required:boolean;satisfied:boolean;explanation:string;value:string|null;source:CurationSource|null }
export interface CurationReadiness { entityId:string;maturityLevel:"Bronze"|"Silver"|"Gold";readinessPercent:number;isGoldReady:boolean;fields:FieldReadiness[];blockingIssues:string[] }
export interface MeteringPointGoldProfile {
  usageType: string | null; energyCarrier: string | null;
  measurementPeriodStart: string | null; measurementPeriodEnd: string | null;
  intervalMinutes: number | null; expectedValueCount: number; actualValueCount: number;
  missingValueCount: number; invalidValueCount: number; estimatedValueCount: number;
  interpolatedValueCount: number; completenessPercentage: number;
}
