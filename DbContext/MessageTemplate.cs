namespace Confuguration.Dbcontext;

public class MessageTemplate
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Content { get; set; }
    public string? Category { get; set; } // Birthday, NewYear, etc.
    public bool IsSystem { get; set; } // System templates for all users
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}