import 'package:firebase_database/firebase_database.dart';

List<GameResult> gameResultsFromDoc(DataSnapshot dataSnapshot) {
  final data = dataSnapshot.value as Map<dynamic, dynamic>;
  return data.entries.map((entry) {
    return GameResult.fromMap(entry.key, entry.value as Map<dynamic, dynamic>);
  }).toList();
}

class GameResult {
  String id;
  String playerName;
  String difficulty;
  int score;
  int timeSurvived;
  DateTime timestamp;
  int totalDamageTaken;
  int totalEnemiesDefeated;
  int wavesCleared;
  Map<String, int> enemiesDefeated;

  GameResult({
    required this.id,
    required this.playerName,
    required this.difficulty,
    required this.score,
    required this.timeSurvived,
    required this.timestamp,
    required this.totalDamageTaken,
    required this.totalEnemiesDefeated,
    required this.wavesCleared,
    required this.enemiesDefeated,
  });

  factory GameResult.fromMap(String id, Map<dynamic, dynamic> data) {
    return GameResult(
      id: id,
      playerName: data["PlayerName"] ?? "Unknown",
      difficulty: data["Difficulty"] ?? "Unknown",
      score: data["Score"] ?? 0,
      timeSurvived: data["TimeSurvived"] ?? 0,
      timestamp: DateTime.parse(data["Timestamp"]),
      totalDamageTaken: data["TotalDamageTaken"] ?? 0,
      totalEnemiesDefeated: data["TotalEnemiesDefeated"] ?? 0,
      wavesCleared: data["WavesCleared"] ?? 0,
      enemiesDefeated: Map<String, int>.from(data["EnemiesDefeated"] ?? {}),
    );
  }
}
