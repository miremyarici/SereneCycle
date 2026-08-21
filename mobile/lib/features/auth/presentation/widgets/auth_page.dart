import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Auth akışındaki beş ekranın ortak iskeleti: kaydırılabilir, dar ekranda
/// taşmayan, geniş ekranda ortalanmış ve sabit genişlikte bir sütun.
class AuthPage extends StatelessWidget {
  const AuthPage({
    required this.child,
    this.padding = const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
    this.hasBackButton = false,
    this.title,
    this.isCentered = true,
    super.key,
  });

  /// Tasarımdaki kart genişliği; tablet/masaüstünde form yayılmasın diye.
  static const maxContentWidth = 448.0;

  final Widget child;
  final EdgeInsetsGeometry padding;

  /// true ise geri oklu bir [AppBar] çizilir.
  final bool hasBackButton;
  final Widget? title;

  /// İçerik ekrandan kısaysa dikeyde ortalansın mı.
  final bool isCentered;

  @override
  Widget build(BuildContext context) {
    final content = SingleChildScrollView(
      padding: padding,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: maxContentWidth),
        child: child,
      ),
    );

    return Scaffold(
      appBar: hasBackButton
          ? AppBar(
              leading: IconButton(
                icon: const Icon(Icons.arrow_back),
                onPressed: () => context.pop(),
              ),
              title: title,
              centerTitle: true,
            )
          : null,
      body: SafeArea(
        child: isCentered
            ? Center(child: content)
            : Align(alignment: Alignment.topCenter, child: content),
      ),
    );
  }
}
