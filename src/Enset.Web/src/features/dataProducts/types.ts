export interface DataProductSummary { id: string; code: string; name: string; category: string; status: string; scope: string; scopeId: string | null; latestVersion: number | null }
export interface Availability { isAvailable: boolean; isAuthorized: boolean; hasRequiredInputData: boolean; missingInputs: string[]; warnings: string[] }
export interface ProductValue { key: string; numericValue: number | null; textValue: string | null; booleanValue: boolean | null; dateTimeValue: string | null; unit: string | null; quality: string }
export interface ProductVersion { dataProductId: string; version: number; status: string; generatedAt: string; periodFrom: string | null; periodTo: string | null; quality: string; generationStatus: string | null; warnings: string[]; values: ProductValue[] }
export interface VersionHistory { version: number; status: string; generatedAt: string; quality: string }
export interface GenerateDataProductResponse { generationRunId: string; status: string; dataProductId: string; version: number }
