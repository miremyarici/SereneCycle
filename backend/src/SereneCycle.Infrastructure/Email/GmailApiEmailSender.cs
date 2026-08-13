using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using SereneCycle.Application.Abstractions;
using SereneCycle.Infrastructure.Options;

namespace SereneCycle.Infrastructure.Email;

/// <summary>
/// Gmail API üzerinden e-posta gönderir.
///
/// Yetkilendirme: kişisel Gmail hesaplarında service account kullanılamadığı
/// için OAuth 2.0 refresh token akışı kullanılır. <see cref="UserCredential"/>
/// access token'ı süresi dolduğunda kendisi yeniler; bizim yapmamız gereken
/// tek şey refresh token'ı güvenli tutmak.
///
/// Scope bilinçli olarak en dar olan: <c>gmail.send</c> — bu uygulama posta
/// kutusunu okuyamaz, yalnızca gönderebilir.
/// </summary>
public sealed class GmailApiEmailSender : IEmailSender, IDisposable
{
    private readonly GmailOptions _options;
    private readonly ILogger<GmailApiEmailSender> _logger;
    private readonly GmailService _gmailService;

    public GmailApiEmailSender(
        IOptions<GmailOptions> options,
        ILogger<GmailApiEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;

        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _options.ClientId,
                    ClientSecret = _options.ClientSecret
                },
                Scopes = [GmailService.Scope.GmailSend]
            });

        // Elimizde sadece refresh token var; access token ilk gönderimde alınır.
        var tokenResponse = new TokenResponse
        {
            RefreshToken = _options.RefreshToken
        };

        var credential = new UserCredential(
            flow, _options.SenderEmail, tokenResponse);

        _gmailService = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Serene Cycle"
        });
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(
            _options.SenderName, _options.SenderEmail));
        mime.To.Add(MailboxAddress.Parse(toEmail));
        mime.Subject = subject;
        mime.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

        using var stream = new MemoryStream();
        await mime.WriteToAsync(stream, cancellationToken);

        var message = new Message { Raw = ToBase64Url(stream.ToArray()) };

        try
        {
            await _gmailService.Users.Messages
                .Send(message, "me")
                .ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Alıcı adresini loglamıyoruz — kişisel veri.
            _logger.LogError(ex, "Gmail API ile e-posta gönderimi başarısız.");
            throw;
        }
    }

    /// <summary>
    /// Gmail API, RFC 2822 mesajını base64url (dolgusuz) bekler.
    /// </summary>
    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    public void Dispose() => _gmailService.Dispose();
}
