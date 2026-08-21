import '../utils/date_format.dart';

/// Uygulama genelindeki route yolları. Auth ve onboarding akışı alt menüsüz,
/// içerik ekranları 4 sekmeli shell'in içinde tanımlı.
abstract class RoutePaths {
  /// Açılış yolu. Kalıcı bir ekran değil: oturum geri kurulurken görünür,
  /// sonuç belli olunca yönlendirme kullanıcıyı gideceği yere alır.
  static const splash = '/';

  static const login = '/login';
  static const signUp = '/sign-up';
  static const verifyCode = '/verify-code';
  static const forgotPassword = '/forgot-password';
  static const newPassword = '/new-password';
  static const onboarding = '/onboarding';

  static const home = '/home';
  static const nutrition = '/nutrition';
  static const exercise = '/exercise';
  static const profile = '/profile';

  /// Profilin altında iç içe route: alt menü açık kalır, geri tuşu profile
  /// döner. [cycleSettingsSegment] router'daki göreli yol, [cycleSettings]
  /// navigasyonda kullanılan tam yol.
  static const cycleSettingsSegment = 'cycle';
  static const cycleSettings = '$profile/$cycleSettingsSegment';

  static const accountSettingsSegment = 'account';
  static const accountSettings = '$profile/$accountSettingsSegment';

  /// Veri dışa aktarma ve hesap silme.
  static const privacySegment = 'privacy';
  static const privacy = '$profile/$privacySegment';

  /// Ana sayfanın altında: aylık takvim ve bir günün adet kaydı.
  static const calendarSegment = 'calendar';
  static const calendar = '$home/$calendarSegment';

  static const periodLogSegment = 'log/:date';

  /// [date] yyyy-MM-dd biçiminde.
  static String periodLog(DateTime date) => '$home/log/${toIsoDate(date)}';

  /// Oturum gerektirmeyen yollar. Onboarding bilinçli olarak dışarıda:
  /// sihirbaz zaten giriş yapmış bir kullanıcıyı tamamlıyor.
  static const _public = {
    login,
    signUp,
    verifyCode,
    forgotPassword,
    newPassword,
  };

  static bool isPublic(String location) => _public.contains(location);
}
