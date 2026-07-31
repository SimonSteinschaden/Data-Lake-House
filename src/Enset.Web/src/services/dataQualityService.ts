import { apiGet } from "../api";
export interface QualityMetric {code:string;name:string;description:string;severity:string;count:number;trend:string;affectedCustomers:number;affectedBuildings:number;affectedMeters:number;action:string;actionUrl:string}
export interface QualityDashboard {calculatedAtUtc:string;customerCompleteness:number;buildingCompleteness:number;meterCompleteness:number;metrics:QualityMetric[];qualityLevels:{level:string;count:number}[];suitability:{useCase:string;suitable:number;notSuitable:number}[];openImportIssues:number;openDataReviews:number}
export const dataQualityService={dashboard:(signal?:AbortSignal)=>apiGet<QualityDashboard>("/api/v1/data-quality/dashboard",{signal})};
