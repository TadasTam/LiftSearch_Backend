namespace LiftSearch.Auth;

public static class UserRoles
{
    public const string Admin = nameof(Admin);
    public const string Traveler = nameof(Traveler);
    public const string Driver = nameof(Driver);

    public static readonly IReadOnlyCollection<string> All = new[] { Admin, Traveler, Driver };
}