import 'package:flutter/material.dart';
import 'package:shifting_sands/components/hero.dart';
import 'package:shifting_sands/components/footer.dart';
import 'package:shifting_sands/components/navbar.dart';

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: const Navbar(),
      body: SingleChildScrollView(
          child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Hero Section
          HeroWidget(content: _buildHeroContent(context)),

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
                            'Shifting Sands is an innovative battle simulation where one Gamemaster faces off against waves of enemy AI. Using a real-time interactive sand terrain, players can manipulate the battlefield to defend their central core from increasingly difficult waves of enemies.',
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
                              _buildFeatureItem(Icons.public, 'Hand Reconstruction'),
                              const SizedBox(width: 24),
                              _buildFeatureItem(Icons.psychology, 'Custom Enemy AI'),
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
                      'assets/gifs/terrain.gif',
                      'Terrain Generation',
                      'Real-time terrain generation using the Azure Kinect v3',
                    ),
                    _buildFeatureCard(
                      'assets/gifs/hands.gif',
                      'Hand Reconstruction',
                      'Fully responsive hand-tracking and reconstruction in-game',
                    ),
                    _buildFeatureCard(
                      'assets/gifs/???.gif',
                      '???',
                      '???',
                    ),
                    _buildFeatureCard(
                      'assets/gifs/enemy.gif',
                      'Enemies',
                      'Several custom enemies designed, modelled and implemented in-house',
                    ),
                    _buildFeatureCard(
                      'assets/gifs/ai.gif',
                      'Custom Enemy AI',
                      'Custom enemy AI implementation built from scratch based on A* search',
                    ),
                    _buildFeatureCard(
                      'assets/gifs/???.gif',
                      '???',
                      '???',
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
                IntrinsicHeight(
                  child: Row(
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
                                'The MVP release was the initial implementation of our original idea to have one Gamemaster against a team of digital, real players. We used the Azure Kinect v3 to capture both depth images and colour images of the sandbox. At this stage, the depth image was used to map the topology of the sand into in-game terrain that could be used as the battlefield for the digital players. We used an open-source networking library called FishNet to network the game, however we found that sending the height data over the network was extremely inefficient.\n\nFollowing feedback from extensive user testing, we decided to pivot and take the tech we had implemented and focus in on the core mechanics that testers found fun, the sand!',
                                style: TextStyle(
                                  fontSize: 16,
                                  color: Colors.white70,
                                  height: 1.6,
                                ),
                              ),
                              const SizedBox(height: 24),
                              LinearProgressIndicator(
                                value: 1.0,
                                backgroundColor: Colors.grey[800],
                                valueColor: const AlwaysStoppedAnimation<Color>(
                                    Color(0xFFD2B48C)),
                              ),
                              const SizedBox(height: 8),
                              const Text(
                                '100% Complete',
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
                            'assets/images/mvp.png',
                            fit: BoxFit.cover,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 48),
                IntrinsicHeight(
                  child: Row(
                    children: [
                      Expanded(
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: Image.asset(
                            'assets/images/beta.png',
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
                                'The beta release included a full shift of focus into what testers found fun, the sand. We removed the multiplayer aspect to improve performance and replaced real players with enemy AI.',
                                style: TextStyle(
                                  fontSize: 16,
                                  color: Colors.white70,
                                  height: 1.6,
                                ),
                              ),
                              const SizedBox(height: 24),
                              LinearProgressIndicator(
                                value: 1,
                                backgroundColor: Colors.grey[800],
                                valueColor: const AlwaysStoppedAnimation<Color>(
                                    Color(0xFFD2B48C)),
                              ),
                              const SizedBox(height: 8),
                              const Text(
                                '100% Complete',
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
                ),
                const SizedBox(height: 48),
                IntrinsicHeight(
                  child: Row(
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
                            'assets/images/final.jpeg',
                            fit: BoxFit.cover,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const Footer(),
        ],
      )),
    );
  }

  Widget _buildHeroContent(BuildContext context) {
    return Padding(
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
            'A COMS30042 Team Project by Seal Team 7',
            style: TextStyle(
              fontSize: 18,
              color: Colors.white70,
              letterSpacing: 1.5,
            ),
          ),
          const SizedBox(height: 32),
          ElevatedButton(
            onPressed: () =>
                Navigator.pushReplacementNamed(context, '/leaderboard'),
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFD2B48C),
              foregroundColor: Colors.black,
              padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 16),
              textStyle:
                  const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            child: const Text('VIEW THE LEADERBOARD'),
          ),
        ],
      ),
    );
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
