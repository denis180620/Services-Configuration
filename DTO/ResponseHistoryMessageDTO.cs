
public class ResponseMessage
{
    public Guid UserId { get; set; }
    public string? RecipientInfo { get; set; } // Email/Phone/Telegram
    public string? Channel { get; set; } // Email, SMS, Telegram
    public string? Content { get; set; }


}

public class MessageStatistics
{
    public int TotalMessages { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount { get; set; }
    public Dictionary<string, int> ByChannel { get; set; } = new();
}