# backend — Serene Cycle API

ASP.NET Core 9 Web API. Katmanlı yapı:

```
src/
├── SereneCycle.Domain/          Entity'ler + faz hesaplama (saf, bağımlılıksız)
├── SereneCycle.Application/     Arayüzler, DTO'lar, Result tipi
├── SereneCycle.Infrastructure/  EF Core, Identity, JWT, Gmail API
└── SereneCycle.Api/             Controller'lar, DI, Swagger
tests/
└── SereneCycle.Tests/           Domain birim testleri
```

Bağımlılık yönü tek yönlü: `Api → Infrastructure → Application → Domain`.
Domain hiçbir framework'e bağımlı değil, bu yüzden faz hesaplama mantığı
widget/veritabanı olmadan test edilebiliyor.

## Uç noktalar

| Method | Yol | Açıklama |
|---|---|---|
| POST | `/auth/register` | Hesap oluşturur, doğrulama kodu gönderir |
| POST | `/auth/verify-code` | Kodu doğrular, token döner |
| POST | `/auth/resend-code` | Kodu yeniden gönderir |
| POST | `/auth/login` | Giriş, token döner |
| POST | `/auth/forgot-password` | Sıfırlama kodu gönderir |
| POST | `/auth/reset-password` | Kodla şifre sıfırlar |
| POST | `/auth/refresh` | Token yeniler (rotation) |
| GET | `/me` | Profil + döngü ayarları |
| PUT | `/me` | Profil/ayar günceller, onboarding'i tamamlar |
| GET | `/phase/today` | Ana sayfa: faz, ilerleme, takvim şeridi |

Swagger UI: geliştirme ortamında `/swagger`.

## Çalıştırma

### Docker Compose (önerilen)

```bash
cd infra
cp .env.example .env
# .env içindeki POSTGRES_PASSWORD ve JWT_SIGNING_KEY'i doldur
docker compose up --build
```

API `http://localhost:8080` üzerinde açılır.

### Yerel (dotnet CLI)

PostgreSQL'in çalışıyor olması gerekir. Gizli değerler appsettings'e
yazılmaz, user-secrets ile verilir:

```bash
cd backend/src/SereneCycle.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<en az 32 karakterlik rastgele değer>"
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=serenecycle;Username=postgres;Password=<sifre>"

dotnet run
```

Migration'ları uygulamak için:

```bash
dotnet ef database update --project ../SereneCycle.Infrastructure --startup-project .
```

## Geliştirme verisi

Geliştirme ortamında uygulama açılırken migration'lar uygulanır ve örnek
veri yazılır (`DevelopmentDataSeeder`). Idempotenttir, tekrar tekrar
çalıştırılabilir. Kapatmak için: `"SeedDevelopmentData": false`.

Hazır gelen test hesabı — e-posta doğrulaması atlanmış durumda, doğrudan
giriş yapabilirsin:

```
E-posta: test@serenecycle.app
Şifre  : Test1234!
```

Bu hesapla birlikte gelenler:

- 3 döngü kaydı; sonuncusu **bugünü döngünün 7. gününe** denk getirir
  (foliküler faz — tasarım mockup'ındaki "Day 7 of 28" durumu)
- 2 günlük kayıt (biri semptomlu, biri duygulu)
- 4 faz × beslenme/hareket için 29 içerik maddesi

Hızlı deneme:

```bash
curl -X POST http://localhost:5080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@serenecycle.app","password":"Test1234!"}'
```

Dönen `accessToken` ile korumalı uçlar çağrılır:

```bash
curl http://localhost:5080/phase/today -H "Authorization: Bearer <token>"
```

## E-posta: Gmail API kurulumu

Geliştirmede varsayılan olarak **e-posta gönderilmez**, doğrulama kodu
loga yazılır (`LoggingEmailSender`). Yani OAuth kurulumu yapmadan da
kayıt/doğrulama akışını test edebilirsin — konsolu izle.

Gerçek gönderim için:

### 1. Google Cloud projesi

1. [Google Cloud Console](https://console.cloud.google.com/) → yeni proje
2. **APIs & Services → Library** → "Gmail API" → **Enable**

### 2. OAuth consent screen

1. **APIs & Services → OAuth consent screen** → User Type: **External**
2. Uygulama adı, destek e-postası doldur
3. **Scopes** → Add → `https://www.googleapis.com/auth/gmail.send` ekle
   (en dar scope; uygulama posta kutusunu okuyamaz, yalnızca gönderir)
4. **Test users** → kendi Gmail adresini ekle

> **Önemli:** Consent screen "Testing" modundayken alınan refresh token
> **7 gün sonra geçersiz olur**. Kalıcı bir token için uygulamayı
> **Publish** et (doğrulama gerekmez, "Testing" → "In production").

### 3. OAuth istemcisi

1. **Credentials → Create Credentials → OAuth client ID**
2. Application type: **Web application**
3. **Authorized redirect URIs** → şunu ekle:
   `https://developers.google.com/oauthplayground`
4. Client ID ve Client Secret'ı kaydet

### 4. Refresh token alma

1. [OAuth 2.0 Playground](https://developers.google.com/oauthplayground/)
2. Sağ üstteki ⚙️ → **Use your own OAuth credentials** işaretle →
   Client ID ve Secret'ı gir
3. Sol panelde scope kutusuna `https://www.googleapis.com/auth/gmail.send`
   yaz → **Authorize APIs** → Google hesabınla izin ver
4. **Exchange authorization code for tokens** → çıkan **Refresh token**'ı kopyala

### 5. Yapılandırma

```bash
cd backend/src/SereneCycle.Api
dotnet user-secrets set "Gmail:UseRealSender" "true"
dotnet user-secrets set "Gmail:ClientId" "<client id>"
dotnet user-secrets set "Gmail:ClientSecret" "<client secret>"
dotnet user-secrets set "Gmail:RefreshToken" "<refresh token>"
dotnet user-secrets set "Gmail:SenderEmail" "<gmail adresin>"
```

Docker için aynı değerler `infra/.env` içine yazılır.

> Gmail'in ücretsiz hesaplarda günlük gönderim limiti ~500 e-postadır;
> portföy projesi için fazlasıyla yeterli. Başka bir sağlayıcıya geçmek
> gerekirse yalnızca `IEmailSender` implementasyonu değişir.

## Testler

```bash
cd backend
dotnet test
```

Faz hesaplama mantığının bütün sınır durumları (menstrüel/foliküler/
ovulasyon/luteal geçişleri, düzensiz döngü uyarısı, geciken adet,
kayan ortalama) burada korunuyor.

## Güvenlik notları

- Doğrulama kodları veritabanında **hash'lenmiş** tutulur, düz metin değil.
- Refresh token'lar da hash'lenerek saklanır ve kullanıldığında
  döndürülür (rotation) — çalınan token yeniden kullanılamaz.
- `/auth/forgot-password` ve `/auth/resend-code`, e-posta kayıtlı olmasa da
  başarı döner: hangi adreslerin sistemde olduğu dışarı sızmaz.
- Kod deneme sayısı sınırlıdır (5), Identity kilitleme açıktır.
- `Jwt:SigningKey` asla `appsettings.json`'a yazılmaz.

Menstrüel veri KVKK kapsamında **özel nitelikli kişisel veridir**.
Uygulama yayınlanacaksa veri saklama/silme politikası ve gizlilik
metni bu doğrultuda gözden geçirilmelidir.
