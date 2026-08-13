import 'package:flutter/material.dart';

/// Faz 0 placeholder. Faz 2'de "önerilen/şimdilik uygun olmayan"
/// hareket kartlarıyla değiştirilecek.
class ExerciseScreen extends StatelessWidget {
  const ExerciseScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Hareket')),
      body: const Center(child: Text('Egzersiz Önerileri — Faz 2')),
    );
  }
}
