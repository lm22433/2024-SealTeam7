import 'package:firebase_database/firebase_database.dart';
import 'package:flutter/material.dart';
import 'package:flutter/scheduler.dart';
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
  int currentPage = 0;
  int itemsPerPage = 10;
  int previousGameResultsLength = 0;
  List<GameResult> _cachedResults = [];

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
        _cachedResults = results; // Cache the results
        return results;
      }
      _cachedResults = [];
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

  void _changePage(int newPage) {
    setState(() => currentPage = newPage);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  Widget _buildPaginationControls(int totalItems, BuildContext context) {
    final screenWidth = MediaQuery.of(context).size.width;
    final isWide = screenWidth > 600;
    int totalPages = (totalItems / itemsPerPage).ceil();
    bool isFirstPage = currentPage == 0;
    bool isLastPage = currentPage >= totalPages - 1;

    if (totalPages <= 1) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.only(top: 16.0),
      child: isWide
          ? Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            icon: Icon(
              Icons.chevron_left,
              color: isFirstPage ? Colors.grey : primaryAccent,
            ),
            onPressed: isFirstPage
                ? null
                : () => _changePage(currentPage - 1),
          ),
          ...List.generate(totalPages, (index) {
            bool isCurrent = index == currentPage;
            return GestureDetector(
              onTap: () => _changePage(index),
              child: Container(
                margin: const EdgeInsets.symmetric(horizontal: 4),
                padding: const EdgeInsets.symmetric(
                    horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: isCurrent ? primaryAccent : Colors.transparent,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(
                    color: isCurrent ? primaryAccent : Colors.grey,
                  ),
                ),
                child: Text(
                  '${index + 1}',
                  style: TextStyle(
                    color: isCurrent ? darkBackground : Colors.white,
                    fontWeight:
                    isCurrent ? FontWeight.bold : FontWeight.normal,
                  ),
                ),
              ),
            );
          }),
          IconButton(
            icon: Icon(
              Icons.chevron_right,
              color: isLastPage ? Colors.grey : primaryAccent,
            ),
            onPressed: isLastPage
                ? null
                : () => _changePage(currentPage + 1),
          ),
        ],
      )
          : Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            icon: Icon(Icons.chevron_left,
                color: isFirstPage ? Colors.grey : primaryAccent),
            onPressed: isFirstPage
                ? null
                : () => _changePage(currentPage - 1),
          ),
          Text(
            'Page ${currentPage + 1} of $totalPages',
            style: const TextStyle(color: Colors.white),
          ),
          IconButton(
            icon: Icon(Icons.chevron_right,
                color: isLastPage ? Colors.grey : primaryAccent),
            onPressed: isLastPage
                ? null
                : () => _changePage(currentPage + 1),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: const Navbar(),
      body: StreamBuilder<List<GameResult>>(
        stream: getGameResults(),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting && _cachedResults.isEmpty) {
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

          // Use cached results if available during page changes
          final gameResults = snapshot.data ?? _cachedResults;

          final int newLength = gameResults.length;
          final int newTotalPages = (newLength / itemsPerPage).ceil();
          int newCurrentPage = currentPage.clamp(0, newTotalPages > 0 ? newTotalPages - 1 : 0);

          if (previousGameResultsLength != newLength) {
            SchedulerBinding.instance.addPostFrameCallback((_) {
              if (mounted && currentPage != newCurrentPage) {
                setState(() {
                  currentPage = newCurrentPage;
                  previousGameResultsLength = newLength;
                });
              } else {
                previousGameResultsLength = newLength;
              }
            });
          }

          if (gameResults.isEmpty) {
            return const Center(
              child: Text(
                'No results available.',
                style: TextStyle(color: Colors.white70, fontSize: 18),
              ),
            );
          }

          final topThree = gameResults.take(3).toList();

          return SingleChildScrollView(
            controller: _scrollController,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                HeroWidget(
                    content: topThree.isNotEmpty
                        ? buildPodium(topThree, MediaQuery.of(context).size.width)
                        : const SizedBox.shrink()),
                Container(
                  key: _rankingsKey,
                  padding: EdgeInsets.symmetric(
                    vertical: 64,
                    horizontal: _getResponsivePadding(context),
                  ),
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
                      LayoutBuilder(
                        builder: (context, constraints) {
                          final isWideScreen = constraints.maxWidth > 900;
                          return isWideScreen
                              ? _buildWideScreenLayout(gameResults)
                              : _buildNarrowScreenLayout(gameResults);
                        },
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

  Widget buildRankingsTable(List<GameResult> gameResults) {
    // Key the table based on current page to help Flutter understand when to rebuild
    final tableKey = ValueKey('table_page_$currentPage');

    int start = currentPage * itemsPerPage;
    int end = start + itemsPerPage;
    if (end > gameResults.length) end = gameResults.length;
    List<GameResult> paginatedResults = gameResults.sublist(start, end);
    final isSmallScreen = MediaQuery.of(context).size.width < 600;

    return Column(
      children: [
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: DataTable(
            key: tableKey,
            headingRowColor:
            MaterialStateProperty.all(Colors.black.withOpacity(0.4)),
            dataRowMaxHeight: 64,
            columnSpacing: isSmallScreen ? 16 : 24,
            horizontalMargin: isSmallScreen ? 8 : 16,
            columns: _buildTableColumns(isSmallScreen),
            rows: List.generate(paginatedResults.length, (index) {
              final game = paginatedResults[index];
              final originalIndex = start + index;
              final bool isTopThree = originalIndex < 3;
              Color rowColor;

              if (originalIndex == 0) {
                rowColor = primaryAccent.withOpacity(0.15);
              } else if (originalIndex == 1) {
                rowColor = Colors.grey.shade300.withOpacity(0.1);
              } else if (originalIndex == 2) {
                rowColor = Colors.brown.shade300.withOpacity(0.1);
              } else {
                rowColor = originalIndex.isEven
                    ? Colors.black.withOpacity(0.2)
                    : Colors.transparent;
              }

              return DataRow(
                color: MaterialStateProperty.all(rowColor),
                cells: _buildTableCells(game, originalIndex, isTopThree, isSmallScreen),
              );
            }),
          ),
        ),
        _buildPaginationControls(gameResults.length, context),
      ],
    );
  }

  double _getResponsivePadding(BuildContext context) {
    final width = MediaQuery.of(context).size.width;
    if (width > 1200) {
      return 64;
    } else if (width > 768) {
      return 32;
    } else {
      return 16;
    }
  }

  Widget _buildWideScreenLayout(List<GameResult> gameResults) {
    return Row(
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
            padding: const EdgeInsets.all(8),
            child: Center(
              child: buildRankingsTable(gameResults),
            ),
          ),
        ),

        const SizedBox(width: 24),

        // Scoring system (right side - 35%)
        Expanded(
          flex: 35,
          child: Container(
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
            child: _buildScoringSystem(),
          ),
        ),
      ],
    );
  }

  Widget _buildNarrowScreenLayout(List<GameResult> gameResults) {
    return Column(
      children: [
        // Leaderboard table (top)
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
          padding: const EdgeInsets.all(8),
          child: Center(
            child: buildRankingsTable(gameResults),
          ),
        ),

        const SizedBox(height: 24),

        // Scoring system (bottom)
        Container(
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
          child: _buildScoringSystem(),
        ),
      ],
    );
  }

  Widget _buildScoringSystem() {
    return Column(
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
      ],
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

  List<DataColumn> _buildTableColumns(bool isSmallScreen) {
    final baseColumns = [
      const DataColumn(
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
      const DataColumn(
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
      const DataColumn(
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
      const DataColumn(
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
    ];

    // Add additional columns only for larger screens
    if (!isSmallScreen) {
      baseColumns.addAll([
        const DataColumn(
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
        const DataColumn(
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
        const DataColumn(
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
        const DataColumn(
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
      ]);
    }

    return baseColumns;
  }

  List<DataCell> _buildTableCells(GameResult game, int index, bool isTopThree, bool isSmallScreen) {
    final baseCells = [
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
              width: isSmallScreen ? 28 : 36,
              height: isSmallScreen ? 28 : 36,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: darkBackground,
                border: Border.all(color: _getRankColor(index).withOpacity(0.6), width: 2),
              ),
              child: Center(
                child: Text(
                  game.playerName.substring(0, 1).toUpperCase(),
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: isSmallScreen ? 12 : 14,
                  ),
                ),
              ),
            ),
            SizedBox(width: isSmallScreen ? 8 : 12),
            Text(
              game.playerName,
              style: TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w500,
                fontSize: isSmallScreen ? 12 : 14,
              ),
            ),
          ],
        ),
      )),
      DataCell(Center(
        child: Container(
          padding: EdgeInsets.symmetric(
            horizontal: isSmallScreen ? 6 : 10,
            vertical: 4,
          ),
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
              fontSize: isSmallScreen ? 10 : 12,
            ),
          ),
        ),
      )),
      DataCell(Center(
        child: Text(
          game.score.toString(),
          style: TextStyle(
            fontWeight: FontWeight.bold,
            color: Colors.white,
            fontSize: isSmallScreen ? 12 : 14,
          ),
        ),
      )),
    ];

    // Add additional cells only for larger screens
    if (!isSmallScreen) {
      baseCells.addAll([
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
      ]);
    }

    return baseCells;
  }

  Widget buildPodium(List<GameResult> topThree, double screenWidth) {
    final bool useCompactLayout = screenWidth < 768;

    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Padding(
            padding: const EdgeInsets.only(bottom: 32, top: 16),
            child: Text(
              'TOP 3',
              style: TextStyle(
                fontSize: useCompactLayout ? 28 : 36,
                fontWeight: FontWeight.bold,
                color: primaryAccent,
                letterSpacing: 2.0,
              ),
            ),
          ),
          _buildPodium(topThree),
          Padding(
            padding: EdgeInsets.only(
              top: useCompactLayout ? 24 : 40,
              bottom: useCompactLayout ? 12 : 20,
            ),
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
                  padding: EdgeInsets.symmetric(
                    horizontal: useCompactLayout ? 16 : 20,
                    vertical: useCompactLayout ? 10 : 12,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                  elevation: 4,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildPodium(List<GameResult> topThree) {
    return Row(
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
            false,
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
            false,
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
            false,
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
      bool isCompact,
      ) {
    final double avatarSize = isCompact ? 60 : 80;
    final double iconSize = isCompact ? 24 : 36;
    final double standWidth = isCompact ? 80 : 100;
    final double nameFontSize = isCompact ? 14 : 16;
    final double scoreFontSize = isCompact ? 12 : 14;
    final double rankFontSize = isCompact ? 32 : 48;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        // Trophy/medal icon
        Icon(
          trophyIcon,
          color: accentColor,
          size: iconSize,
        ),

        SizedBox(height: isCompact ? 8 : 16),

        // Player avatar
        Container(
          width: avatarSize,
          height: avatarSize,
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
                fontSize: isCompact ? 24 : 32,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ),

        SizedBox(height: isCompact ? 8 : 16),

        // Player name
        Text(
          player.playerName,
          style: TextStyle(
            color: Colors.white,
            fontSize: nameFontSize,
            fontWeight: FontWeight.bold,
          ),
          textAlign: TextAlign.center,
          overflow: TextOverflow.ellipsis,
        ),

        SizedBox(height: isCompact ? 4 : 8),

        // Player score
        Text(
          '${player.score} pts',
          style: TextStyle(
            color: accentColor,
            fontSize: scoreFontSize,
            fontWeight: FontWeight.w500,
          ),
        ),

        SizedBox(height: isCompact ? 8 : 16),

        // Podium stand
        Container(
          width: standWidth,
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
                fontSize: rankFontSize,
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