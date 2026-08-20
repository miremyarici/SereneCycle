namespace SereneCycle.Application.Common;

/// <summary>
/// Birden fazla serviste birebir tekrar eden hata metinleri. Tek yerde
/// durmaları önemli: aynı durumun iki uçta farklı cümleyle dönmesi mobil
/// tarafta "iki ayrı hata" gibi görünür.
/// </summary>
public static class ServiceErrors
{
    /// <summary>
    /// Token geçerli ama kullanıcı satırı yok. Pratikte yalnızca hesap
    /// silindiğinde görülür; yine de her serviste aynı cümleyle dönmeli.
    /// </summary>
    public const string UserNotFound = "Kullanıcı bulunamadı.";
}
