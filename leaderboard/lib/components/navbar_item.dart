import 'package:flutter/material.dart';

class NavbarItem extends StatelessWidget {
  final String pageName;
  final IconData iconData;
  final String route;

  const NavbarItem({super.key, required this.pageName, required this.iconData, required this.route});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () => Navigator.pushReplacementNamed(context, route),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(iconData, size: 24),
          const SizedBox(width: 8),
          Text(pageName),
        ],
      ),
    );
  }
}