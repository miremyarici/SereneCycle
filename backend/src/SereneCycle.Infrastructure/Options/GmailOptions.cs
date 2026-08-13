using System.ComponentModel.DataAnnotations;

namespace SereneCycle.Infrastructure.Options;

/// <summary>
/// Gmail API (OAuth 2.0) ayarları.
///
/// Kişisel bir @gmail.com hesabında service account kullanılamaz (o, Google
/// Workspace'in domain-wide delegation özelliğini gerektirir). Bu yüzden
/// akış şöyle: bir kez OAuth onayı verilir, alınan refresh token saklanır,
/// sunucu her mail göndermeden önce onunla yeni access token alır.
///
/// Kurulum adımları için: backend/README.md
/// </summary>
public class GmailOptions
{
    public const string SectionName = "Gmail";

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Tek seferlik OAuth onayından alınan uzun ömürlü refresh token.</summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Gönderen adres — OAuth onayını veren hesapla aynı olmalı.</summary>
    [Required, EmailAddress]
    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = "Serene Cycle";
}
