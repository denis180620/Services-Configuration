using Confuguration.ServicesSending;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using DTOResponseSending;
using Twilio.Http;

namespace Confuguration.ServicesSending;

    public class EmailSender : IMessageSender
    {
        public string Channel => "Email";
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Result<ResponseSender>> SendAsync(string RecipientInfo, string content)
        {
            try
            {
            Console.WriteLine($"RecipientInfo = '{RecipientInfo}'");
            var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Congratulation Service",
                    _configuration["Email:From"]));
                email.To.Add(new MailboxAddress("", RecipientInfo));
                email.Subject = "Поздравление!";

                email.Body = new TextPart("html")
                {
                    Text = content
                };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _configuration["Email:SmtpServer"],
                    int.Parse(_configuration["Email:Port"]),
                    MailKit.Security.SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return Result<ResponseSender>.Success(new ResponseSender {Success = true});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки сообщения");
                return Result<ResponseSender>.Failure(ex.Message);
            }
        }
    }