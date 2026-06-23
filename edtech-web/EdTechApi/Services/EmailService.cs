using System.Net;
using System.Net.Mail;

namespace EdTechApi.Services;

public interface IEmailService
{
    Task<EmailSendResult> SendEmailAsync(string to, string subject, string html);
}

public class EmailSendResult
{
    public string Status { get; set; } = "skipped";
    public string? Message { get; set; }
    public string? Via { get; set; }
    public string? Id { get; set; }
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private SmtpClient? _smtpClient;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
        InitializeSmtp();
    }

    private void InitializeSmtp()
    {
        var host = _config["Email:SmtpHost"];
        var user = _config["Email:SmtpUser"];
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user)) return;

        var port = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
        var pass = _config["Email:SmtpPass"] ?? "";

        _smtpClient = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = port == 465
        };
    }

    public async Task<EmailSendResult> SendEmailAsync(string to, string subject, string html)
    {
        if (_smtpClient != null)
        {
            try
            {
                var from = _config["Email:SmtpFrom"] ?? _config["Email:SmtpUser"];
                var msg = new MailMessage(from!, to, subject, html) { IsBodyHtml = true };
                await _smtpClient.SendMailAsync(msg);
                _logger.LogInformation("[Email] Sent via SMTP to {To}", to);
                return new EmailSendResult { Status = "sent", Via = "smtp" };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Email] SMTP failed: {Error}", ex.Message);
            }
        }

        _logger.LogInformation("[Email] No email provider configured. Would send to {To}: {Subject}", to, subject);
        return new EmailSendResult { Status = "skipped", Message = "No email provider configured" };
    }
}
