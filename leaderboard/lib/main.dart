import 'package:firebase_app_check/firebase_app_check.dart';
import 'package:firebase_core/firebase_core.dart';
import 'firebase_options.dart';
import 'package:flutter/material.dart';

// ignore: avoid_web_libraries_in_flutter
import 'dart:html';

import 'pages/home_page.dart';
import 'pages/leaderboard_page.dart';
import 'pages/not_found_page.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);
  await FirebaseAppCheck.instance.activate(
    webProvider: ReCaptchaV3Provider('6LcHa_UqAAAAAI50LKwoXvbxTT9QLvmpvUCKC_Fb')
  );
  runApp(const ShiftingSandsApp());
}

class ShiftingSandsApp extends StatelessWidget {
  const ShiftingSandsApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Shifting Sands',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFFD2B48C), // Sand color
          brightness: Brightness.dark,
        ),
        useMaterial3: true,
        fontFamily: 'JetBrainsMono',
      ),
      initialRoute: window.location.pathname ?? "/",
      onGenerateRoute: (settings) {
        switch (settings.name) {
          case '/':
            return PageRouteBuilder(
                pageBuilder: (_, __, ___) => const HomePage(),
              transitionDuration: Duration.zero,
              reverseTransitionDuration: Duration.zero,
            );
          case '/leaderboard':
            return PageRouteBuilder(
              pageBuilder: (_, __, ___) => const LeaderboardPage(),
              transitionDuration: Duration.zero,
              reverseTransitionDuration: Duration.zero,
            );
          default:
            return PageRouteBuilder(
              pageBuilder: (_, __, ___) => const NotFoundPage(),
              transitionDuration: Duration.zero,
              reverseTransitionDuration: Duration.zero,
            );
        }
      },
    );
  }
}
