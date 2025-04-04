import 'package:flutter/material.dart';

class HeroWidget extends StatelessWidget {
  final Widget content;
  final String backgroundImagePath;
  final double? maxHeight;
  final List<Color> gradientColors;

  const HeroWidget({
    super.key,
    required this.content,
    this.backgroundImagePath = 'assets/images/hero_background.png',
    this.maxHeight,
    this.gradientColors = const [
      Color.fromRGBO(0, 0, 0, 0.7),
      Color.fromRGBO(0, 0, 0, 0.9),
    ],
  });

  @override
  Widget build(BuildContext context) {
    // Get screen dimensions
    final screenSize = MediaQuery.of(context).size;
    final screenHeight = screenSize.height;
    final screenWidth = screenSize.width;

    // Determine if we're on a mobile device
    final isMobile = screenWidth < 600;

    // Calculate container height based on device
    final containerHeight = maxHeight ?? screenHeight;

    // Add padding that adjusts based on screen size
    final horizontalPadding = screenWidth * (isMobile ? 0.05 : 0.1);

    return Container(
      constraints: BoxConstraints(
        maxHeight: containerHeight,
        minHeight: 400, // Ensures minimum reasonable height
      ),
      width: double.infinity,
      child: Stack(
        alignment: Alignment.center,
        children: [
          // Background image with gradient overlay
          ShaderMask(
            shaderCallback: (rect) {
              return LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: gradientColors,
              ).createShader(rect);
            },
            blendMode: BlendMode.darken,
            child: Image.asset(
              backgroundImagePath,
              height: containerHeight,
              width: double.infinity,
              fit: BoxFit.cover,
            ),
          ),
          // Content with responsive padding
          Padding(
            padding: EdgeInsets.symmetric(
              horizontal: horizontalPadding,
              vertical: screenHeight * 0.05,
            ),
            child: LayoutBuilder(
              builder: (context, constraints) {
                return Container(
                  width: constraints.maxWidth,
                  constraints: BoxConstraints(
                    maxWidth: 1200, // Maximum width for content on larger screens
                  ),
                  child: content,
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}