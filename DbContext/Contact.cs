using Confuguration.Dbcontext;

public class Contact
{
    public int Id {get; set;}
    public Guid UserId {get; set;}
    public string Name {get; set;}
    public int Phone {get; set;}
    public string NikNameTelegram {get; set;}
    public string IdVk {get; set;}
    public string Email {get; set;}
    public virtual User? User {get; set;}
}