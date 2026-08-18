/// Uygulama genelindeki route yolları. Faz 0'da sadece 4 sekme kullanımda;
/// onboarding/auth yolları burada tanımlı olarak duruyor ama henüz bir
/// [GoRoute]'a bağlanmadı — ilgili ekranlar Faz 1/Faz 3'te yazılınca eklenir.
abstract class RoutePaths {
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

  /// Ana sayfanın altında: aylık takvim ve bir günün adet kaydı.
  static const calendarSegment = 'calendar';
  static const calendar = '$home/$calendarSegment';

  static const periodLogSegment = 'log/:date';

  /// [date] yyyy-MM-dd biçiminde.
  static String periodLog(DateTime date) =>
      '$home/log/${date.year.toString().padLeft(4, '0')}-'
      '${date.month.toString().padLeft(2, '0')}-'
      '${date.day.toString().padLeft(2, '0')}';
}
