using Enset.Domain.Customers;
using Enset.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Enset.Infrastructure.Persistence;

public static class DevelopmentIdentitySeeder
{
    private static readonly Guid CustomerId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    public static async Task SeedDevelopmentIdentityAsync(this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EnsetDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var customer = await db.Customers.SingleOrDefaultAsync(
            x => x.Id == CustomerId || x.CustomerNumber == "DEV-CUSTOMER",
            cancellationToken);
        if (customer is null)
        {
            customer = new Customer
            {
                Id = CustomerId,
                CustomerNumber = "DEV-CUSTOMER",
                Name = "ENSET Development Customer",
                Type = CustomerType.Company,
                IsActive = true
            };
            db.Customers.Add(customer);
        }

        await EnsureUser(db, "development-user", "Development Employee",
            "development-user@enset.local", GlobalUserRole.EnsetEmployee, null,
            customer.Id, cancellationToken);
        await EnsureUser(db, "development-customer-admin", "Development Customer Admin",
            "development-customer-admin@enset.local", null, UserCustomerRole.CustomerAdmin,
            customer.Id, cancellationToken);
        await EnsureUser(db, "development-customer-user", "Development Customer User",
            "development-customer-user@enset.local", null, UserCustomerRole.CustomerUser,
            customer.Id, cancellationToken);
        await EnsureUser(db, "development-customer-viewer", "Development Customer Viewer",
            "development-customer-viewer@enset.local", null, UserCustomerRole.CustomerViewer,
            customer.Id, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureUser(EnsetDbContext db, string externalIdentity,
        string displayName, string email, GlobalUserRole? globalRole,
        UserCustomerRole? customerRole, Guid customerId,
        CancellationToken cancellationToken)
    {
        var user = await db.ApplicationUsers
            .Include(x => x.CustomerAssignments)
            .SingleOrDefaultAsync(x => x.ExternalIdentity == externalIdentity, cancellationToken);
        if (user is null)
        {
            user = new ApplicationUser
            {
                ExternalIdentity = externalIdentity,
                DisplayName = displayName,
                Email = email,
                GlobalRole = globalRole,
                IsActive = true
            };
            db.ApplicationUsers.Add(user);
        }

        if (customerRole.HasValue && !user.CustomerAssignments.Any(x =>
                x.CustomerId == customerId && x.IsActive))
        {
            user.CustomerAssignments.Add(new UserCustomerAssignment
            {
                CustomerId = customerId,
                Role = customerRole.Value,
                ValidFrom = DateTimeOffset.UnixEpoch,
                IsActive = true
            });
        }
    }
}
