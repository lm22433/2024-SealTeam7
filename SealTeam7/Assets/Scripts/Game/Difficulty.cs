using System;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Enemies.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game
{
    public enum DifficultyType
    {
        Tutorial,
        Easy,
        Normal,
        Hard,
        Impossible
    }
    
    [CreateAssetMenu(fileName = "New Difficulty", menuName = "Game/Difficulty")]
    public class Difficulty : ScriptableObject
    {
        public string difficultyName;
        public DifficultyType difficultyType;

        [Header("Enemy Group Scaling")] 
        public float groupSizeMultiplier;
    }
}