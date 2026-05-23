namespace Confuguration.Dbcontext;

public class UserSession
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string RefreshToken { get; set; }  
    public string JwtToken { get; set; }      
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int ExpiresIn { get; set; }

    public virtual User? User { get; set; }
}