using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tests.EditMode
{
    public class SceneTests
    {
        [SetUp]
        public void Setup()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TerrainTesting.unity");
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
