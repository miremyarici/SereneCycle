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
}
