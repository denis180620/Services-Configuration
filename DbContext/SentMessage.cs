
namespace Confuguration.Dbcontext;

public class SentMessage
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? RecipientInfo { get; set; } // Email/Phone/Telegram
    public string? Channel { get; set; } // Email, SMS, Telegram
    public string? Content { get; set; }
    public string? Status { get; set; } // Sent, Failed, Delivered
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set;} = DateTime.UtcNow;

    public virtual User? User { get; set; }
}
