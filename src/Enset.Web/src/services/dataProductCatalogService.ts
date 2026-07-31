import { apiGet } from "../api";
export interface ProductMetadata {code:string;name:string;germanName:string;description:string;category:string;version:{major:number;minor:number;patch:number};owner:string;inputs:string[];usedProducts:string[];outputSchema:string[];dataSource:string;snapshotVersion:string;qualityLevel:string;suitability:string;refresh:string;supportedExports:string[];apiEndpoint:string;period:string;aggregationLevel:string;missingDataBehavior:string;lineage:string}
export interface CatalogItem {metadata:ProductMetadata;lastUpdatedUtc:string}
export interface ProductPreview {metadata:ProductMetadata;generatedAtUtc:string;rows:Record<string,unknown>[]}
export const dataProductCatalogService={
 list:()=>apiGet<CatalogItem[]>("/api/v1/data-product-catalog"),
 get:(code:string)=>apiGet<CatalogItem>(`/api/v1/data-product-catalog/${encodeURIComponent(code)}`),
 preview:(code:string)=>apiGet<ProductPreview>(`/api/v1/data-product-catalog/${encodeURIComponent(code)}/preview?limit=25`),
 exportUrl:(code:string,format:string)=>`/api/v1/data-product-catalog/${encodeURIComponent(code)}/export?format=${encodeURIComponent(format)}`
};
