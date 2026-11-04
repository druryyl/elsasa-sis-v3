namespace Elsasa.Sis.Finance;

/// <summary>
/// Authoritative permission constants for the Finance domain (GAP-008, TD-07, ADR-009).
/// </summary>
public static class Permissions
{
    public static class Policies
    {
        public const string View = "Permissions.Finance.Policies.View";
        public const string Manage = "Permissions.Finance.Policies.Manage";
        public const string Approve = "Permissions.Finance.Policies.Approve";
    }

    public static class Obligations
    {
        public const string View = "Permissions.Finance.Obligations.View";
        public const string Assess = "Permissions.Finance.Obligations.Assess";
    }

    public static class Billing
    {
        public const string View = "Permissions.Finance.Billing.View";
        public const string Generate = "Permissions.Finance.Billing.Generate";
    }

    public static class Accounts
    {
        public const string View = "Permissions.Finance.Accounts.View";
    }

    /// <summary>
    /// Explicit domain prefix hierarchy matching cross-domain permission conventions.
    /// </summary>
    public static class Finance
    {
        public static class Policies
        {
            public const string View = Permissions.Policies.View;
            public const string Manage = Permissions.Policies.Manage;
            public const string Approve = Permissions.Policies.Approve;
        }

        public static class Obligations
        {
            public const string View = Permissions.Obligations.View;
            public const string Assess = Permissions.Obligations.Assess;
        }

        public static class Billing
        {
            public const string View = Permissions.Billing.View;
            public const string Generate = Permissions.Billing.Generate;
        }

        public static class Accounts
        {
            public const string View = Permissions.Accounts.View;
        }
    }
}
