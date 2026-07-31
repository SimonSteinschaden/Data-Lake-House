using Enset.Application.CanonicalSnapshots;
using Enset.Domain.Curation;
using Enset.Domain.Energy;
using Xunit;

namespace Enset.Import.Tests;

public sealed class EnergySystemGoldAssessmentTests
{
    [Fact]
    public void Missing_energy_system_number_keeps_bronze_and_is_never_confirmable()
    {
        var result = EnergySystemGoldDefinition.Evaluate(
            null, EnergySystemType.Photovoltaic, 10m, true,
            new Dictionary<string, bool> { ["EnergySystemNumber"] = true });

        Assert.Equal(DataMaturityLevel.Bronze, result.MaturityLevel);
        Assert.False(result.TechnicallyComplete);
        var numberField = result.GoldFieldStates.Single(f => f.FieldName == "EnergySystemNumber");
        Assert.False(numberField.IsConfirmable);
        Assert.Equal(EnergySystemGoldFieldState.Missing, numberField.State);
    }

    [Fact]
    public void Complete_but_unconfirmed_fields_yield_silver()
    {
        var result = EnergySystemGoldDefinition.Evaluate(
            "AN-1", EnergySystemType.Photovoltaic, 10m, true, confirmations: null);

        Assert.Equal(DataMaturityLevel.Silver, result.MaturityLevel);
        Assert.False(result.IsGoldReady);
        Assert.Contains(result.GoldFieldStates,
            f => f.FieldName == "Type" && f.State == EnergySystemGoldFieldState.PresentUnconfirmed);
    }

    [Fact]
    public void All_confirmable_fields_confirmed_yields_gold()
    {
        var confirmations = new Dictionary<string, bool> { ["Type"] = true, ["RatedPowerKw"] = true };
        var result = EnergySystemGoldDefinition.Evaluate(
            "AN-1", EnergySystemType.Photovoltaic, 10m, true, confirmations);

        Assert.Equal(DataMaturityLevel.Gold, result.MaturityLevel);
        Assert.True(result.IsGoldReady);
    }

    [Fact]
    public void Building_assignment_missing_blocks_gold_even_if_type_and_power_confirmed()
    {
        var confirmations = new Dictionary<string, bool> { ["Type"] = true, ["RatedPowerKw"] = true };
        var result = EnergySystemGoldDefinition.Evaluate(
            "AN-1", EnergySystemType.Photovoltaic, 10m, buildingAssigned: false, confirmations);

        Assert.Equal(DataMaturityLevel.Bronze, result.MaturityLevel);
        Assert.False(result.TechnicallyComplete);
        var assignmentField = result.GoldFieldStates.Single(f => f.FieldName == "BuildingAssignment");
        Assert.False(assignmentField.IsConfirmable);
        Assert.Equal(EnergySystemGoldFieldState.Missing, assignmentField.State);
    }

    [Theory]
    [InlineData(EnergySystemType.DistrictHeating)]
    [InlineData(EnergySystemType.Ventilation)]
    [InlineData(EnergySystemType.Other)]
    public void Rated_power_is_not_required_for_types_without_a_meaningful_power_rating(EnergySystemType type)
    {
        var confirmations = new Dictionary<string, bool> { ["Type"] = true };
        var result = EnergySystemGoldDefinition.Evaluate(
            "AN-1", type, ratedPowerKw: null, buildingAssigned: true, confirmations);

        Assert.Equal(DataMaturityLevel.Gold, result.MaturityLevel);
        Assert.DoesNotContain(result.GoldFieldStates, f => f.FieldName == "RatedPowerKw");
    }

    [Theory]
    [InlineData(EnergySystemType.Photovoltaic)]
    [InlineData(EnergySystemType.HeatPump)]
    [InlineData(EnergySystemType.Boiler)]
    [InlineData(EnergySystemType.BatteryStorage)]
    [InlineData(EnergySystemType.ChargingInfrastructure)]
    [InlineData(EnergySystemType.Cooling)]
    public void Rated_power_is_required_for_types_with_a_meaningful_power_rating(EnergySystemType type)
    {
        Assert.True(EnergySystemGoldDefinition.RequiresRatedPower(type));
    }

    [Fact]
    public void Warnings_only_cover_confirmable_present_unconfirmed_fields()
    {
        var result = EnergySystemGoldDefinition.Evaluate(
            "AN-1", EnergySystemType.Photovoltaic, 10m, true, confirmations: null);

        Assert.All(result.ConfirmationReasons, reason =>
            Assert.DoesNotContain("Anlagennummer", reason));
        Assert.All(result.ConfirmationReasons, reason =>
            Assert.DoesNotContain("Gebäudezuordnung", reason));
    }
}
