using System.Text.Json;
using Enset.Application.Associations;
using Enset.Domain.Associations;
using Enset.Domain.Common;
using Enset.Domain.Customers;
using Enset.Domain.Energy;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Associations;

public sealed class EfAssociationService(EnsetDbContext db, TimeProvider timeProvider) : IAssociationService
{
    public const string CustomerBuilding = "customer-building";
    public const string BuildingMeter = "building-meter";
    public const string BuildingEnergySystem = "building-energy-system";
    public const string MeterSeries = "meter-series";
    public const string BuildingDocument = "building-document";
    public const string CustomerProject = "customer-project";

    private static readonly IReadOnlyList<AssociationTypeDefinition> Definitions =
    [
        D(CustomerBuilding,AssociationEntityType.Customer,AssociationEntityType.Building,"Kunden","Objekte",
            ["Owner","Operator","PropertyManager","Tenant","ContractingPartner"],true,true,true,true),
        D(BuildingMeter,AssociationEntityType.Building,AssociationEntityType.Meter,"Objekte","Zählpunkte",
            ["MainMeter","SubMeter","GenerationMeter","SumMeter","ControlMeter"],false,true,true,true,"1","n"),
        D(BuildingEnergySystem,AssociationEntityType.Building,AssociationEntityType.EnergySystem,"Objekte","Anlagen",
            ["LocatedAt","Supplies","GeneratesFor","StoresEnergyFor","SharedSystem","Other"],false,true,true,true,"1","n"),
        D(MeterSeries,AssociationEntityType.Meter,AssociationEntityType.MeterSeries,"Zählpunkte","Messreihen",
            ["MeasurementSeries"],false,false,false,false,"1","1"),
        D(BuildingDocument,AssociationEntityType.Building,AssociationEntityType.Document,"Objekte","Dokumente",
            ["EnergyCertificate","Plan","Invoice","InspectionReport","Contract","Photo","Other"],true,true,false,true),
        D(CustomerProject,AssociationEntityType.Customer,AssociationEntityType.Project,"Kunden","Projekte",
            ["Client","Partner","Operator","Owner"],true,true,true,true)
    ];

    public IReadOnlyList<AssociationTypeDefinition> Types() => Definitions;

    public async Task<AssociationEntityPage> SearchEntities(AssociationEntityQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page); var size = Math.Clamp(q.PageSize, 1, 100);
        var search = q.Search?.Trim().ToLower();
        IQueryable<AssociationEntityListItem> query = q.EntityType switch
        {
            AssociationEntityType.Customer => db.Customers.AsNoTracking()
                .Where(x => !q.IsActive.HasValue || x.IsActive == q.IsActive)
                .Where(x => search == null || x.Name.ToLower().Contains(search) ||
                            x.CustomerNumber.ToLower().Contains(search) ||
                            (x.City != null && x.City.ToLower().Contains(search)))
                .Select(x => new AssociationEntityListItem(x.Id,x.Name,x.CustomerNumber,x.IsActive?"Aktiv":"Inaktiv",q.EntityType,
                    new Dictionary<string,string?>{{"Ort",x.City},{"Typ",x.Type.ToString()}},
                    x.BuildingAssignments.Count(a=>a.ValidTo==null),null)),
            AssociationEntityType.Building => db.Buildings.AsNoTracking()
                .Where(x => !q.IsActive.HasValue || x.IsActive == q.IsActive)
                .Where(x => search == null || x.Name.ToLower().Contains(search) ||
                            x.BuildingNumber.ToLower().Contains(search))
                .Select(x => new AssociationEntityListItem(x.Id,x.Name,x.BuildingNumber,x.IsActive?"Aktiv":"Inaktiv",q.EntityType,
                    new Dictionary<string,string?>{{"Nummer",x.BuildingNumber}},
                    x.CustomerAssignments.Count(a=>a.ValidTo==null),null)),
            AssociationEntityType.Meter => db.Meters.AsNoTracking()
                .Where(x => !q.IsActive.HasValue || x.IsActive == q.IsActive)
                .Where(x => !q.RelatedEntityId.HasValue || x.BuildingId == q.RelatedEntityId)
                .Where(x => search == null || x.Name.ToLower().Contains(search) ||
                            x.MeterNumber.ToLower().Contains(search) ||
                            (x.ExternalIdentifier != null && x.ExternalIdentifier.ToLower().Contains(search)))
                .Select(x => new AssociationEntityListItem(x.Id,x.MeterNumber,x.Name,x.IsActive?"Aktiv":"Inaktiv",q.EntityType,
                    new Dictionary<string,string?>{{"Energieträger",x.Medium.ToString()},{"Richtung",x.Direction.ToString()}},
                    x.BuildingAssignments.Count(a=>a.ValidTo==null),null)),
            AssociationEntityType.EnergySystem => db.EnergySystems.AsNoTracking()
                .Where(x => !q.IsActive.HasValue || x.IsActive == q.IsActive)
                .Where(x => search == null || x.Name.ToLower().Contains(search) ||
                            x.EnergySystemNumber.ToLower().Contains(search))
                .Select(x => new AssociationEntityListItem(x.Id,x.Name,x.EnergySystemNumber,x.IsActive?"Aktiv":"Inaktiv",q.EntityType,
                    new Dictionary<string,string?>{{"Typ",x.Type.ToString()}},
                    x.BuildingAssignments.Count(a=>a.ValidTo==null),null)),
            AssociationEntityType.MeterSeries => db.Meters.AsNoTracking()
                .Where(x => x.Readings.Any())
                .Where(x => search == null || x.MeterNumber.ToLower().Contains(search))
                .Select(x => new AssociationEntityListItem(x.Id,$"Messreihe {x.MeterNumber}",
                    x.Readings.Min(r=>r.Timestamp)+" – "+x.Readings.Max(r=>r.Timestamp),"Aktiv",q.EntityType,
                    new Dictionary<string,string?>{{"Einheit",x.Unit.ToString()},{"Intervall",x.Readings.Select(r=>r.IntervalSeconds).FirstOrDefault().ToString()}},
                    1,null)),
            AssociationEntityType.Document => db.Documents.AsNoTracking()
                .Where(x => search == null || x.FilePath.ToLower().Contains(search))
                .Select(x => new AssociationEntityListItem(x.Id,Path.GetFileName(x.FilePath),x.Type.ToString(),"Aktiv",q.EntityType,
                    new Dictionary<string,string?>{{"Typ",x.Type.ToString()},{"Datum",x.UploadedAt.ToString()}},
                    x.BuildingAssignments.Count(a=>a.ValidTo==null),null)),
            AssociationEntityType.Project => db.Projects.AsNoTracking()
                .Where(x => search == null || x.Name.ToLower().Contains(search))
                .Select(x => new AssociationEntityListItem(x.Id,x.Name,x.Status.ToString(),"Aktiv",q.EntityType,
                    new Dictionary<string,string?>{{"Status",x.Status.ToString()},{"Beginn",x.StartDate.ToString()}},
                    x.CustomerAssignments.Count(a=>a.ValidTo==null),null)),
            AssociationEntityType.ImportBatch => db.ImportReports.AsNoTracking()
                .Where(x => search == null || (x.SourceFileName != null && x.SourceFileName.ToLower().Contains(search)))
                .Select(x => new AssociationEntityListItem(x.ImportId,x.SourceFileName ?? "Import",x.Status,"Import",q.EntityType,
                    new Dictionary<string,string?>{{"Status",x.Status}},0,null)),
            _ => throw new ArgumentOutOfRangeException()
        };
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(items,page,size,total,(int)Math.Ceiling(total/(double)size));
    }

    public async Task<IReadOnlyList<AssociationListItem>> List(string type, Guid? sourceId,
        Guid? targetId, DateOnly? validAt, bool historical, CancellationToken ct)
    {
        var date = validAt ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return type switch
        {
            CustomerBuilding => await db.CustomerBuildingAssignments.AsNoTracking()
                .Where(x=>(!sourceId.HasValue||x.CustomerId==sourceId)&&(!targetId.HasValue||x.BuildingId==targetId))
                .Where(x=>historical||(x.ValidFrom<=date.ToDateTime(TimeOnly.MinValue)&&(x.ValidTo==null||x.ValidTo>=date.ToDateTime(TimeOnly.MinValue))))
                .Select(x=>new AssociationListItem(x.Id,type,x.CustomerId,x.BuildingId,x.Customer.Name,x.Building.Name,x.Role.ToString(),DateOnly.FromDateTime(x.ValidFrom),x.ValidTo==null?null:DateOnly.FromDateTime(x.ValidTo.Value),x.IsPrimary,x.ValidTo==null)).ToArrayAsync(ct),
            BuildingMeter => await db.BuildingMeterAssignments.AsNoTracking().Where(x=>(!sourceId.HasValue||x.BuildingId==sourceId)&&(!targetId.HasValue||x.MeterId==targetId)).Where(x=>historical||x.ValidTo==null).Select(x=>new AssociationListItem(x.Id,type,x.BuildingId,x.MeterId,x.Building.Name,x.Meter.MeterNumber,x.Role.ToString(),x.ValidFrom,x.ValidTo,x.IsPrimary,x.ValidTo==null)).ToArrayAsync(ct),
            BuildingEnergySystem => await db.EnergySystemBuildingAssignments.AsNoTracking().Where(x=>(!sourceId.HasValue||x.BuildingId==sourceId)&&(!targetId.HasValue||x.EnergySystemId==targetId)).Where(x=>historical||x.ValidTo==null).Select(x=>new AssociationListItem(x.Id,type,x.BuildingId,x.EnergySystemId,x.Building.Name,x.EnergySystem.Name,x.Role.ToString(),DateOnly.FromDateTime(x.ValidFrom),x.ValidTo==null?null:DateOnly.FromDateTime(x.ValidTo.Value),x.IsPrimary,x.ValidTo==null)).ToArrayAsync(ct),
            BuildingDocument => await db.BuildingDocumentAssignments.AsNoTracking().Where(x=>(!sourceId.HasValue||x.BuildingId==sourceId)&&(!targetId.HasValue||x.DocumentId==targetId)).Where(x=>historical||x.ValidTo==null).Select(x=>new AssociationListItem(x.Id,type,x.BuildingId,x.DocumentId,x.Building.Name,x.Document.FilePath,x.Role.ToString(),x.ValidFrom,x.ValidTo,false,x.ValidTo==null)).ToArrayAsync(ct),
            CustomerProject => await db.CustomerProjectAssignments.AsNoTracking().Where(x=>(!sourceId.HasValue||x.CustomerId==sourceId)&&(!targetId.HasValue||x.ProjectId==targetId)).Where(x=>historical||x.ValidTo==null).Select(x=>new AssociationListItem(x.Id,type,x.CustomerId,x.ProjectId,x.Customer.Name,x.Project.Name,x.Role.ToString(),x.ValidFrom,x.ValidTo,x.IsPrimary,x.ValidTo==null)).ToArrayAsync(ct),
            MeterSeries => await db.Meters.AsNoTracking().Where(x=>x.Readings.Any()&&(!sourceId.HasValue||x.Id==sourceId)&&(!targetId.HasValue||x.Id==targetId)).Select(x=>new AssociationListItem(x.Id,type,x.Id,x.Id,x.MeterNumber,"Messreihe "+x.MeterNumber,"MeasurementSeries",null,null,true,true)).ToArrayAsync(ct),
            _ => []
        };
    }

    public async Task<AssociationPreviewResponse> Preview(AssociationPreviewRequest request, CancellationToken ct)
    {
        var definition = Definition(request.AssociationType);
        var conflicts = new List<AssociationConflict>();
        if (request.SourceIds.Count==0||request.TargetIds.Count==0)
            conflicts.Add(C("EMPTY_SELECTION","Quelle und Ziel müssen ausgewählt sein."));
        if (request.ValidFrom.HasValue&&request.ValidTo.HasValue&&request.ValidTo<request.ValidFrom)
            conflicts.Add(C("INVALID_VALIDITY","Gültig bis darf nicht vor Gültig von liegen."));
        if (definition.SupportsRole && (string.IsNullOrWhiteSpace(request.Role) ||
            !definition.AllowedRoles.Contains(request.Role,StringComparer.OrdinalIgnoreCase)))
            conflicts.Add(C("INVALID_ROLE","Die Rolle ist für diesen Zuordnungstyp nicht zulässig."));
        if (!definition.SupportsMultipleSources&&request.SourceIds.Count>1)
            conflicts.Add(C("SOURCE_CARDINALITY","Dieser Typ erlaubt nur eine Quelle."));
        if (!definition.SupportsMultipleTargets&&request.TargetIds.Count>1)
            conflicts.Add(C("TARGET_CARDINALITY","Dieser Typ erlaubt nur ein Ziel."));
        if (request.AssociationType==MeterSeries && request.SourceIds.Zip(request.TargetIds,(s,t)=>s==t).Any(x=>!x))
            conflicts.Add(C("SERIES_METER_IMMUTABLE","Messreihen sind über MeterId gebunden und dürfen nicht einem anderen Zählpunkt zugeordnet werden."));
        var proposed = (from s in request.SourceIds from t in request.TargetIds
            select new ProposedAssociation(s,t,request.Role,request.ValidFrom,request.ValidTo,request.IsPrimary,"New")).ToArray();
        var existing = new List<ProposedAssociation>();
        foreach(var p in proposed)
        {
            if (!await Exists(definition.SourceEntityType,p.SourceId,ct)||!await Exists(definition.TargetEntityType,p.TargetId,ct))
                conflicts.Add(C("REFERENCE_NOT_FOUND","Quelle oder Ziel wurde nicht gefunden.",p.SourceId,p.TargetId));
            var matches=await List(request.AssociationType,p.SourceId,p.TargetId,null,false,ct);
            if(matches.Count>0){existing.AddRange(matches.Select(x=>new ProposedAssociation(x.SourceId,x.TargetId,x.Role,x.ValidFrom,x.ValidTo,x.IsPrimary,"Existing")));conflicts.Add(new("IDENTICAL_ASSIGNMENT",AssociationConflictSeverity.Information,"Die Zuordnung besteht bereits.",p.SourceId,p.TargetId));}
            else if(await HasValidityOverlap(request.AssociationType,p.SourceId,p.TargetId,
                        request.ValidFrom,request.ValidTo,ct))
                conflicts.Add(C("VALIDITY_OVERLAP",
                    "Der Gültigkeitszeitraum überschneidet eine bestehende Zuordnung.",
                    p.SourceId,p.TargetId));
            if(request.IsPrimary&&definition.SupportsPrimary)
            {
                var current=await List(request.AssociationType,null,p.TargetId,null,false,ct);
                if(current.Any(x=>x.IsPrimary&&x.SourceId!=p.SourceId))
                    conflicts.Add(new("PRIMARY_CONFLICT",AssociationConflictSeverity.Warning,"Eine konkurrierende Primärzuordnung wird ersetzt.",p.SourceId,p.TargetId));
            }
            if(request.AssociationType is BuildingMeter or BuildingEnergySystem)
            {
                var current=await List(request.AssociationType,null,p.TargetId,null,false,ct);
                if(current.Any(x=>x.SourceId!=p.SourceId))
                    conflicts.Add(new("TARGET_ALREADY_ASSIGNED",AssociationConflictSeverity.Warning,"Das Ziel ist bereits einem anderen Objekt zugeordnet.",p.SourceId,p.TargetId));
            }
        }
        var blocking=conflicts.Any(x=>x.Severity==AssociationConflictSeverity.Blocking);
        var warnings=conflicts.Where(x=>x.Severity==AssociationConflictSeverity.Warning).Select(x=>x.Message).Distinct().ToArray();
        return new(proposed,existing,conflicts,warnings,
            request.SourceIds.Concat(request.TargetIds).Distinct().ToArray(),
            ["Canonical Snapshots und darauf basierende Data Products werden bei der nächsten Abfrage neu projiziert."],
            proposed.Length-existing.Count,!blocking&&(warnings.Length==0||request.ConfirmWarnings));
    }

    public async Task<AssociationCommandResponse> Commit(AssociationPreviewRequest request, Guid userId, CancellationToken ct)
    {
        var preview=await Preview(request,ct);
        if(!preview.CanCommit) throw new AssociationValidationException(preview);
        if(request.AssociationType==MeterSeries) return new(Guid.NewGuid(),0,0,0,preview.ExistingAssignments.Count,preview.Warnings);
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var operation=Guid.NewGuid();var created=0;var skipped=0;var today=request.ValidFrom??DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        foreach(var p in preview.ProposedAssignments)
        {
            if(preview.ExistingAssignments.Any(x=>x.SourceId==p.SourceId&&x.TargetId==p.TargetId)){skipped++;continue;}
            if(request.IsPrimary) await ClearPrimary(request.AssociationType,p.TargetId,operation,userId,request.Reason,ct);
            switch(request.AssociationType)
            {
                case CustomerBuilding: db.CustomerBuildingAssignments.Add(new(){CustomerId=p.SourceId,BuildingId=p.TargetId,Role=Enum.Parse<CustomerBuildingRole>(request.Role!,true),ValidFrom=today.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc),ValidTo=request.ValidTo?.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc),IsPrimary=request.IsPrimary});break;
                case BuildingMeter:
                    db.BuildingMeterAssignments.Add(new(){BuildingId=p.SourceId,MeterId=p.TargetId,Role=Enum.Parse<BuildingMeterRole>(request.Role!,true),ValidFrom=today,ValidTo=request.ValidTo,IsPrimary=request.IsPrimary});
                    (await db.Meters.FindAsync([p.TargetId],ct))!.BuildingId=p.SourceId;break;
                case BuildingEnergySystem: db.EnergySystemBuildingAssignments.Add(new(){BuildingId=p.SourceId,EnergySystemId=p.TargetId,Role=Enum.Parse<EnergySystemBuildingRole>(request.Role!,true),ValidFrom=today.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc),ValidTo=request.ValidTo?.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc),IsPrimary=request.IsPrimary});break;
                case BuildingDocument: db.BuildingDocumentAssignments.Add(new(){BuildingId=p.SourceId,DocumentId=p.TargetId,Role=Enum.Parse<BuildingDocumentRole>(request.Role!,true),ValidFrom=request.ValidFrom,ValidTo=request.ValidTo});break;
                case CustomerProject:
                    db.CustomerProjectAssignments.Add(new(){CustomerId=p.SourceId,ProjectId=p.TargetId,Role=Enum.Parse<CustomerProjectRole>(request.Role!,true),ValidFrom=request.ValidFrom,ValidTo=request.ValidTo,IsPrimary=request.IsPrimary});
                    (await db.Projects.FindAsync([p.TargetId],ct))!.CustomerId=p.SourceId;break;
            }
            Audit(operation,userId,request.AssociationType,p.SourceId,p.TargetId,"Created",null,p,request.Reason);created++;
        }
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return new(operation,created,0,0,skipped,preview.Warnings);
    }

    public async Task<AssociationPreviewResponse> RemovePreview(RemoveAssociationRequest request, CancellationToken ct)
    {
        var current=await List(request.AssociationType,null,null,null,true,ct);
        var selected=current.Where(x=>request.AssociationIds.Contains(x.Id)).ToArray();
        var conflicts=new List<AssociationConflict>();
        if(request.AssociationType==MeterSeries) conflicts.Add(C("SERIES_REQUIRED","Eine Messreihe darf nicht von ihrem internen Zählpunkt getrennt werden."));
        if(selected.Length!=request.AssociationIds.Distinct().Count()) conflicts.Add(C("REFERENCE_NOT_FOUND","Mindestens eine Zuordnung wurde nicht gefunden."));
        var warnings=selected.Where(x=>x.IsPrimary).Select(x=>"Eine Primärzuordnung wird beendet.").Distinct().ToArray();
        return new([],selected.Select(x=>new ProposedAssociation(x.SourceId,x.TargetId,x.Role,x.ValidFrom,x.ValidTo,x.IsPrimary,"End")).ToArray(),
            conflicts,warnings,selected.SelectMany(x=>new[]{x.SourceId,x.TargetId}).Distinct().ToArray(),
            ["Suitability und Data-Product-Berechenbarkeit können sinken."],selected.Length,
            conflicts.Count==0&&(warnings.Length==0||request.ConfirmWarnings));
    }

    public async Task<AssociationCommandResponse> Remove(RemoveAssociationRequest request, Guid userId, CancellationToken ct)
    {
        var preview=await RemovePreview(request,ct);if(!preview.CanCommit)throw new AssociationValidationException(preview);
        await using var tx=await db.Database.BeginTransactionAsync(ct);var op=Guid.NewGuid();var ended=0;
        foreach(var id in request.AssociationIds.Distinct())
        {
            var end=request.EndDate??DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            switch(request.AssociationType)
            {
                case CustomerBuilding:{var x=await db.CustomerBuildingAssignments.FindAsync([id],ct);if(x==null)continue;var before=new{x.ValidTo,x.IsPrimary};x.ValidTo=end.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);x.IsPrimary=false;Audit(op,userId,request.AssociationType,x.CustomerId,x.BuildingId,"Ended",before,new{x.ValidTo,x.IsPrimary},request.Reason);break;}
                case BuildingMeter:{var x=await db.BuildingMeterAssignments.FindAsync([id],ct);if(x==null)continue;var before=new{x.ValidTo,x.IsPrimary};x.ValidTo=end;x.IsPrimary=false;var meter=await db.Meters.FindAsync([x.MeterId],ct);if(meter?.BuildingId==x.BuildingId)meter.BuildingId=null;Audit(op,userId,request.AssociationType,x.BuildingId,x.MeterId,"Ended",before,new{x.ValidTo,x.IsPrimary},request.Reason);break;}
                case BuildingEnergySystem:{var x=await db.EnergySystemBuildingAssignments.FindAsync([id],ct);if(x==null)continue;var before=new{x.ValidTo,x.IsPrimary};x.ValidTo=end.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);x.IsPrimary=false;Audit(op,userId,request.AssociationType,x.BuildingId,x.EnergySystemId,"Ended",before,new{x.ValidTo,x.IsPrimary},request.Reason);break;}
                case BuildingDocument:{var x=await db.BuildingDocumentAssignments.FindAsync([id],ct);if(x==null)continue;var before=new{x.ValidTo};x.ValidTo=end;Audit(op,userId,request.AssociationType,x.BuildingId,x.DocumentId,"Ended",before,new{x.ValidTo},request.Reason);break;}
                case CustomerProject:{var x=await db.CustomerProjectAssignments.FindAsync([id],ct);if(x==null)continue;var before=new{x.ValidTo,x.IsPrimary};x.ValidTo=end;x.IsPrimary=false;Audit(op,userId,request.AssociationType,x.CustomerId,x.ProjectId,"Ended",before,new{x.ValidTo,x.IsPrimary},request.Reason);break;}
            }ended++;
        }
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(op,0,0,ended,0,preview.Warnings);
    }

    public async Task<AssociationCommandResponse> SetPrimary(SetPrimaryAssociationRequest request, Guid userId, CancellationToken ct)
    {
        var item=(await List(request.AssociationType,null,null,null,false,ct)).FirstOrDefault(x=>x.Id==request.AssociationId)
            ??throw new KeyNotFoundException("Association not found.");
        var competing=(await List(request.AssociationType,null,item.TargetId,null,false,ct))
            .Where(x=>x.IsPrimary&&x.Id!=item.Id).ToArray();
        if(competing.Length>0&&!request.ConfirmWarnings)
            throw new AssociationValidationException(new([],[],[
                new("PRIMARY_CONFLICT",AssociationConflictSeverity.Warning,
                    "Eine bestehende Primärzuordnung würde ersetzt.",item.SourceId,item.TargetId)],
                ["Eine bestehende Primärzuordnung würde ersetzt."],
                [item.SourceId,item.TargetId],["Primärstatus wird atomar gewechselt."],1,false));
        await using var tx=await db.Database.BeginTransactionAsync(ct);var op=Guid.NewGuid();
        await ClearPrimary(request.AssociationType,item.TargetId,op,userId,request.Reason,ct);
        switch(request.AssociationType){case CustomerBuilding:(await db.CustomerBuildingAssignments.FindAsync([item.Id],ct))!.IsPrimary=true;break;case BuildingMeter:(await db.BuildingMeterAssignments.FindAsync([item.Id],ct))!.IsPrimary=true;break;case BuildingEnergySystem:(await db.EnergySystemBuildingAssignments.FindAsync([item.Id],ct))!.IsPrimary=true;break;case CustomerProject:(await db.CustomerProjectAssignments.FindAsync([item.Id],ct))!.IsPrimary=true;break;default:throw new InvalidOperationException("Primary assignment is not supported.");}
        Audit(op,userId,request.AssociationType,item.SourceId,item.TargetId,"MarkedPrimary",item,new{item.Id,IsPrimary=true},request.Reason);
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(op,0,1,0,0,[]);
    }

    public async Task<IReadOnlyList<AssociationAuditItem>> History(string? type, Guid? sourceId, Guid? targetId, CancellationToken ct)=>
        await db.AssociationAuditEntries.AsNoTracking().Where(x=>(type==null||x.AssociationType==type)&&(!sourceId.HasValue||x.SourceId==sourceId)&&(!targetId.HasValue||x.TargetId==targetId)).OrderByDescending(x=>x.ChangedAtUtc).Select(x=>new AssociationAuditItem(x.Id,x.OperationId,x.ChangedAtUtc,x.ChangedByUserId,x.AssociationType,x.SourceId,x.TargetId,x.Action,x.Before,x.After,x.Reason)).ToArrayAsync(ct);

    private static AssociationTypeDefinition Definition(string key)=>Definitions.FirstOrDefault(x=>x.Key.Equals(key,StringComparison.OrdinalIgnoreCase))??throw new ArgumentException($"Unknown association type '{key}'.");
    private static AssociationTypeDefinition D(string key,AssociationEntityType s,AssociationEntityType t,string sl,string tl,string[] roles,bool ms,bool mt,bool primary,bool validity,string sc="n",string tc="n")=>new(key,s,t,sl,tl,roles,ms,mt,primary,roles.Length>0,validity,true,sc,tc,"EndValidity",["Duplicate","PrimaryConflict","Cardinality","ValidityOverlap","InactiveEntity"]);
    private static AssociationConflict C(string code,string message,Guid? s=null,Guid? t=null)=>new(code,AssociationConflictSeverity.Blocking,message,s,t);
    private async Task<bool> Exists(AssociationEntityType type,Guid id,CancellationToken ct)=>type switch{AssociationEntityType.Customer=>await db.Customers.AnyAsync(x=>x.Id==id&&x.IsActive,ct),AssociationEntityType.Building=>await db.Buildings.AnyAsync(x=>x.Id==id&&x.IsActive,ct),AssociationEntityType.Meter=>await db.Meters.AnyAsync(x=>x.Id==id&&x.IsActive,ct),AssociationEntityType.EnergySystem=>await db.EnergySystems.AnyAsync(x=>x.Id==id&&x.IsActive,ct),AssociationEntityType.MeterSeries=>await db.MeterReadings.AnyAsync(x=>x.MeterId==id,ct),AssociationEntityType.Document=>await db.Documents.AnyAsync(x=>x.Id==id,ct),AssociationEntityType.Project=>await db.Projects.AnyAsync(x=>x.Id==id,ct),AssociationEntityType.ImportBatch=>await db.ImportReports.AnyAsync(x=>x.ImportId==id,ct),_=>false};
    private async Task ClearPrimary(string type,Guid target,Guid op,Guid user,string? reason,CancellationToken ct)
    {
        if(type==CustomerBuilding){foreach(var x in await db.CustomerBuildingAssignments.Where(x=>x.BuildingId==target&&x.IsPrimary&&x.ValidTo==null).ToArrayAsync(ct)){x.IsPrimary=false;Audit(op,user,type,x.CustomerId,x.BuildingId,"UnmarkedPrimary",new{IsPrimary=true},new{IsPrimary=false},reason);}}
        else if(type==BuildingMeter){foreach(var x in await db.BuildingMeterAssignments.Where(x=>x.MeterId==target&&x.IsPrimary&&x.ValidTo==null).ToArrayAsync(ct)){x.IsPrimary=false;Audit(op,user,type,x.BuildingId,x.MeterId,"UnmarkedPrimary",new{IsPrimary=true},new{IsPrimary=false},reason);}}
        else if(type==BuildingEnergySystem){foreach(var x in await db.EnergySystemBuildingAssignments.Where(x=>x.EnergySystemId==target&&x.IsPrimary&&x.ValidTo==null).ToArrayAsync(ct)){x.IsPrimary=false;Audit(op,user,type,x.BuildingId,x.EnergySystemId,"UnmarkedPrimary",new{IsPrimary=true},new{IsPrimary=false},reason);}}
        else if(type==CustomerProject){foreach(var x in await db.CustomerProjectAssignments.Where(x=>x.ProjectId==target&&x.IsPrimary&&x.ValidTo==null).ToArrayAsync(ct)){x.IsPrimary=false;Audit(op,user,type,x.CustomerId,x.ProjectId,"UnmarkedPrimary",new{IsPrimary=true},new{IsPrimary=false},reason);}}
    }
    private async Task<bool> HasValidityOverlap(string type,Guid source,Guid target,
        DateOnly? from,DateOnly? to,CancellationToken ct)
    {
        var start=from??DateOnly.MinValue;var end=to??DateOnly.MaxValue;
        var startUtc=start.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);
        var endUtc=end.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);
        return type switch
        {
            CustomerBuilding=>await db.CustomerBuildingAssignments.AnyAsync(x=>
                x.CustomerId==source&&x.BuildingId==target&&x.ValidFrom<=endUtc&&
                (x.ValidTo==null||x.ValidTo>=startUtc),ct),
            BuildingMeter=>await db.BuildingMeterAssignments.AnyAsync(x=>
                x.BuildingId==source&&x.MeterId==target&&x.ValidFrom<=end&&
                (x.ValidTo==null||x.ValidTo>=start),ct),
            BuildingEnergySystem=>await db.EnergySystemBuildingAssignments.AnyAsync(x=>
                x.BuildingId==source&&x.EnergySystemId==target&&x.ValidFrom<=endUtc&&
                (x.ValidTo==null||x.ValidTo>=startUtc),ct),
            BuildingDocument=>await db.BuildingDocumentAssignments.AnyAsync(x=>
                x.BuildingId==source&&x.DocumentId==target&&
                (x.ValidFrom==null||x.ValidFrom<=end)&&(x.ValidTo==null||x.ValidTo>=start),ct),
            CustomerProject=>await db.CustomerProjectAssignments.AnyAsync(x=>
                x.CustomerId==source&&x.ProjectId==target&&
                (x.ValidFrom==null||x.ValidFrom<=end)&&(x.ValidTo==null||x.ValidTo>=start),ct),
            _=>false
        };
    }
    private void Audit(Guid op,Guid user,string type,Guid source,Guid target,string action,object? before,object? after,string? reason)=>db.AssociationAuditEntries.Add(new(){OperationId=op,ChangedAtUtc=timeProvider.GetUtcNow().UtcDateTime,ChangedByUserId=user,AssociationType=type,SourceId=source,TargetId=target,Action=action,Before=before==null?null:JsonSerializer.Serialize(before),After=after==null?null:JsonSerializer.Serialize(after),Reason=reason});
}

public sealed class AssociationValidationException(AssociationPreviewResponse preview)
    : Exception("Association validation failed.")
{
    public AssociationPreviewResponse Preview { get; } = preview;
}
