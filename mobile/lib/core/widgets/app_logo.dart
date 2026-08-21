import 'package:flutter/material.dart';

/// Uygulama logosu: giriş/kayıt ekranlarındaki "Serene Cycle" yazısının
/// yerini alan çizgi çizim logo.
class AppLogo extends StatelessWidget {
  const AppLogo({this.height = 120, super.key});

  static const assetPath = 'assets/images/logo.png';

  final double height;

  @override
  Widget build(BuildContext context) =>
      Image.asset(assetPath, height: height);
}
