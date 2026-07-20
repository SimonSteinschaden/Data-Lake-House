using Enset.Domain.Buildings;
using Enset.Domain.Common;
using Enset.Domain.Energy;
using Enset.Domain.Geography;
using Enset.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Xunit;

namespace Enset.Import.Tests;

public class GeographyModelTests
{
    private readonly IModel _model = CreateModel();

    [Fact]
    public void Model_contains_complete_geography_domain()
    {
        Assert.NotNull(_model.FindEntityType(typeof(Country)));
        Assert.NotNull(_model.FindEntityType(typeof(State)));
        Assert.NotNull(_model.FindEntityType(typeof(District)));
        Assert.NotNull(_model.FindEntityType(typeof(Municipality)));
        Assert.NotNull(_model.FindEntityType(typeof(PostalCodeArea)));
        Assert.NotNull(_model.FindEntityType(typeof(Region)));
        Assert.NotNull(_model.FindEntityType(typeof(Address)));
    }

    [Fact]
    public void Administrative_hierarchy_has_required_parent_relationships()
    {
        AssertRelationship<State, Country>(nameof(State.CountryId), isRequired: true);
        AssertRelationship<District, State>(nameof(District.StateId), isRequired: true);
        AssertRelationship<Municipality, District>(
            nameof(Municipality.DistrictId),
            isRequired: true);
    }

    [Fact]
    public void Postal_code_areas_and_regions_can_group_municipalities()
    {
        var postalCodeArea = _model.FindEntityType(typeof(PostalCodeArea))!;
        var region = _model.FindEntityType(typeof(Region))!;

        Assert.Contains(
            postalCodeArea.GetSkipNavigations(),
            navigation => navigation.Name == nameof(PostalCodeArea.Municipalities));
        Assert.Contains(
            region.GetSkipNavigations(),
            navigation => navigation.Name == nameof(Region.Municipalities));
    }

    [Fact]
    public void Buildings_and_energy_systems_use_addresses_without_asset_inheritance()
    {
        AssertRelationship<BuildingVersion, Address>(
            nameof(BuildingVersion.AddressId),
            isRequired: false);
        AssertRelationship<EnergySystem, Address>(
            nameof(EnergySystem.AddressId),
            isRequired: false);

        Assert.Equal(typeof(BaseEntity), typeof(Building).BaseType);
        Assert.Equal(typeof(BaseEntity), typeof(EnergySystem).BaseType);
    }

    [Fact]
    public void Address_has_country_and_optional_local_assignments()
    {
        AssertRelationship<Address, Country>(nameof(Address.CountryId), isRequired: true);
        AssertRelationship<Address, Municipality>(
            nameof(Address.MunicipalityId),
            isRequired: false);
        AssertRelationship<Address, PostalCodeArea>(
            nameof(Address.PostalCodeAreaId),
            isRequired: false);
    }

    private void AssertRelationship<TDependent, TPrincipal>(
        string foreignKeyProperty,
        bool isRequired)
    {
        var entityType = _model.FindEntityType(typeof(TDependent));
        Assert.NotNull(entityType);

        var foreignKey = Assert.Single(
            entityType.GetForeignKeys(),
            candidate => candidate.PrincipalEntityType.ClrType == typeof(TPrincipal)
                && candidate.Properties.Any(
                    property => property.Name == foreignKeyProperty));

        Assert.Equal(isRequired, foreignKey.IsRequired);
    }

    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=enset_geography_model;Username=test")
            .Options;

        using var context = new EnsetDbContext(options);
        return context.Model;
    }
}
