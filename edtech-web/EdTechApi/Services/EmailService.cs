using SendGrid;
using SendGrid.Helpers.Mail;

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
    private readonly string? _apiKey;
    private readonly string? _fromEmail;
    private readonly string? _fromName;
    private readonly ILogger<EmailService> _logger;
    private readonly ICircuitBreakerService _circuitBreaker;

    public EmailService(IConfiguration config, ILogger<EmailService> logger, ICircuitBreakerService circuitBreaker)
    {
        _apiKey = config["SendGrid:ApiKey"];
        _fromEmail = config["SendGrid:FromEmail"] ?? config["Email:SmtpFrom"] ?? "noreply@edtech.app";
        _fromName = config["SendGrid:FromName"] ?? "EdTech Examination App";
        _logger = logger;
        _circuitBreaker = circuitBreaker;
    }

    public async Task<EmailSendResult> SendEmailAsync(string to, string subject, string html)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogInformation("[Email] SendGrid not configured. Would send to {To}: {Subject}", to, subject);
            return new EmailSendResult { Status = "skipped", Message = "SendGrid not configured" };
        }

        try
        {
            return await _circuitBreaker.ExecuteAsync("sendgrid-api", async () =>
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var recipient = new EmailAddress(to);
                var msg = MailHelper.CreateSingleEmail(from, recipient, subject, null, html);
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogInformation("[Email] Sent via SendGrid to {To}, status: {StatusCode}", to, response.StatusCode);
                    return new EmailSendResult { Status = "sent", Via = "sendgrid", Id = body };
                }

                var errBody = await response.Body.ReadAsStringAsync();
                _logger.LogWarning("[Email] SendGrid failed ({StatusCode}): {Error}", response.StatusCode, errBody);
                return new EmailSendResult { Status = "failed", Message = $"SendGrid returned {response.StatusCode}" };
            });
        }
        catch (CircuitBreakerOpenException ex)
        {
            _logger.LogWarning("[Email] Circuit breaker open: {Message}", ex.Message);
            return new EmailSendResult { Status = "failed", Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[Email] SendGrid error: {Error}", ex.Message);
            return new EmailSendResult { Status = "failed", Message = ex.Message };
        }
    }
}
