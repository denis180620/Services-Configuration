using System.Dynamic;
using Confuguration.Dbcontext;

namespace Confuguration.ServicesSending;

public interface IMessageSender
{
    string Channel {get;}
    Task<bool> SendAsync(string recipient, string content);

}