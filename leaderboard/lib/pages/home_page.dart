import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:shifting_sands/components/navbar.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<StatefulWidget> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
        appBar: const Navbar(),
        body: SingleChildScrollView(
            child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Stack(
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
                    'assets/images/hero_background.jpg',
                    height: 600,
                    width: double.infinity,
                    fit: BoxFit.cover,
                  ),
                ),
                // Hero content
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 24),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Text(
                        'SHIFTING SANDS',
                        style: TextStyle(
                          fontSize: 48,
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                          letterSpacing: 2,
                        ),
                      ),
                      const SizedBox(height: 16),
                      const Text(
                        'A COMS30042 Team Project',
                        style: TextStyle(
                          fontSize: 18,
                          color: Colors.white70,
                          letterSpacing: 1.5,
                        ),
                      ),
                      const SizedBox(height: 32),
                      ElevatedButton(
                        onPressed: () {},
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFFD2B48C),
                          foregroundColor: Colors.black,
                          padding: const EdgeInsets.symmetric(
                              horizontal: 32, vertical: 16),
                          textStyle: const TextStyle(
                              fontSize: 18, fontWeight: FontWeight.bold),
                        ),
                        child: const Text('VIEW THE LEADERBOARD'),
                      ),
                    ],
                  ),
                ),
              ],
            ),

            // Game Overview Section
            Container(
              padding: const EdgeInsets.symmetric(vertical: 64, horizontal: 24),
              color: Colors.black87,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  const Text(
                    'GAME OVERVIEW',
                    style: TextStyle(
                      fontSize: 32,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFFD2B48C),
                      letterSpacing: 1.5,
                    ),
                  ),
                  const SizedBox(height: 48),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'The Ultimate Battle Simulation',
                              style: TextStyle(
                                fontSize: 24,
                                fontWeight: FontWeight.bold,
                                color: Colors.white,
                              ),
                            ),
                            const SizedBox(height: 16),
                            const Text(
                              'Shifting Sands is an innovative battle simulation where one Gamemaster faces off against waves of enemy AI. Using a real-time interactive sand terrain, players can manipulate the battlefield to defend their central core from increasingly difficult hordes of enemies.',
                              style: TextStyle(
                                fontSize: 16,
                                color: Colors.white70,
                                height: 1.6,
                              ),
                            ),
                            const SizedBox(height: 24),
                            Row(
                              children: [
                                _buildFeatureItem(
                                    Icons.gamepad, 'Interactive Terrain'),
                                const SizedBox(width: 24),
                                _buildFeatureItem(Icons.public, 'Godly Hands'),
                                const SizedBox(width: 24),
                                _buildFeatureItem(Icons.psychology, 'Enemy AI'),
                              ],
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 48),
                      Expanded(
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: Image.asset(
                            'assets/images/gameplay1.jpg',
                            fit: BoxFit.cover,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),

            // Game Features Gallery
            Container(
              padding: const EdgeInsets.symmetric(vertical: 64, horizontal: 24),
              color: const Color(0xFF1A1A1A),
              child: Column(
                children: [
                  const Text(
                    'GAME FEATURES',
                    style: TextStyle(
                      fontSize: 32,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFFD2B48C),
                      letterSpacing: 1.5,
                    ),
                  ),
                  const SizedBox(height: 48),
                  GridView.count(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    crossAxisCount: 3,
                    mainAxisSpacing: 20,
                    crossAxisSpacing: 20,
                    children: [
                      _buildFeatureCard(
                        'assets/images/feature1.jpg',
                        'Enemy Types',
                        'Multiple enemy tiers with unique abilities and challenges',
                      ),
                      _buildFeatureCard(
                        'assets/images/feature2.jpg',
                        'Sand Manipulation',
                        'Shape the terrain to create defenses or traps',
                      ),
                      _buildFeatureCard(
                        'assets/images/feature3.jpg',
                        'Central Core',
                        'Protect your base from the encroaching enemy forces',
                      ),
                      _buildFeatureCard(
                        'assets/images/feature4.jpg',
                        'Score System',
                        'Earn points for kills, combos, and survival time',
                      ),
                      _buildFeatureCard(
                        'assets/images/feature5.jpg',
                        'Real-time Feedback',
                        'Experience responsive terrain manipulation',
                      ),
                      _buildFeatureCard(
                        'assets/images/feature6.jpg',
                        'Gesture System',
                        'Trigger special abilities with hand gestures',
                      ),
                    ],
                  ),
                ],
              ),
            ),

            // Development Progress
            Container(
              padding: const EdgeInsets.symmetric(vertical: 64, horizontal: 24),
              color: Colors.black87,
              child: Column(
                children: [
                  const Text(
                    'DEVELOPMENT PROGRESS',
                    style: TextStyle(
                      fontSize: 32,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFFD2B48C),
                      letterSpacing: 1.5,
                    ),
                  ),
                  const SizedBox(height: 48),
                  Row(
                    children: [
                      Expanded(
                        child: Container(
                          padding: const EdgeInsets.all(24),
                          decoration: BoxDecoration(
                            color: const Color(0xFF2A2A2A),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'MVP Stage',
                                style: TextStyle(
                                  fontSize: 24,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.white,
                                ),
                              ),
                              const SizedBox(height: 16),
                              const Text(
                                'We are currently working on the Minimum Viable Product (MVP) for Shifting Sands, focusing on real-time sand feedback, enemy AI, and core gameplay mechanics.',
                                style: TextStyle(
                                  fontSize: 16,
                                  color: Colors.white70,
                                  height: 1.6,
                                ),
                              ),
                              const SizedBox(height: 24),
                              LinearProgressIndicator(
                                value: 0.65,
                                backgroundColor: Colors.grey[800],
                                valueColor: const AlwaysStoppedAnimation<Color>(
                                    Color(0xFFD2B48C)),
                              ),
                              const SizedBox(height: 8),
                              const Text(
                                '65% Complete',
                                style: TextStyle(
                                  color: Colors.white70,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(width: 24),
                      Expanded(
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: Image.asset(
                            'assets/images/mvp.jpg',
                            fit: BoxFit.cover,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 48),
                  Row(
                    children: [
                      Expanded(
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: Image.asset(
                            'assets/images/beta.jpg',
                            fit: BoxFit.cover,
                          ),
                        ),
                      ),
                      const SizedBox(width: 24),
                      Expanded(
                        child: Container(
                          padding: const EdgeInsets.all(24),
                          decoration: BoxDecoration(
                            color: const Color(0xFF2A2A2A),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Beta Stage',
                                style: TextStyle(
                                  fontSize: 24,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.white,
                                ),
                              ),
                              const SizedBox(height: 16),
                              const Text(
                                'The beta release will include improved stability, additional game mechanics, and multiplayer support for testing.',
                                style: TextStyle(
                                  fontSize: 16,
                                  color: Colors.white70,
                                  height: 1.6,
                                ),
                              ),
                              const SizedBox(height: 24),
                              LinearProgressIndicator(
                                value: 0.65,
                                backgroundColor: Colors.grey[800],
                                valueColor: const AlwaysStoppedAnimation<Color>(
                                    Color(0xFFD2B48C)),
                              ),
                              const SizedBox(height: 8),
                              const Text(
                                '20% Complete',
                                style: TextStyle(
                                  color: Colors.white70,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 48),
                  Row(
                    children: [
                      Expanded(
                        child: Container(
                          padding: const EdgeInsets.all(24),
                          decoration: BoxDecoration(
                            color: const Color(0xFF2A2A2A),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Final Stage',
                                style: TextStyle(
                                  fontSize: 24,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.white,
                                ),
                              ),
                              const SizedBox(height: 16),
                              const Text(
                                'The final release will be fully optimized with polished gameplay, enhanced visuals, and complete content.',
                                style: TextStyle(
                                  fontSize: 16,
                                  color: Colors.white70,
                                  height: 1.6,
                                ),
                              ),
                              const SizedBox(height: 24),
                              LinearProgressIndicator(
                                value: 0.65,
                                backgroundColor: Colors.grey[800],
                                valueColor: const AlwaysStoppedAnimation<Color>(
                                    Color(0xFFD2B48C)),
                              ),
                              const SizedBox(height: 8),
                              const Text(
                                '65% Complete',
                                style: TextStyle(
                                  color: Colors.white70,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(width: 24),
                      Expanded(
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: Image.asset(
                            'assets/images/development.jpg',
                            fit: BoxFit.cover,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),

            // University Project Section
            Container(
              padding: const EdgeInsets.symmetric(vertical: 64, horizontal: 24),
              color: const Color(0xFF1A1A1A),
              child: Row(
                children: [
                  Image.asset(
                    'assets/images/university_logo.png',
                    height: 120,
                  ),
                  const SizedBox(width: 48),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'UNIVERSITY OF BRISTOL',
                          style: TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                        SizedBox(height: 8),
                        Text(
                          'COMS30042 Team Project',
                          style: TextStyle(
                            fontSize: 18,
                            color: Color(0xFFD2B48C),
                          ),
                        ),
                        SizedBox(height: 16),
                        Text(
                          'This game is being developed as part of the COMS30042 Team Project at the University of Bristol. Our team is working together to create an innovative gaming experience that combines physical interaction with digital gameplay.',
                          style: TextStyle(
                            fontSize: 16,
                            color: Colors.white70,
                            height: 1.6,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            // Footer
            Container(
              padding: const EdgeInsets.symmetric(vertical: 32, horizontal: 24),
              color: Colors.black,
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Image.asset('assets/images/logo.png', height: 40),
                      Row(
                        children: [
                          IconButton(
                            onPressed: () {},
                            icon: const Icon(FontAwesomeIcons.envelope,
                                color: Colors.white),
                          ),
                          IconButton(
                            onPressed: () {},
                            icon: const Icon(FontAwesomeIcons.discord,
                                color: Colors.white),
                          ),
                          IconButton(
                            onPressed: () {},
                            icon: const Icon(FontAwesomeIcons.github,
                                color: Colors.white),
                          ),
                        ],
                      ),
                    ],
                  ),
                  const SizedBox(height: 24),
                  const Divider(color: Colors.white24),
                  const SizedBox(height: 24),
                  const Text(
                    '© 2025 Shifting Sands Team. All rights reserved. University of Bristol COMS30042 Team Project.',
                    style: TextStyle(
                      color: Colors.white54,
                      fontSize: 14,
                    ),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            ),
          ],
        )));
  }

  Widget _buildFeatureItem(IconData icon, String text) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, color: const Color(0xFFD2B48C), size: 20),
        const SizedBox(width: 8),
        Text(
          text,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 14,
            fontWeight: FontWeight.w500,
          ),
        ),
      ],
    );
  }

  Widget _buildFeatureCard(String imagePath, String title, String description) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF2A2A2A),
        borderRadius: BorderRadius.circular(8),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.2),
            spreadRadius: 1,
            blurRadius: 5,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Image.asset(
              imagePath,
              width: double.infinity,
              fit: BoxFit.cover,
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  description,
                  style: const TextStyle(
                    fontSize: 14,
                    color: Colors.white70,
                    height: 1.5,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
