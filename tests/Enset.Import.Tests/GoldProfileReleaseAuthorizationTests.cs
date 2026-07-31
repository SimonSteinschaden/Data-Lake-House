using Enset.Application.Authorization;
using Enset.Domain.Users;
using Enset.Infrastructure.GoldProfiles;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class GoldProfileReleaseAuthorizationTests
{
    [Theory]
    [InlineData(nameof(GlobalUserRole.EnsetEmployee))]
    public async Task Employee_without_admin_role_cannot_release_gold_profile(string role)
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var employee = new CurrentUserContext();
        employee.Initialize(Guid.NewGuid(), true, [role]);
        await using var db = new EnsetDbContext(options, employee);
        var service = new GoldProfileVersionService(db, null!, null!, employee, TimeProvider.System);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.Release("Building", Guid.NewGuid(), Guid.NewGuid(), 1, "Test", default));
    }

    [Theory]
    [InlineData(nameof(GlobalUserRole.EnsetEmployee))]
    public async Task Employee_without_admin_role_cannot_revoke_gold_profile(string role)
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var employee = new CurrentUserContext();
        employee.Initialize(Guid.NewGuid(), true, [role]);
        await using var db = new EnsetDbContext(options, employee);
        var service = new GoldProfileVersionService(db, null!, null!, employee, TimeProvider.System);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.Revoke("Building", Guid.NewGuid(), Guid.NewGuid(), 1, "Test", default));
    }

    [Fact]
    public async Task Administrator_role_passes_the_release_authorization_gate()
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var admin = new CurrentUserContext();
        admin.Initialize(Guid.NewGuid(), true, [GlobalUserRole.EnsetAdmin.ToString()]);
        await using var db = new EnsetDbContext(options, admin);
        var service = new GoldProfileVersionService(db, null!, null!, admin, TimeProvider.System);

        var exception = await Record.ExceptionAsync(() =>
            service.Release("Building", Guid.NewGuid(), Guid.NewGuid(), 1, "Test", default));

        Assert.NotNull(exception);
        Assert.IsNotType<UnauthorizedAccessException>(exception);
    }
}
