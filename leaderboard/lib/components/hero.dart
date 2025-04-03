import 'package:flutter/material.dart';

class HeroWidget extends StatelessWidget {
  final Widget content;

  const HeroWidget({super.key, required this.content});

  @override
  Widget build(BuildContext context) {
    final containerHeight = MediaQuery.of(context).size.height;

    return Stack(
      alignment: Alignment.center,
      children: [
        // Background image with gradient overlay
        ShaderMask(
          shaderCallback: (rect) {
            return LinearGradient(
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
              colors: [
                Colors.black.withOpacity(0.7),
                Colors.black.withOpacity(0.9)
              ],
            ).createShader(rect);
          },
          blendMode: BlendMode.darken,
          child: Image.asset(
            'assets/images/hero_background.png',
            height: containerHeight,
            width: double.infinity,
            fit: BoxFit.cover,
          ),
        ),
        content
      ],
    );
  }
}
