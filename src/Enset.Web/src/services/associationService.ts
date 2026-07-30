import {apiGet,apiPatch,apiPost} from "../api";
export type EntityType="Customer"|"Building"|"Meter"|"EnergySystem"|"MeterSeries"|"Document"|"Project"|"ImportBatch";
export interface AssociationType {key:string;sourceEntityType:EntityType;targetEntityType:EntityType;sourceLabel:string;targetLabel:string;allowedRoles:string[];supportsMultipleSources:boolean;supportsMultipleTargets:boolean;supportsPrimary:boolean;supportsRole:boolean;supportsValidity:boolean;supportsHistory:boolean;sourceCardinality:string;targetCardinality:string;deleteBehavior:string;conflictRules:string[]}
export interface EntityItem {id:string;displayName:string;secondaryLabel:string|null;status:string;entityType:EntityType;keyFacts:Record<string,string|null>;currentAssignmentsCount:number;qualityLevel:string|null}
export interface EntityPage {items:EntityItem[];page:number;pageSize:number;totalCount:number;totalPages:number}
export interface PreviewRequest {associationType:string;sourceIds:string[];targetIds:string[];role:string|null;validFrom:string|null;validTo:string|null;isPrimary:boolean;confirmWarnings:boolean;reason:string|null}
export interface Conflict {code:string;severity:"Information"|"Warning"|"Blocking";message:string;sourceId:string|null;targetId:string|null}
export interface Preview {proposedAssignments:unknown[];existingAssignments:unknown[];conflicts:Conflict[];warnings:string[];affectedEntities:string[];impacts:string[];totalChanges:number;canCommit:boolean}
export interface CommandResult {operationId:string;created:number;updated:number;ended:number;skipped:number;warnings:string[]}
export interface Assignment {id:string;associationType:string;sourceId:string;targetId:string;sourceDisplayName:string|null;targetDisplayName:string|null;role:string|null;validFrom:string|null;validTo:string|null;isPrimary:boolean;isActive:boolean}
export interface AuditItem {id:string;operationId:string;changedAtUtc:string;changedByUserId:string;associationType:string;sourceId:string;targetId:string;action:string;before:string|null;after:string|null;reason:string|null}
export const associationService={
 types:()=>apiGet<AssociationType[]>("/api/v1/associations/types"),
 entities:(entityType:EntityType,search:string,page=1)=>apiGet<EntityPage>(`/api/v1/associations/entities?entityType=${entityType}&search=${encodeURIComponent(search)}&page=${page}&pageSize=25`),
 list:(type:string,history=false)=>apiGet<Assignment[]>(`/api/v1/associations?associationType=${encodeURIComponent(type)}&includeHistorical=${history}`),
 preview:(request:PreviewRequest)=>apiPost<Preview,PreviewRequest>("/api/v1/associations/preview",request),
 commit:(request:PreviewRequest)=>apiPost<CommandResult,PreviewRequest>("/api/v1/associations",request),
 removePreview:(type:string,ids:string[],confirmWarnings=false)=>apiPost<Preview,{associationType:string;associationIds:string[];endDate:string;reason:string;confirmWarnings:boolean}>("/api/v1/associations/remove-preview",{associationType:type,associationIds:ids,endDate:new Date().toISOString().slice(0,10),reason:"Manuell beendet",confirmWarnings}),
 remove:(type:string,ids:string[],confirmWarnings=false)=>apiPost<CommandResult,{associationType:string;associationIds:string[];endDate:string;reason:string;confirmWarnings:boolean}>("/api/v1/associations/remove",{associationType:type,associationIds:ids,endDate:new Date().toISOString().slice(0,10),reason:"Manuell beendet",confirmWarnings}),
 primary:(type:string,id:string)=>apiPatch<CommandResult,{associationType:string;associationId:string;reason:string;confirmWarnings:boolean}>(`/api/v1/associations/${encodeURIComponent(id)}/primary`,{associationType:type,associationId:id,reason:"Manuell als primär markiert",confirmWarnings:true}),
 history:(type:string)=>apiGet<AuditItem[]>(`/api/v1/associations/history?associationType=${encodeURIComponent(type)}`)
};
