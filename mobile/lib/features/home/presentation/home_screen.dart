import 'package:flutter/material.dart';

/// Faz 0 placeholder. Faz 1'de faz kartı, ilerleme halkası ve
/// yatay takvim şeridiyle değiştirilecek.
class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Serene Cycle')),
      body: const Center(child: Text('Ana Sayfa — Faz 1')),
    );
  }
}
