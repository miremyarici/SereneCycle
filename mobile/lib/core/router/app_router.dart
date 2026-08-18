import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/forgot_password_screen.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/auth/presentation/new_password_screen.dart';
import '../../features/auth/presentation/sign_up_screen.dart';
import '../../features/auth/presentation/verify_code_screen.dart';
import '../../features/exercise/presentation/exercise_screen.dart';
import '../../features/home/presentation/calendar_screen.dart';
import '../../features/home/presentation/home_screen.dart';
import '../../features/logs/presentation/period_log_screen.dart';
import '../../features/nutrition/presentation/nutrition_screen.dart';
import '../../features/onboarding/presentation/onboarding_screen.dart';
import '../../features/profile/presentation/account_settings_screen.dart';
import '../../features/profile/presentation/cycle_settings_screen.dart';
import '../../features/profile/presentation/profile_screen.dart';
import 'app_shell.dart';
import 'route_paths.dart';

final _rootNavigatorKey = GlobalKey<NavigatorState>();

/// Uygulama giriş ekranından başlar. Auth akışı ve onboarding shell'in
/// dışında (alt menüsüz), içerik ekranları 4 sekmeli shell'in içinde.
final appRouterProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    navigatorKey: _rootNavigatorKey,
    initialLocation: RoutePaths.login,
    routes: [
      // --- transactional / alt menüsüz ---
      GoRoute(
        path: RoutePaths.login,
        name: 'login',
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: RoutePaths.signUp,
        name: 'signUp',
        builder: (context, state) => const SignUpScreen(),
      ),
      GoRoute(
        path: RoutePaths.verifyCode,
        name: 'verifyCode',
        // Bir önceki ekran e-postayı `extra` ile taşır (route param değil):
        // hassas veri URL'e/geçmişe yazılmasın.
        builder: (context, state) =>
            VerifyCodeScreen(email: state.extra as String? ?? ''),
      ),
      GoRoute(
        path: RoutePaths.forgotPassword,
        name: 'forgotPassword',
        builder: (context, state) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: RoutePaths.newPassword,
        name: 'newPassword',
        builder: (context, state) =>
            NewPasswordScreen(email: state.extra as String? ?? ''),
      ),
      GoRoute(
        path: RoutePaths.onboarding,
        name: 'onboarding',
        builder: (context, state) => const OnboardingScreen(),
      ),

      // --- 4 sekmeli shell (alt menü) ---
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            AppShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: RoutePaths.home,
                name: 'home',
                builder: (context, state) => const HomeScreen(),
                routes: [
                  GoRoute(
                    path: RoutePaths.calendarSegment,
                    name: 'calendar',
                    builder: (context, state) => const CalendarScreen(),
                  ),
                  GoRoute(
                    path: RoutePaths.periodLogSegment,
                    name: 'periodLog',
                    // Tarih yolun bir parçası: kullanıcı geri gelip aynı
                    // güne dönebilsin ve derin bağlantı çalışsın.
                    builder: (context, state) => PeriodLogScreen(
                      date: DateTime.parse(state.pathParameters['date']!),
                    ),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: RoutePaths.nutrition,
                name: 'nutrition',
                builder: (context, state) => const NutritionScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: RoutePaths.exercise,
                name: 'exercise',
                builder: (context, state) => const ExerciseScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: RoutePaths.profile,
                name: 'profile',
                builder: (context, state) => const ProfileScreen(),
                routes: [
                  GoRoute(
                    path: RoutePaths.accountSettingsSegment,
                    name: 'accountSettings',
                    builder: (context, state) => const AccountSettingsScreen(),
                  ),
                  GoRoute(
                    path: RoutePaths.cycleSettingsSegment,
                    name: 'cycleSettings',
                    builder: (context, state) => const CycleSettingsScreen(),
                  ),
                ],
              ),
            ],
          ),
        ],
      ),
    ],
  );
});
