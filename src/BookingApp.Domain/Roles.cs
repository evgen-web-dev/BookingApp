using System.Collections.Frozen;

namespace BookingApp.Domain;

public static class Roles
{
    public const string Host = "Host";
    public const string Client = "Client";
    
    public static readonly IReadOnlySet<string> RolesAvailableForPublicRegistration = new HashSet<string>() { Client, Host }.ToFrozenSet();
    
    public static readonly IReadOnlySet<string> AllRoles = new HashSet<string>() { Client, Host }.ToFrozenSet(); // extend when Admin and other roles are added 
}