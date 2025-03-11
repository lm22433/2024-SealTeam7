using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "New Difficulty", menuName = "Game/Difficulty")]
    public class Difficulty : ScriptableObject
    {
        public string difficultyName;
        
        [Header("Base Values")] 
        public int baseEnemyGroupCount;
        public float baseSpawnDelay;
        public float baseWaveTimeLimit;

        [Header("Growth Multipliers")] 
        [Range(1.0f, 2.0f)] public float enemyGroupCountMultiplier;
        [Range(1.0f, 2.0f)] public float enemyGroupSizeMultiplier;
        [Range(0.0f, 1.0f)] public float spawnDelayMultiplier;
        [Range(1.0f, 2.0f)] public float waveTimeLimitMultiplier;

        public int GetWaveEnemyGroups(int wave) => Mathf.FloorToInt(baseEnemyGroupCount * Mathf.Pow(enemyGroupCountMultiplier, wave - 1));
        public float GetWaveSpawnDelay(int wave) => baseSpawnDelay * Mathf.Pow(spawnDelayMultiplier, wave - 1);
        public float GetWaveTimeLimit(int wave) => baseWaveTimeLimit * Mathf.Pow(waveTimeLimitMultiplier, wave - 1);
        
        public int GetWaveGroupSize(int baseSize, int wave) => Mathf.FloorToInt(baseSize * Mathf.Pow(enemyGroupSizeMultiplier, wave - 1));
    }
}