using Enemies;
using Game;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class EnemyDataTests
    {
        [Test]
        public void TestGetDifficultyMultiplier()
        {
            float easy = EnemyData.GetDifficultyMultiplier(DifficultyType.Easy);
            float normal = EnemyData.GetDifficultyMultiplier(DifficultyType.Normal);
            float hard = EnemyData.GetDifficultyMultiplier(DifficultyType.Hard);
            float impossible = EnemyData.GetDifficultyMultiplier(DifficultyType.Impossible);
            
            Assert.AreEqual(0.8f, easy);
            Assert.AreEqual(1.0f, normal);
            Assert.AreEqual(1.2f, hard);
            Assert.AreEqual(1.5f, impossible);
        }
    }
}