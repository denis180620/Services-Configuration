using System.Dynamic;
using Confuguration.Dbcontext;
using DTOResponseSending;

namespace Confuguration.ServicesSending;

public interface IMessageSender
{
    string Channel {get;}
    Task<Result<ResponseSender>> SendAsync(string RecipientInfo, string content);

}