import 'package:flutter/material.dart';

class Navbar extends StatelessWidget implements PreferredSizeWidget {
  const Navbar({super.key});

  @override
  Widget build(BuildContext context) {
    return AppBar(
      backgroundColor: Colors.transparent,
      elevation: 0,
      title: GestureDetector(
        onTap: () => Navigator.pushReplacementNamed(context, '/'),
        child: Row(
          children: [
            Image.asset('assets/images/logo.png', height: 40),
            const SizedBox(width: 10),
            const Text(
              'Shifting Sands',
              style: TextStyle(
                color: Colors.white,
                fontSize: 20,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
      ),
      actions: _buildNavbarItems(context),
    );
  }

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);

  List<Widget> _buildNavbarItems(BuildContext context) {
    final Map<String, String> items = {
      'LEADERBOARD': '/leaderboard',
      'GAME': '/game',
      'ABOUT': '/about',
      'TEAM': '/team',
      'DEVELOPMENT': '/development',
    };

    return [
      Wrap(
        spacing: 20, // Adjust spacing between buttons
        children: items.entries.map((item) {
          return GestureDetector(
            onTap: () => Navigator.pushReplacementNamed(context, item.value),
            child: Text(
              item.key,
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
              ),
            ),
          );
        }).toList(),
      ),
      const SizedBox(width: 20),
    ];
  }
}
