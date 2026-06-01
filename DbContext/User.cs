using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Confuguration.Dbcontext;

public class User : IdentityUser<Guid>
{
    public Guid UserId {get; set;}
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual ICollection<UserTamplate>? Tamplates { get; set; }
    public virtual ICollection<SentMessage>? SentMessages { get; set; }
    public virtual ICollection<Contact>? Contacts { get; set; }
    public virtual ICollection<UserSession>? UserSessions { get; set; }
}

public class Role : IdentityRole<Guid>
{
    public string? Description { get; set; } = string.Empty;
}