namespace SereneCycle.Application.Abstractions;

public interface ITokenService
{
    /// <summary>Kısa ömürlü erişim token'ı üretir.</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(
        Guid userId,
        string email);

    /// <summary>
    /// Kriptografik olarak güvenli, rastgele refresh token üretir.
    /// Veritabanında hash'lenmiş olarak saklanır.
    /// </summary>
    string CreateRefreshToken();
}
