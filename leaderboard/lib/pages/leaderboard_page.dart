import 'package:firebase_database/firebase_database.dart';
import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:shifting_sands/components/footer.dart';
import 'package:shifting_sands/components/hero.dart';
import 'package:shifting_sands/components/navbar.dart';
import 'package:shifting_sands/models/game_result.dart';

class LeaderboardPage extends StatefulWidget {
  const LeaderboardPage({super.key});

  @override
  State<LeaderboardPage> createState() => _LeaderboardPageState();
}

class _LeaderboardPageState extends State<LeaderboardPage> {
  final ScrollController _scrollController = ScrollController();
  final GlobalKey _rankingsKey = GlobalKey();

  static const Color primaryAccent = Color(0xFFD2B48C);
  static const Color darkBackground = Color(0xFF1A1A1A);
  static const Color cardBackground = Color(0xFF2A2A2A);

  Stream<List<GameResult>> getGameResults() {
    final DatabaseReference databaseReference = FirebaseDatabase.instance.ref("gameResults");
    return databaseReference.onValue.map((event) {
      final dataSnapshot = event.snapshot;
      if (dataSnapshot.value != null) {
        List<GameResult> results = gameResultsFromDoc(dataSnapshot);
        results.sort((a, b) => b.score.compareTo(a.score));
        return results;
      }
      return [];
    });
  }

  void _scrollToRankings() {
    if (_rankingsKey.currentContext != null) {
      Scrollable.ensureVisible(
        _rankingsKey.currentContext!,
        duration: const Duration(milliseconds: 800),
        curve: Curves.easeInOut,
      );
    }
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: const Navbar(),
      body: StreamBuilder<List<GameResult>>(
        stream: getGameResults(),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(
              child: CircularProgressIndicator(
                valueColor: AlwaysStoppedAnimation<Color>(primaryAccent),
              ),
            );
          }

          if (snapshot.hasError) {
            return Center(
              child: Text(
                'Error: ${snapshot.error}',
                style: const TextStyle(color: Colors.white70),
              ),
            );
          }

          final gameResults = snapshot.data ?? [];
          if (gameResults.isEmpty) {
            return const Center(
              child: Text(
                'No results available.',
                style: TextStyle(color: Colors.white70, fontSize: 18),
              ),
            );
          }

          // Get top 3 players if available
          final topThree = gameResults.take(gameResults.length >= 3 ? 3 : gameResults.length).toList();

          return SingleChildScrollView(
            controller: _scrollController,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Hero section with podium
                HeroWidget(content: topThree.isNotEmpty ? buildPodium(topThree) : const SizedBox.shrink()),

                // Combined Rankings & Scoring section - always side by side
                Container(
                  key: _rankingsKey,
                  padding: const EdgeInsets.symmetric(vertical: 64, horizontal: 24),
                  color: darkBackground,
                  child: Column(
                    children: [
                      const Text(
                        'LEADERBOARD',
                        style: TextStyle(
                          fontSize: 32,
                          fontWeight: FontWeight.bold,
                          color: primaryAccent,
                          letterSpacing: 1.5,
                        ),
                      ),
                      const SizedBox(height: 32),

                      // Fixed side-by-side layout (no responsive behavior)
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Leaderboard table (left side - 65%)
                          Expanded(
                            flex: 65,
                            child: Container(
                              decoration: BoxDecoration(
                                color: cardBackground,
                                borderRadius: BorderRadius.circular(8),
                                boxShadow: [
                                  BoxShadow(
                                    color: Colors.black.withOpacity(0.3),
                                    spreadRadius: 2,
                                    blurRadius: 10,
                                    offset: const Offset(0, 5),
                                  ),
                                ],
                              ),
                              clipBehavior: Clip.antiAlias,
                              padding: const EdgeInsets.all(8), // Added 8px padding around the table
                              child: Center( // Center the table within the card
                                child: buildRankingsTable(gameResults),
                              ),
                            ),
                          ),

                          const SizedBox(width: 24),

                          // Scoring system (right side - 35%)
                          Expanded(
                            flex: 35,
                            child: LayoutBuilder(
                                builder: (context, constraints) {
                                  return Container(
                                    padding: const EdgeInsets.all(24),
                                    decoration: BoxDecoration(
                                      color: cardBackground,
                                      borderRadius: BorderRadius.circular(8),
                                      boxShadow: [
                                        BoxShadow(
                                          color: Colors.black.withOpacity(0.3),
                                          spreadRadius: 2,
                                          blurRadius: 10,
                                          offset: const Offset(0, 5),
                                        ),
                                      ],
                                    ),
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        const Row(
                                          children: [
                                            Icon(
                                              FontAwesomeIcons.trophy,
                                              color: primaryAccent,
                                              size: 24,
                                            ),
                                            SizedBox(width: 16),
                                            Text(
                                              'Scoring System',
                                              style: TextStyle(
                                                fontSize: 24,
                                                fontWeight: FontWeight.bold,
                                                color: Colors.white,
                                              ),
                                            ),
                                          ],
                                        ),
                                        const SizedBox(height: 24),
                                        _buildScoringItem('Enemy Kills', '10-50 points per enemy based on type'),
                                        const SizedBox(height: 16),
                                        _buildScoringItem('Wave Completion', '100 points × wave number'),
                                        const SizedBox(height: 16),
                                        _buildScoringItem('Survival Time', '1 point per second survived'),
                                        const SizedBox(height: 16),
                                        _buildScoringItem('Difficulty Multiplier', 'Easy: ×1, Medium: ×1.5, Hard: ×2'),
                                        const SizedBox(height: 16),
                                        _buildScoringItem('Kill Streak', 'Bonus points for consecutive kills'),
                                        const SizedBox(height: 16),
                                        _buildScoringItem('Health Bonus', '100 points for each 10% health remaining'),
                                        // Image removed and additional scoring items added
                                        // to ensure equal height with the rankings table
                                      ],
                                    ),
                                  );
                                }
                            ),
                          ),
                        ],
                      ),

                      const SizedBox(height: 24),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const Icon(
                            Icons.info_outline,
                            color: Colors.white54,
                            size: 16,
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'Rankings are updated in real-time',
                            style: TextStyle(
                              color: Colors.white.withOpacity(0.6),
                              fontSize: 14,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                const Footer(),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildScoringItem(String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: 8,
          height: 8,
          margin: const EdgeInsets.only(top: 6, right: 12),
          decoration: const BoxDecoration(
            shape: BoxShape.circle,
            color: primaryAccent,
          ),
        ),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Colors.white,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                value,
                style: TextStyle(
                  fontSize: 14,
                  color: Colors.white.withOpacity(0.7),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget buildRankingsTable(List<GameResult> gameResults) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: DataTable(
        headingRowColor: MaterialStateProperty.all(Colors.black.withOpacity(0.4)),
        dataRowMaxHeight: 64,
        columnSpacing: 24,
        horizontalMargin: 16,
        columns: const [
          DataColumn(
            label: Center(
              child: Text(
                'RANK',
                style: TextStyle(
                  color: primaryAccent,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
          DataColumn(
            label: Center(
              child: Text(
                'PLAYER NAME',
                style: TextStyle(
                  color: primaryAccent,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
          DataColumn(
            label: Center(
              child: Text(
                'DIFFICULTY',
                style: TextStyle(
                  color: primaryAccent,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
          DataColumn(
            label: Center(
              child: Text(
                'SCORE',
                style: TextStyle(
                  color: primaryAccent,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
          DataColumn(
              label: Center(
                child: Text(
                  'ENEMIES KILLED',
                  style: TextStyle(
                    color: primaryAccent,
                    fontWeight: FontWeight.bold,
                  )
                ),
              ),
          ),
          DataColumn(
            label: Center(
              child: Text(
                'WAVES',
                style: TextStyle(
                  color: primaryAccent,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
          DataColumn(
            label: Center(
              child: Text(
                'TIME (s)',
                style: TextStyle(
                  color: primaryAccent,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
          DataColumn(
            label: Center(
              child: Text(
                'DAMAGE TAKEN',
                style: TextStyle(
                  color: primaryAccent,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
        ],
        rows: List.generate(gameResults.length, (index) {
          final game = gameResults[index];
          final bool isTopThree = index < 3;

          Color rowColor;
          if (index == 0) {
            rowColor = primaryAccent.withOpacity(0.15);
          } else if (index == 1) {
            rowColor = Colors.grey.shade300.withOpacity(0.1);
          } else if (index == 2) {
            rowColor = Colors.brown.shade300.withOpacity(0.1);
          } else {
            rowColor = index.isEven ? Colors.black.withOpacity(0.2) : Colors.transparent;
          }

          return DataRow(
            color: MaterialStateProperty.all(rowColor),
            cells: [
              DataCell(
                Center(
                  child: Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: isTopThree ? _getRankColor(index) : Colors.grey.withOpacity(0.2),
                      border: isTopThree ? Border.all(color: _getRankColor(index).withOpacity(0.6), width: 2) : null,
                    ),
                    child: Center(
                      child: Text(
                        '${index + 1}',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: isTopThree ? Colors.black : Colors.white,
                        ),
                      ),
                    ),
                  ),
                ),
              ),
              DataCell(Center(
                child: Row(
                  children: [
                    Container(
                      width: 36,
                      height: 36,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: darkBackground,
                        border: Border.all(color: _getRankColor(index).withOpacity(0.6), width: 2),
                      ),
                      child: Center(
                        child: Text(
                          game.playerName.substring(0, 1).toUpperCase(),
                          style: const TextStyle(color: Colors.white, fontSize: 14),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Text(
                      game.playerName,
                      style: const TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ],
                ),
              )),
              DataCell(Center(
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: _getDifficultyColor(game.difficulty).withOpacity(0.2),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(
                      color: _getDifficultyColor(game.difficulty).withOpacity(0.5),
                      width: 1,
                    ),
                  ),
                  child: Text(
                    game.difficulty.toUpperCase(),
                    style: TextStyle(
                      color: _getDifficultyColor(game.difficulty),
                      fontWeight: FontWeight.bold,
                      fontSize: 12,
                    ),
                  ),
                ),
              )),
              DataCell(Center(
                child: Text(
                  game.score.toString(),
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                  ),
                ),
              )),
              DataCell(Center(
                child: Text(
                  game.totalEnemiesDefeated.toString(),
                  style: TextStyle(
                    color: Colors.white.withOpacity(0.8),
                  ),
                ),
              )),
              DataCell(Center(
                child: Text(
                  game.wavesCleared.toString(),
                  style: TextStyle(
                    color: Colors.white.withOpacity(0.8),
                  ),
                ),
              )),
              DataCell(Center(
                child: Text(
                  game.timeSurvived.toString(),
                  style: TextStyle(
                    color: Colors.white.withOpacity(0.8),
                  ),
                ),
              )),
              DataCell(Center(
                child: Text(
                  game.totalDamageTaken.toString(),
                  style: TextStyle(
                    color: Colors.white.withOpacity(0.8),
                  ),
                ),
              )),
            ],
          );
        }),
      ),
    );
  }

  Widget buildPodium(List<GameResult> topThree) {
    return Column(
      children: [
        // Title at the top
        Padding(
          padding: const EdgeInsets.only(bottom: 32, top: 16),
          child: Text(
            'TOP 3',
            style: TextStyle(
              fontSize: 36,
              fontWeight: FontWeight.bold,
              color: primaryAccent,
              letterSpacing: 2.0,
            ),
          ),
        ),
        // Podium content
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            // 2nd Place - Left
            if (topThree.length >= 2)
              _buildPodiumPlayer(
                topThree[1],
                160,
                Colors.grey.shade300,
                FontAwesomeIcons.medal,
                '2',
              ),

            const SizedBox(width: 24),

            // 1st Place - Center (taller)
            if (topThree.isNotEmpty)
              _buildPodiumPlayer(
                topThree[0],
                200,
                primaryAccent,
                FontAwesomeIcons.crown,
                '1',
              ),

            const SizedBox(width: 24),

            // 3rd Place - Right
            if (topThree.length >= 3)
              _buildPodiumPlayer(
                topThree[2],
                130,
                Colors.brown.shade300,
                FontAwesomeIcons.award,
                '3',
              ),
          ],
        ),

        // Jump button at the bottom of podium
        Padding(
          padding: const EdgeInsets.only(top: 40, bottom: 20),
          child: Center(
            child: ElevatedButton.icon(
              onPressed: _scrollToRankings,
              icon: const Icon(Icons.arrow_downward, color: darkBackground),
              label: const Text(
                'VIEW ALL RANKINGS',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  color: darkBackground,
                ),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: primaryAccent,
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
                elevation: 4,
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildPodiumPlayer(
      GameResult player,
      double height,
      Color accentColor,
      IconData trophyIcon,
      String rankText,
      ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        // Trophy/medal icon
        Icon(
          trophyIcon,
          color: accentColor,
          size: 36,
        ),

        const SizedBox(height: 16),

        // Player avatar
        Container(
          width: 80,
          height: 80,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: cardBackground,
            border: Border.all(color: accentColor, width: 3),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.3),
                blurRadius: 10,
                spreadRadius: 2,
              ),
            ],
          ),
          child: Center(
            child: Text(
              player.playerName.substring(0, 1).toUpperCase(),
              style: TextStyle(
                color: Colors.white,
                fontSize: 32,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ),

        const SizedBox(height: 16),

        // Player name
        Text(
          player.playerName,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 16,
            fontWeight: FontWeight.bold,
          ),
        ),

        const SizedBox(height: 8),

        // Player score
        Text(
          '${player.score} pts',
          style: TextStyle(
            color: accentColor,
            fontSize: 14,
            fontWeight: FontWeight.w500,
          ),
        ),

        const SizedBox(height: 16),

        // Podium stand
        Container(
          width: 100,
          height: height,
          decoration: BoxDecoration(
            color: accentColor.withOpacity(0.8),
            borderRadius: const BorderRadius.only(
              topLeft: Radius.circular(8),
              topRight: Radius.circular(8),
            ),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.4),
                blurRadius: 8,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Center(
            child: Text(
              rankText,
              style: TextStyle(
                color: Colors.black,
                fontSize: 48,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ),
      ],
    );
  }

  // Helper color functions
  Color _getDifficultyColor(String difficulty) {
    switch (difficulty.toLowerCase()) {
      case 'easy':
        return Colors.green;
      case 'normal':
        return Colors.yellow;
      case 'hard':
        return Colors.orange;
      case 'impossible':
        return Colors.red;
      default:
        return Colors.purple;
    }
  }

  Color _getRankColor(int rank) {
    switch (rank) {
      case 0:
        return primaryAccent; // Gold for 1st place
      case 1:
        return Colors.grey.shade300; // Silver for 2nd place
      case 2:
        return Colors.brown.shade300; // Bronze for 3rd place
      default:
        return Colors.grey.withOpacity(0.3);
    }
  }
}