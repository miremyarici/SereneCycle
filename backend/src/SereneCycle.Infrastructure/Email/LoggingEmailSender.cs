using Microsoft.Extensions.Logging;
using SereneCycle.Application.Abstractions;

namespace SereneCycle.Infrastructure.Email;

/// <summary>
/// Geliştirme ortamı için: e-postayı göndermek yerine loga yazar.
/// Gmail OAuth kurulumu yapılmadan da kayıt/doğrulama akışı denenebilsin diye.
/// Üretimde asla kullanılmaz (Program.cs ortama göre seçer).
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    : IEmailSender
{
    public Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "[GELİŞTİRME] E-posta gönderilmedi. Alıcı: {Email}, Konu: {Subject}\n{Body}",
            toEmail, subject, htmlBody);

        return Task.CompletedTask;
    }
}
