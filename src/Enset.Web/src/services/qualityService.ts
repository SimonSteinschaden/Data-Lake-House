import { apiGet, apiPost } from "../api/apiClient";
import type { PagedResult } from "../types/paging";

export interface ScopeQuality {
  scopeId: string;
  scopeType: string;
  level: "Bronze" | "Silver" | "Gold";
  blockingReasons: string[];
}

export interface MeterQualityAssessment {
  meterId: string;
  qualityLevel: "Bronze" | "Silver" | "Gold";
  profileAnalysisStatus: string;
  currentAnalysisId: string | null;
  analysisVersion: string | null;
  completenessPercentage: number | null;
  openIssueCount: number;
  blockingIssueCount: number;
  openAnomalyCount: number;
  lastAnalyzedAtUtc: string | null;
  confirmationStatus: string;
  blockingReasons: string[];
  warnings: string[];
  nextActions: string[];
}

export interface EnergySystemQualityAssessment {
  energySystemId: string;
  qualityLevel: "Bronze" | "Silver" | "Gold";
  confirmationStatus: string;
  missingRequirements: string[];
  blockingReasons: string[];
  warnings: string[];
  nextActions: string[];
}

export interface OperationalBuildingQuality {
  inventoryDeclarationStatus: string;
  nextActions: string[];
  noRelevantEnergySystemsConfirmed: boolean;
  meterAssessments: MeterQualityAssessment[];
  energySystemAssessments: EnergySystemQualityAssessment[];
  assessment: {
    buildingCoreQuality: "Bronze" | "Silver" | "Gold";
    overallQualityLevel: "Bronze" | "Silver" | "Gold";
    meterQualities: ScopeQuality[];
    energySystemQualities: ScopeQuality[];
    meterInventoryComplete: boolean;
    energySystemInventoryComplete: boolean;
    blockingReasons: string[];
    warnings: string[];
    annualConsumptionStatus: string;
    annualProductionStatus: string;
    goldProgress: {
      percentage: number;
      goldCount: number;
      silverCount: number;
      bronzeCount: number;
    };
  };
}

export interface MeterProfileAnalysis {
  id: string;
  meterId: string;
  versionNumber: number;
  periodFromUtc: string;
  periodToUtc: string;
  analysisStatus: string;
  analysisVersion: string;
  expectedReadingCount: number;
  actualReadingCount: number;
  completenessPercentage: number;
  gapCount: number;
  anomalyCount: number;
  blockingIssueCount: number;
  warningCount: number;
  detectedIntervalSeconds: number | null;
  detectedUnit: string | null;
  executedAtUtc: string;
  executedByDisplayNameSnapshot: string | null;
  summary: string | null;
  isCurrent: boolean;
}

export interface MeterProfileIssue {
  id: string;
  meterProfileAnalysisId: string;
  meterId: string;
  code: string;
  category: string;
  severity: string;
  message: string;
  technicalDetails: string | null;
  isBlocking: boolean;
  resolutionStatus: string;
  originalValue: string | null;
  expectedValue: string | null;
  createdAtUtc: string;
  resolvedAtUtc: string | null;
}

export interface MeterProfileCurationDecision {
  id: string;
  meterProfileIssueId: string;
  decisionType: string;
  previousValue: string | null;
  newValue: string | null;
  generatedValueMethod: string | null;
  confidencePercent: number | null;
  reason: string;
  comment: string | null;
  decidedByDisplayNameSnapshot: string | null;
  decidedAtUtc: string;
  isFinal: boolean;
}

export interface CurationDecisionRequest {
  decisionType: "ConfirmAsCorrect" | "CorrectValue" | "MarkInvalid" | "AcceptGap" |
    "AddManualValue" | "GenerateEstimatedValue" | "MarkForObservation" |
    "IgnoreWithReason" | "Reopen";
  previousValue?: string | null;
  newValue?: string | null;
  generatedValueMethod?: string | null;
  confidencePercent?: number | null;
  reason: string;
  comment?: string | null;
  isFinal: boolean;
  supersedesDecisionId?: string | null;
  displayName?: string | null;
}

export const qualityService = {
  building(id: string, signal?: AbortSignal) {
    return apiGet<OperationalBuildingQuality>(
      `/api/v1/buildings/${encodeURIComponent(id)}/quality-assessment`,
      { signal },
    );
  },
  meter(id: string, signal?: AbortSignal) {
    return apiGet<MeterQualityAssessment>(
      `/api/v1/meters/${encodeURIComponent(id)}/quality-assessment`,
      { signal },
    );
  },
  analysis(meterId: string, analysisId: string, signal?: AbortSignal) {
    return apiGet<MeterProfileAnalysis>(
      `/api/v1/meters/${encodeURIComponent(meterId)}/profile-analyses/${encodeURIComponent(analysisId)}`,
      { signal },
    );
  },
  analysisHistory(meterId: string, page = 1, pageSize = 20, signal?: AbortSignal) {
    return apiGet<PagedResult<MeterProfileAnalysis>>(
      `/api/v1/meters/${encodeURIComponent(meterId)}/profile-analyses?page=${page}&pageSize=${pageSize}`,
      { signal },
    );
  },
  startAnalysis(meterId: string, periodFromUtc: string, periodToUtc: string, analysisVersion: string) {
    return apiPost<MeterProfileAnalysis>(
      `/api/v1/meters/${encodeURIComponent(meterId)}/profile-analyses`,
      { periodFromUtc, periodToUtc, analysisVersion },
    );
  },
  issues(meterId: string, analysisId: string, page = 1, pageSize = 50, signal?: AbortSignal) {
    return apiGet<PagedResult<MeterProfileIssue>>(
      `/api/v1/meters/${encodeURIComponent(meterId)}/profile-analyses/${encodeURIComponent(analysisId)}` +
      `/issues?page=${page}&pageSize=${pageSize}`,
      { signal },
    );
  },
  decisions(meterId: string, analysisId: string, issueId: string, signal?: AbortSignal) {
    return apiGet<PagedResult<MeterProfileCurationDecision>>(
      `/api/v1/meters/${encodeURIComponent(meterId)}/profile-analyses/${encodeURIComponent(analysisId)}` +
      `/issues/${encodeURIComponent(issueId)}/decisions`,
      { signal },
    );
  },
  decide(meterId: string, analysisId: string, issueId: string, request: CurationDecisionRequest) {
    return apiPost<MeterProfileCurationDecision, CurationDecisionRequest>(
      `/api/v1/meters/${encodeURIComponent(meterId)}/profile-analyses/${encodeURIComponent(analysisId)}` +
      `/issues/${encodeURIComponent(issueId)}/decisions`,
      request,
    );
  },
};
