using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity.Entities;

public sealed class ApplicationRole : IdentityRole<Id>
{
    public ApplicationRole() : base()
    {
        Id = Id.New();
    }

    public ApplicationRole(string roleName) : this()
    {
        Name = roleName;
        NormalizedName = roleName.ToUpperInvariant();
    }

    public string? Description { get; set; }
}
