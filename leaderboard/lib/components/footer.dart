import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';

class Footer extends StatelessWidget {
  const Footer({super.key});

  @override
  Widget build(BuildContext context) {
    final screenWidth = MediaQuery.of(context).size.width;
    final isMobile = screenWidth < 600;

    return Container(
      padding: EdgeInsets.symmetric(
        vertical: isMobile ? 24 : 32,
        horizontal: isMobile ? 16 : 24,
      ),
      color: Colors.black,
      child: Column(
        children: [
          isMobile ? _buildMobileLayout() : _buildDesktopLayout(),
          SizedBox(height: isMobile ? 16 : 24),
          const Divider(color: Colors.white24),
          SizedBox(height: isMobile ? 16 : 24),
          Text(
            '© 2025 Shifting Sands Team. All rights reserved. '
                'University of Bristol COMS30042 Team Project.',
            style: TextStyle(
              color: Colors.white54,
              fontSize: isMobile ? 12 : 14,
              height: 1.4,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }

  Widget _buildDesktopLayout() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Image.asset('assets/images/logo.png', height: 40),
        _buildSocialIcons(),
      ],
    );
  }

  Widget _buildMobileLayout() {
    return Column(
      children: [
        Image.asset('assets/images/logo.png', height: 30),
        const SizedBox(height: 20),
        _buildSocialIcons(isMobile: true),
      ],
    );
  }

  Widget _buildSocialIcons({bool isMobile = false}) {
    return Wrap(
      spacing: isMobile ? 16 : 24,
      children: [
        IconButton(
          iconSize: isMobile ? 20 : 24,
          padding: EdgeInsets.zero,
          onPressed: () {},
          icon: const Icon(FontAwesomeIcons.envelope, color: Colors.white),
        ),
        IconButton(
          iconSize: isMobile ? 20 : 24,
          padding: EdgeInsets.zero,
          onPressed: () {},
          icon: const Icon(FontAwesomeIcons.discord, color: Colors.white),
        ),
        IconButton(
          iconSize: isMobile ? 20 : 24,
          padding: EdgeInsets.zero,
          onPressed: () {},
          icon: const Icon(FontAwesomeIcons.github, color: Colors.white),
        ),
      ],
    );
  }
}