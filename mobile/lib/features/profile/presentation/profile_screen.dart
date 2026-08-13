import 'package:flutter/material.dart';

/// Faz 0 placeholder. Faz 2'de döngü ayarları stub'ıyla,
/// Faz 3'te tam profil/ayarlar ekranıyla değiştirilecek.
class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Profil')),
      body: const Center(child: Text('Profil — Faz 2/3')),
    );
  }
}
