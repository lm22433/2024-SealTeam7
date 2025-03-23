import 'package:flutter/material.dart';
import 'package:shifting_sands/components/navbar.dart';

class NotFoundPage extends StatelessWidget {
  const NotFoundPage({super.key});

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      appBar: Navbar(),
      body: Center(
        child: Text('404 Page Not Found'),
      ),
    );
  }
}