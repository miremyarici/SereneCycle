namespace SereneCycle.Infrastructure.Services;

/// <summary>
/// E-posta gövdeleri. Uygulamanın sıcak kahverengi paletiyle uyumlu,
/// e-posta istemcilerinde güvenli çalışması için inline stil kullanır.
/// </summary>
public static class EmailTemplates
{
    public static string VerificationCode(string name, string code) =>
        Wrap($"""
            <p style="margin:0 0 16px;">Merhaba {Escape(name)},</p>
            <p style="margin:0 0 24px;">
              Serene Cycle hesabını doğrulamak için aşağıdaki kodu kullan:
            </p>
            {CodeBlock(code)}
            <p style="margin:24px 0 0;font-size:14px;color:#50443e;">
              Kod 15 dakika geçerlidir. Bu isteği sen yapmadıysan
              bu e-postayı yok sayabilirsin.
            </p>
            """);

    public static string PasswordReset(string name, string code) =>
        Wrap($"""
            <p style="margin:0 0 16px;">Merhaba {Escape(name)},</p>
            <p style="margin:0 0 24px;">
              Şifreni sıfırlamak için aşağıdaki kodu kullan:
            </p>
            {CodeBlock(code)}
            <p style="margin:24px 0 0;font-size:14px;color:#50443e;">
              Kod 15 dakika geçerlidir. Şifreni sıfırlamak istemediysen
              bu e-postayı yok sayabilirsin; hesabın güvende.
            </p>
            """);

    private static string CodeBlock(string code) =>
        $"""
        <div style="background:#fff1eb;border-radius:16px;padding:20px;
                    text-align:center;">
          <span style="font-size:32px;letter-spacing:8px;font-weight:600;
                       color:#63432e;">{Escape(code)}</span>
        </div>
        """;

    private static string Wrap(string content) =>
        $"""
        <div style="font-family:'Helvetica Neue',Arial,sans-serif;
                    background:#fff8f6;padding:32px;color:#29170e;">
          <div style="max-width:480px;margin:0 auto;background:#ffffff;
                      border-radius:16px;padding:32px;">
            <h1 style="margin:0 0 24px;font-size:24px;color:#63432e;">
              Serene Cycle
            </h1>
            {content}
          </div>
        </div>
        """;

    /// <summary>
    /// Kullanıcıdan gelen isim doğrudan HTML'e gömüldüğü için kaçışlanır.
    /// </summary>
    private static string Escape(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
