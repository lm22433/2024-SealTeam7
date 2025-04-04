import 'package:flutter/material.dart';
import 'package:shifting_sands/components/hero.dart';
import 'package:shifting_sands/components/footer.dart';
import 'package:shifting_sands/components/navbar.dart';

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    final screenWidth = MediaQuery.of(context).size.width;
    final isMobile = screenWidth < 600;
    final isTablet = screenWidth >= 600 && screenWidth < 1200;

    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: const Navbar(),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            HeroWidget(content: _buildHeroContent(context)),
            _buildGameOverview(context, isMobile),
            _buildGameFeatures(context, isMobile, isTablet),
            _buildDevelopmentProgress(context, isMobile),
            const Footer(),
          ],
        ),
      ),
    );
  }

  Widget _buildGameOverview(BuildContext context, bool isMobile) {
    return Container(
      padding: EdgeInsets.symmetric(
        vertical: isMobile ? 32 : 64,
        horizontal: isMobile ? 16 : 24,
      ),
      color: Colors.black87,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Text(
            'GAME OVERVIEW',
            style: TextStyle(
              fontSize: isMobile ? 24 : 32,
              fontWeight: FontWeight.bold,
              color: const Color(0xFFD2B48C),
              letterSpacing: 1.5,
            ),
          ),
          const SizedBox(height: 32),
          LayoutBuilder(
            builder: (context, constraints) {
              if (isMobile) {
                return _buildMobileOverview();
              }
              return _buildDesktopOverview(constraints.maxWidth);
            },
          ),
        ],
      ),
    );
  }

  Widget _buildMobileOverview() {
    return Column(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(8),
          child: Image.asset(
            'assets/images/gameplay1.jpg',
            fit: BoxFit.cover,
          ),
        ),
        const SizedBox(height: 24),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'The Ultimate Battle Simulation',
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: Colors.white,
              ),
            ),
            const SizedBox(height: 16),
            const Text(
              'Shifting Sands is an innovative battle simulation where one Gamemaster faces off against waves of enemy AI. Using a real-time interactive sand terrain, players can manipulate the battlefield to defend their central core from increasingly difficult waves of enemies.',
              style: TextStyle(
                fontSize: 14,
                color: Colors.white70,
                height: 1.6,
              ),
            ),
            const SizedBox(height: 24),
            Wrap(
              spacing: 16,
              runSpacing: 16,
              children: [
                _buildFeatureItem(Icons.gamepad, 'Interactive Terrain'),
                _buildFeatureItem(Icons.public, 'Hand Reconstruction'),
                _buildFeatureItem(Icons.psychology, 'Custom Enemy AI'),
              ],
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildDesktopOverview(double maxWidth) {
    return Row(
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
                  _buildFeatureItem(Icons.gamepad, 'Interactive Terrain'),
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
    );
  }

  Widget _buildGameFeatures(
      BuildContext context, bool isMobile, bool isTablet) {
    return Container(
      padding: EdgeInsets.symmetric(
        vertical: isMobile ? 32 : 64,
        horizontal: isMobile ? 16 : 24,
      ),
      color: const Color(0xFF1A1A1A),
      child: Column(
        children: [
          Text(
            'GAME FEATURES',
            style: TextStyle(
              fontSize: isMobile ? 24 : 32,
              fontWeight: FontWeight.bold,
              color: const Color(0xFFD2B48C),
              letterSpacing: 1.5,
            ),
          ),
          const SizedBox(height: 32),
          LayoutBuilder(
            builder: (context, constraints) {
              int crossAxisCount = 1;
              if (constraints.maxWidth > 1200) {
                crossAxisCount = 3;
              } else if (constraints.maxWidth > 600) {
                crossAxisCount = 2;
              }
              return GridView.count(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                crossAxisCount: crossAxisCount,
                mainAxisSpacing: 16,
                crossAxisSpacing: 16,
                childAspectRatio: isMobile ? 0.8 : 1,
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
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildDevelopmentProgress(BuildContext context, bool isMobile) {
    return Container(
      padding: EdgeInsets.symmetric(
        vertical: isMobile ? 24 : 48,
        horizontal: isMobile ? 12 : 24,
      ),
      color: Colors.black87,
      child: Column(
        children: [
          Text(
            'DEVELOPMENT PROGRESS',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: isMobile ? 22 : 32,
              fontWeight: FontWeight.bold,
              color: const Color(0xFFD2B48C),
              letterSpacing: 1.2,
            ),
          ),
          const SizedBox(height: 24),
          _buildProgressStage(
            isMobile: isMobile,
            image: 'assets/images/mvp.png',
            title: 'MVP Release',
            description: 'The MVP release was the initial implementation of our original idea to have one Gamemaster against a team of digital, real players. We used the Azure Kinect v3 to capture both depth images and colour images of the sandbox. At this stage, the depth image was used to map the topology of the sand into in-game terrain that could be used as the battlefield for the digital players. We used an open-source networking library called FishNet to network the game, however we found that sending the height data over the network was extremely inefficient.\n\nFollowing feedback from extensive user testing, we decided to pivot and take the tech we had implemented and focus in on the core mechanics that testers found fun, the sand!',
            progress: 1.0,
            swap: false,
          ),
          SizedBox(height: isMobile ? 24 : 32),
          _buildProgressStage(
            isMobile: isMobile,
            image: 'assets/images/beta.png',
            title: 'Beta Release',
            description: 'The beta release included a full shift of focus into what testers found most enjoyable - the sand itself. In order to emphasise the gameplay around shaping the terrain of the sand, the multiplayer aspect of the game was removed and replaced with enemy AI. Additionally, we implemented hand tracking to mask out players\' hands, preventing unintended terrain manipulation and ensuring smoother gameplay. Based on user feedback, we also began constructing a larger sandbox, as testers felt the original box was too small and lacked immersion. Finally, we also started creating enemy models to enhance the game’s visual appeal and overall immersion. Shaders were also implemented to further refine the game\'s look and feel in order to match our toon style aesthetic.',
            progress: 1.0,
            swap: true,
          ),
          SizedBox(height: isMobile ? 24 : 32),
          _buildProgressStage(
            isMobile: isMobile,
            image: 'assets/images/final.png',
            title: 'Final Release - Games Day',
            description: 'For the final release, we refined and expanded the game\'s core mechanics to create a more immersive and polished experience. We implemented hand reconstruction, using hand landmarks to virtually track the game master\’s hands in-game. The world now features infinite sand, extending across the entire map to create the illusion of a vast desert. We also introduced more shaders to enhance the game\'s visual style and finalised construction of the larger sandbox, improving physical immersion. The enemy system was also expanded with a new wave-based difficulty system and additional enemy types that require different strategies, such as aerial and underground enemies which must be defeated by creating mountains and digging valleys respectively. Finally, to complete the experience, we added a UI to provide clear feedback and round out the game\’s presentation.',
            progress: 1.0,
            swap: false,
          ),
        ],
      ),
    );
  }

  Widget _buildProgressStage({
    required bool isMobile,
    required String image,
    required String title,
    required String description,
    required double progress,
    required bool swap,
  }) {
    return LayoutBuilder(
      builder: (context, constraints) {
        if (isMobile) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              AspectRatio(
                aspectRatio: 16 / 9,
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: Image.asset(
                    image,
                    fit: BoxFit.cover,
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFF2A2A2A),
                  borderRadius: BorderRadius.circular(8),
                ),
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
                    const SizedBox(height: 12),
                    Text(
                      description,
                      style: const TextStyle(
                        fontSize: 14,
                        color: Colors.white70,
                        height: 1.4,
                      ),
                    ),
                    const SizedBox(height: 16),
                    LinearProgressIndicator(
                      value: progress,
                      backgroundColor: Colors.grey[800],
                      valueColor: const AlwaysStoppedAnimation<Color>(Color(0xFFD2B48C)),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      '${(progress * 100).round()}% Complete',
                      style: const TextStyle(
                        color: Colors.white70,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          );
        }

        if (swap) {
          return IntrinsicHeight(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  flex: 1,
                  child: Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: const Color(0xFF2A2A2A),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          title,
                          style: TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                        const SizedBox(height: 16),
                        Text(
                          description,
                          style: TextStyle(
                            fontSize: 16,
                            color: Colors.white70,
                            height: 1.6,
                          ),
                        ),
                        const SizedBox(height: 24),
                        LinearProgressIndicator(
                          value: progress,
                          backgroundColor: Colors.grey[800],
                          valueColor: AlwaysStoppedAnimation<Color>(Color(0xFFD2B48C)),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          '${(progress * 100).round()}% Complete',
                          style: TextStyle(
                            color: Colors.white70,
                            fontSize: 14,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(width: 24),
                Expanded(
                  flex: 1,
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: Image.asset(
                      image,
                      fit: BoxFit.cover,
                    ),
                  ),
                ),
              ],
            ),
          );
        }

        // Desktop/Tablet layout
        return IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 1,
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: Image.asset(
                    image,
                    fit: BoxFit.cover,
                  ),
                ),
              ),
              const SizedBox(width: 24),
              Expanded(
                flex: 1,
                child: Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: const Color(0xFF2A2A2A),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: TextStyle(
                          fontSize: 24,
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                        ),
                      ),
                      const SizedBox(height: 16),
                      Text(
                        description,
                        style: TextStyle(
                          fontSize: 16,
                          color: Colors.white70,
                          height: 1.6,
                        ),
                      ),
                      const SizedBox(height: 24),
                      LinearProgressIndicator(
                        value: progress,
                        backgroundColor: Colors.grey[800],
                        valueColor: AlwaysStoppedAnimation<Color>(Color(0xFFD2B48C)),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        '${(progress * 100).round()}% Complete',
                        style: TextStyle(
                          color: Colors.white70,
                          fontSize: 14,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildHeroContent(BuildContext context) {
    final isMobile = MediaQuery.of(context).size.width < 600;

    return Padding(
      padding: EdgeInsets.symmetric(horizontal: isMobile ? 16 : 24),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Text(
            'SHIFTING SANDS',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: isMobile ? 32 : 48,
              fontWeight: FontWeight.bold,
              color: Colors.white,
              letterSpacing: 2,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'A COMS30042 Team Project by Seal Team 7',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: isMobile ? 14 : 18,
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
              padding: EdgeInsets.symmetric(
                horizontal: isMobile ? 24 : 32,
                vertical: isMobile ? 12 : 16,
              ),
              textStyle: TextStyle(
                fontSize: isMobile ? 16 : 18,
                fontWeight: FontWeight.bold,
              ),
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
          AspectRatio(
            aspectRatio: 16 / 9,
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