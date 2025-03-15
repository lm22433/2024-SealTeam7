import 'package:firebase_database/firebase_database.dart';
import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:shifting_sands/components/navbar.dart';
import 'package:shifting_sands/models/game_result.dart';

class LeaderboardPage extends StatelessWidget {
  const LeaderboardPage({super.key});

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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
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
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Hero section with background
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
                        height: 300,
                        width: double.infinity,
                        fit: BoxFit.cover,
                      ),
                    ),
                    // Hero content
                    const Padding(
                      padding: EdgeInsets.symmetric(horizontal: 24),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Text(
                            'LEADERBOARD',
                            style: TextStyle(
                              fontSize: 48,
                              fontWeight: FontWeight.bold,
                              color: Colors.white,
                              letterSpacing: 2,
                            ),
                          ),
                          SizedBox(height: 16),
                          Text(
                            'THE BEST SHIFTING SANDS PLAYERS',
                            style: TextStyle(
                              fontSize: 18,
                              color: primaryAccent,
                              letterSpacing: 1.5,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),

                // Podium section
                Container(
                  padding: const EdgeInsets.symmetric(vertical: 64, horizontal: 24),
                  color: Colors.black87,
                  child: const Column(
                    children: [
                      Text(
                        'TOP PERFORMERS',
                        style: TextStyle(
                          fontSize: 32,
                          fontWeight: FontWeight.bold,
                          color: primaryAccent,
                          letterSpacing: 1.5,
                        ),
                      ),
                      SizedBox(height: 32),
                      // topThree.isNotEmpty ? buildPodium(topThree) : const SizedBox.shrink(),
                    ],
                  ),
                ),

                // Rankings table section
                Container(
                  padding: const EdgeInsets.symmetric(vertical: 64, horizontal: 24),
                  color: darkBackground,
                  child: Column(
                    children: [
                      const Text(
                        'ALL RANKINGS',
                        style: TextStyle(
                          fontSize: 32,
                          fontWeight: FontWeight.bold,
                          color: primaryAccent,
                          letterSpacing: 1.5,
                        ),
                      ),
                      const SizedBox(height: 32),
                      Container(
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
                        child: buildRankingsTable(gameResults),
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

                // About scoring section
                Container(
                  padding: const EdgeInsets.symmetric(vertical: 64, horizontal: 24),
                  color: Colors.black87,
                  child: Column(
                    children: [
                      const Text(
                        'HOW SCORING WORKS',
                        style: TextStyle(
                          fontSize: 32,
                          fontWeight: FontWeight.bold,
                          color: primaryAccent,
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
                                color: cardBackground,
                                borderRadius: BorderRadius.circular(8),
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
                                ],
                              ),
                            ),
                          ),
                          const SizedBox(width: 24),
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
                                icon: const Icon(FontAwesomeIcons.envelope, color: Colors.white),
                              ),
                              IconButton(
                                onPressed: () {},
                                icon: const Icon(FontAwesomeIcons.discord, color: Colors.white),
                              ),
                              IconButton(
                                onPressed: () {},
                                icon: const Icon(FontAwesomeIcons.github, color: Colors.white),
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
            ),
          );
        },
      ),
    );
  }

  // Add this helper method for scoring items
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

  // Complete the buildRankingsTable function
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
            label: Text(
              'RANK',
              style: TextStyle(
                color: primaryAccent,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          DataColumn(
            label: Text(
              'PLAYER',
              style: TextStyle(
                color: primaryAccent,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          DataColumn(
            label: Text(
              'DIFFICULTY',
              style: TextStyle(
                color: primaryAccent,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          DataColumn(
            label: Text(
              'SCORE',
              style: TextStyle(
                color: primaryAccent,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          DataColumn(
            label: Text(
              'TIME (s)',
              style: TextStyle(
                color: primaryAccent,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          DataColumn(
            label: Text(
              'WAVES',
              style: TextStyle(
                color: primaryAccent,
                fontWeight: FontWeight.bold,
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
                Container(
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
              DataCell(Row(
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
              )),
              DataCell(Container(
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
              )),
              DataCell(Text(
                game.score.toString(),
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  color: Colors.white,
                ),
              )),
              DataCell(Text(
                game.timeSurvived.toString(),
                style: TextStyle(
                  color: Colors.white.withOpacity(0.8),
                ),
              )),
              DataCell(Text(
                game.wavesCleared.toString(),
                style: TextStyle(
                  color: Colors.white.withOpacity(0.8),
                ),
              )),
            ],
          );
        }),
      ),
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
