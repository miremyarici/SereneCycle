/// Backend'in Identity şifre kuralıyla birebir eşleşir
/// (`AddInfrastructure` içindeki `PasswordOptions`): en az 8 karakter,
/// en az bir büyük harf, bir küçük harf, bir rakam. Özel karakter şart değil.
String? validatePassword(String? value) {
  if (value == null || value.isEmpty) {
    return 'Şifre gerekli';
  }
  if (value.length < 8) {
    return 'Şifre en az 8 karakter olmalı';
  }
  if (!value.contains(RegExp(r'[A-Z]'))) {
    return 'Şifre en az bir büyük harf içermeli';
  }
  if (!value.contains(RegExp(r'[a-z]'))) {
    return 'Şifre en az bir küçük harf içermeli';
  }
  if (!value.contains(RegExp(r'[0-9]'))) {
    return 'Şifre en az bir rakam içermeli';
  }
  return null;
}
