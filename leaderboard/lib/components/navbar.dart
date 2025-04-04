import 'package:flutter/material.dart';

class Navbar extends StatelessWidget implements PreferredSizeWidget {
  const Navbar({super.key});

  @override
  Widget build(BuildContext context) {
    final screenWidth = MediaQuery.of(context).size.width;
    final isMobile = screenWidth < 600;
    final isTablet = screenWidth >= 600 && screenWidth < 1200;

    return AppBar(
      backgroundColor: Colors.transparent,
      elevation: 0,
      title: GestureDetector(
        onTap: () => Navigator.pushReplacementNamed(context, '/'),
        child: Row(
          children: [
            Image.asset(
              'assets/images/logo.png',
              height: isMobile ? 32 : 40,
            ),
            const SizedBox(width: 12),
            Text(
              'Shifting Sands',
              style: TextStyle(
                color: Colors.white,
                fontSize: isMobile ? 16 : (isTablet ? 18 : 20),
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
      ),
      actions: _buildNavbarItems(context, isMobile, isTablet),
    );
  }

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);

  List<Widget> _buildNavbarItems(
      BuildContext context, bool isMobile, bool isTablet) {
    final Map<String, String> items = {
      'LEADERBOARD': '/leaderboard',
      // 'GAME': '/game',
      // 'ABOUT': '/about',
      // 'TEAM': '/team',
      // 'DEVELOPMENT': '/development',
    };

    if (isMobile) {
      return [
        IconButton(
          icon: const Icon(Icons.menu, color: Colors.white),
          onPressed: () => _showMobileMenu(context, items),
        ),
      ];
    }

    return [
      Padding(
        padding: EdgeInsets.symmetric(horizontal: isTablet ? 12 : 24),
        child: Row(
          children: items.entries.map((item) {
            return Padding(
              padding: EdgeInsets.symmetric(horizontal: isTablet ? 8 : 12),
              child: TextButton(
                onPressed: () =>
                    Navigator.pushReplacementNamed(context, item.value),
                style: TextButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                ),
                child: Text(
                  item.key,
                  style: TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.bold,
                    fontSize: isTablet ? 14 : 16,
                  ),
                ),
              ),
            );
          }).toList(),
        ),
      ),
    ];
  }

  void _showMobileMenu(BuildContext context, Map<String, String> items) {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.black.withOpacity(0.9),
      builder: (context) => Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Menu',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close, color: Colors.white),
                  onPressed: () => Navigator.pop(context),
                ),
              ],
            ),
          ),
          ...items.entries.map((item) => ListTile(
            title: Text(
              item.key,
              style: const TextStyle(color: Colors.white),
            ),
            onTap: () {
              Navigator.pop(context);
              Navigator.pushReplacementNamed(context, item.value);
            },
          )),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}