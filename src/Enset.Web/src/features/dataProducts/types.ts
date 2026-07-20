export interface DataProductSummary { id: string; code: string; name: string; category: string; status: string; scope: string; scopeId?: string; latestVersion?: number }
export interface Availability { isAvailable: boolean; isAuthorized: boolean; hasRequiredInputData: boolean; missingInputs: string[]; warnings: string[] }
export interface ProductValue { key: string; numericValue?: number; textValue?: string; unit?: string; quality: string }
export interface ProductVersion { dataProductId: string; version: number; status: string; generatedAt: string; periodFrom?: string; periodTo?: string; quality: string; generationStatus?: string; warnings: string[]; values: ProductValue[] }
export interface VersionHistory { version: number; status: string; generatedAt: string; quality: string }
