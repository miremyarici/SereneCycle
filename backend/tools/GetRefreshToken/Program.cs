using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;

// Gmail gönderimi için refresh token alır: tarayıcıda bir kez onay verilir,
// token terminale yazılır. OAuth Playground'a gerek kalmaz.
//
// Kullanım:
//   dotnet run --project backend/tools/GetRefreshToken
//
// Google Cloud'da istemci tipi "Desktop app" olmalı: bu araç geri dönüş için
// http://localhost üzerinde geçici bir dinleyici açıyor, Web istemcilerinde
// bu adresin önceden kayıtlı olması gerekirdi (port her seferinde değişiyor).

const string scope = "https://www.googleapis.com/auth/gmail.send";

var clientId = Ask("Client ID", args.ElementAtOrDefault(0));
var clientSecret = Ask("Client Secret", args.ElementAtOrDefault(1));

if (clientId.Length == 0 || clientSecret.Length == 0)
{
    Console.Error.WriteLine("Client ID ve Client Secret zorunlu.");
    return 1;
}

var flow = new GoogleAuthorizationCodeFlow(
    new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecrets = new ClientSecrets
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        },
        Scopes = [scope],
        // Hesap daha önce izin verdiyse Google refresh token'ı tekrar
        // göndermiyor; "consent" ile her çalıştırmada yenisi geliyor.
        Prompt = "consent"
    });

// DataStore verilmedi: token diske yazılmasın, sadece ekrana bassın.
Console.WriteLine("Tarayıcı açılıyor — gönderici Gmail hesabınla izin ver.");
Console.WriteLine("\"Google hasn't verified this app\" çıkarsa:"
    + " Advanced → Go to ... (unsafe).\n");

UserCredential credential;

try
{
    credential = await new AuthorizationCodeInstalledApp(
            flow, new LocalServerCodeReceiver())
        .AuthorizeAsync("serene-cycle-sender", CancellationToken.None);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Yetkilendirme başarısız: {ex.Message}");
    return 1;
}

var refreshToken = credential.Token.RefreshToken;

if (string.IsNullOrEmpty(refreshToken))
{
    Console.Error.WriteLine(
        "Google refresh token döndürmedi. İstemci tipi 'Desktop app' mi,"
        + " kontrol et.");
    return 1;
}

Console.WriteLine("\nRefresh token alındı. Kalan adım — şunları çalıştır:\n");
Console.WriteLine("cd backend/src/SereneCycle.Api");
Console.WriteLine("dotnet user-secrets set \"Gmail:UseRealSender\" \"true\"");
Console.WriteLine($"dotnet user-secrets set \"Gmail:ClientId\" \"{clientId}\"");
Console.WriteLine(
    $"dotnet user-secrets set \"Gmail:ClientSecret\" \"{clientSecret}\"");
Console.WriteLine(
    $"dotnet user-secrets set \"Gmail:RefreshToken\" \"{refreshToken}\"");
Console.WriteLine(
    "dotnet user-secrets set \"Gmail:SenderEmail\" \"<gmail adresin>\"");

return 0;

static string Ask(string label, string? provided)
{
    if (!string.IsNullOrWhiteSpace(provided))
    {
        return provided.Trim();
    }

    Console.Write($"{label}: ");
    return (Console.ReadLine() ?? string.Empty).Trim();
}
