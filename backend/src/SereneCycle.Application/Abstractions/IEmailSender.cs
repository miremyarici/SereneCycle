namespace SereneCycle.Application.Abstractions;

/// <summary>
/// E-posta gönderimi bir uygulama detayıdır. Bu arayüzün arkasında şu an
/// Gmail API var; Brevo/SendGrid'e geçiş tek bir Infrastructure sınıfını
/// değiştirmekle sınırlı kalsın diye soyutlandı.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
