using System;
using System.Collections.Generic;

namespace Leaderboard
{
    public class GameResult
    {
        public string PlayerName;
        public string Difficulty;
        public string Timestamp;
        public long Score;
        public long WavesCleared;
        public long TotalEnemiesDefeated;
        public long TotalDamageTaken;
        public double TimeSurvived;
        public Dictionary<string, long> EnemiesDefeated;

        public GameResult(string playerName, string difficulty, long score, long wavesCleared, long totalEnemies,
            long damageTaken, double timeSurvived, Dictionary<string, long> enemiesDefeated)
        {
            PlayerName = playerName;
            Difficulty = difficulty;
            Timestamp = DateTime.UtcNow.ToString("o");
            Score = score;
            WavesCleared = wavesCleared;
            TotalEnemiesDefeated = totalEnemies;
            TotalDamageTaken = damageTaken;
            TimeSurvived = timeSurvived;
            EnemiesDefeated = enemiesDefeated;
        }
    }
}