using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Confuguration.Dbcontext;

public class User : IdentityUser<Guid>
{
    [Key]
    public int Id {get; set;}
    public Guid UserId {get; set;}
    public string? Username {get; set;}
    public override string?  Email {get; set;}
    public string Role {get; set;} = "User";
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public string Password {get; set;}

    public virtual ICollection<UserTamplate>? Tamplates {get; set; }
    public virtual ICollection<SentMessage> SentMessages { get; set; }
    public virtual ICollection<Contact> Contacts {get; set;}
    public virtual ICollection<UserSession> UserSessions {get; set;}
}
public class Role : IdentityRole<int>
{
    public string Description { get; set; } = string.Empty;
}
public class UserRefreshToken
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreateAt { get; set; }
    public bool IsRevoked { get; set; }
    [ForeignKey(nameof(UserId))]
    public User user { get; set; }
}