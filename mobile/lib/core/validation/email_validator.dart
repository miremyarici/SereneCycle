/// Üç auth ekranı da aynı iki kuralı tekrarlıyordu. Gerçek doğrulama
/// sunucuda; buradaki kontrol yalnızca kullanıcıyı erken uyarmak için.
String? validateEmail(String? value) {
  if (value == null || value.trim().isEmpty) {
    return 'E-posta gerekli';
  }
  if (!value.contains('@')) {
    return 'Geçerli bir e-posta gir';
  }
  return null;
}
