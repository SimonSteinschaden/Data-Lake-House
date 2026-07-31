using Xunit;

namespace Enset.Import.Tests;

public sealed class UnifiedCrudArchitectureTests
{
    [Fact]
    public void CrudReadService_UsesCanonicalSnapshotsWithoutParallelRules()
    {
        var source = File.ReadAllText(FindSource(
            "src", "Enset.Infrastructure", "ReadModel",
            "EfEntityReadService.cs"));

        Assert.Contains("ICanonicalSnapshotReader snapshots", source);
        Assert.DoesNotContain("CuratedFieldValues", source);
        Assert.DoesNotContain("CanonicalAnnualValue.Calculate", source);
        Assert.DoesNotContain("TimeSpan.FromDays(364", source);
    }

    [Fact]
    public void FrontendDisplays_DoNotCalculateAnnualValuesOrQuality()
    {
        var source = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "components", "domain",
            "CanonicalDisplays.tsx"));

        Assert.DoesNotContain("reduce(", source);
        Assert.DoesNotContain("goldMaturityPercentage", source);
        Assert.Contains("status === \"IncompleteYear\"", source);
        Assert.Contains("formatUiValue(level)", source);
    }

    [Fact]
    public void LocalizedEnumSelects_KeepTechnicalApiValues()
    {
        var options = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "components", "ui",
            "enumOptions.ts"));
        var buildingForm = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "features", "buildings",
            "BuildingForm.tsx"));
        var energySystemForm = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "features", "energySystems",
            "EnergySystemForm.tsx"));
        var metersPage = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "pages", "MetersPage.tsx"));

        Assert.Contains("""{ value: "Residential", label: "Wohnen" }""", options);
        Assert.Contains("""{ value: "Photovoltaic", label: "Photovoltaik" }""", options);
        Assert.Contains("""{ value: "Measured", label: "Gemessen" }""", options);
        Assert.Contains("value={option.value}", buildingForm);
        Assert.Contains("value={option.value}", energySystemForm);
        Assert.DoesNotContain("formatUiValue", buildingForm);
        Assert.DoesNotContain("formatUiValue", energySystemForm);
        Assert.Contains("readingType: \"Instantaneous\", qualityFlag: \"Measured\"",
            metersPage);
        Assert.DoesNotContain("qualityFlag: \"Valid\"", metersPage);
    }

    [Fact]
    public void ApiClient_ReadsRfc7807ProblemJsonAndDisplaysValidationDetails()
    {
        var apiClient = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "api", "apiClient.ts"));
        var crudUi = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "features", "crud", "crudUi.tsx"));

        Assert.Contains("application/problem+json", apiClient);
        Assert.Contains("error.problem?.errors", crudUi);
        Assert.Contains("PostalCode: \"PLZ\"", crudUi);
    }

    [Fact]
    public void BuildingDetail_UsesCanonicalGoldAssessmentWithoutProgressBar()
    {
        var page = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "pages",
            "BuildingsPage.tsx"));
        var readiness = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "features", "curation",
            "CurationReadinessPanel.tsx"));
        var styles = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "index.css"));

        Assert.Contains(
            "summary.goldAssessment.goldCompletenessPercentage",
            page);
        Assert.Contains(
            "buildingAssessment={summary.goldAssessment}",
            page);
        Assert.DoesNotContain("<progress", readiness);
        Assert.Contains("goldPresentFieldCount", readiness);
        Assert.Contains("goldConfirmedFieldCount", readiness);
        Assert.Contains("In der Datenprüfung bestätigen", readiness);
        Assert.Contains("entityId=${id}&fieldName=${item.fieldName}", readiness);
        Assert.Contains(
            "Alle Gold-relevanten Stammdaten sind vollständig.",
            readiness);
        Assert.Contains("--quality-bronze", styles);
        Assert.Contains("--quality-silver", styles);
        Assert.Contains("--quality-gold", styles);
    }

    [Fact]
    public void BuildingForm_DoesNotWriteSystemIdentityOrBuildingNumber()
    {
        var form = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "features", "buildings",
            "BuildingForm.tsx"));
        var types = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "features", "buildings",
            "types.ts"));
        var contracts = File.ReadAllText(FindSource(
            "src", "Enset.Application", "Crud",
            "CrudContracts.cs"));

        Assert.DoesNotContain("text(\"buildingNumber\"", form);
        Assert.DoesNotContain("GebäudeID", form);
        Assert.Contains("Externe Gebäude-ID (optional)", form);
        Assert.DoesNotContain("buildingNumber:", types[
            types.IndexOf("export interface BuildingFormModel",
                StringComparison.Ordinal)..]);
        Assert.Contains(
            "record BuildingCreateRequest(string Name",
            contracts);
        Assert.Contains(
            "record BuildingUpdateRequest(string Name",
            contracts);
    }

    private static string FindSource(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                [directory.FullName, .. segments]);
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException(Path.Combine(segments));
    }
}
