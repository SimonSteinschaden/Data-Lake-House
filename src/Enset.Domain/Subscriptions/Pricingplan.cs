//TODO:
//├── Subscriptions
//│   ├── PricingPlan
//│   ├── PricingPlanEntitlement
//│   ├── CustomerSubscription
//│   └── SubscriptionStatus

/*public class PricingPlan : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PricingPlanEntitlement> Entitlements { get; set; }
        = new List<PricingPlanEntitlement>();
}