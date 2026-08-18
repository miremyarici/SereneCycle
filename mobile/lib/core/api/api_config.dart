import 'dart:io' show Platform;

import 'package:flutter/foundation.dart' show kIsWeb;

abstract class ApiConfig {
  static const _port = 5147;

  /// Android emülatörü ana makineye `localhost` ile ulaşamaz; emülatörün
  /// kendi loopback'i olur. 10.0.2.2 emülatörden ana makineye giden adres.
  static String get baseUrl {
    if (kIsWeb) {
      return 'http://localhost:$_port';
    }

    if (Platform.isAndroid) {
      return 'http://10.0.2.2:$_port';
    }

    return 'http://localhost:$_port';
  }
}
