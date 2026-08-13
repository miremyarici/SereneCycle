import 'package:flutter/material.dart';

/// Faz 0 placeholder. Faz 2'de "önerilen/sınırlı" içerik kartları
/// ve Faz 4'te Yiyecek Tara butonuyla değiştirilecek.
class NutritionScreen extends StatelessWidget {
  const NutritionScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Beslenme')),
      body: const Center(child: Text('Beslenme Desteği — Faz 2')),
    );
  }
}
