import 'package:flutter/material.dart';

// ignore: avoid_web_libraries_in_flutter
import 'dart:html' as html;

class NavigationUtils {
  static void navigateToRoute(BuildContext context, String route) {
    Navigator.pushReplacementNamed(context, route);
    html.window.history.pushState(null, '', route);
  }
}