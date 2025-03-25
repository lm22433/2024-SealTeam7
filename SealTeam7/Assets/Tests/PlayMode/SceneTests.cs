using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.PlayMode
{
    public class SceneTests
    {
        [SetUp]
        public void Setup()
        {
            SceneManager.LoadScene("Assets/Scenes/TerrainTesting.unity");
        }
    
        [Test]
        public void VerifyGameManagerExistsInScene()
        {
            GameObject gameObject = GameObject.Find("GameManager");
            Assert.That(gameObject, Is.Not.Null);
        }
    
        [Test]
        public void VerifyMapManagerExistsInScene()
        {
            GameObject gameObject = GameObject.Find("MapManager");
            Assert.That(gameObject, Is.Not.Null);
        }
    
        [Test]
        public void VerifyEnemyManagerExistsInScene()
        {
            GameObject gameObject = GameObject.Find("EnemyManager");
            Assert.That(gameObject, Is.Not.Null);
        }
    }
}
