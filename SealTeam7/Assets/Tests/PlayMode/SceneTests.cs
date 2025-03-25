using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class SceneTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup() => SceneManager.LoadScene("Assets/Scenes/TerrainTesting.unity", LoadSceneMode.Single);
        
        [UnityTest]
        public IEnumerator VerifyGameManagerExistsInScene()
        {
            GameObject gameObject = GameObject.Find("GameManager");
            Assert.That(gameObject, Is.Not.Null);
            yield return null;
        }
    
        [UnityTest]
        public IEnumerator VerifyMapManagerExistsInScene()
        {
            GameObject gameObject = GameObject.Find("MapManager");
            Assert.That(gameObject, Is.Not.Null);
            yield return null;
        }
    
        [UnityTest]
        public IEnumerator VerifyEnemyManagerExistsInScene()
        {
            GameObject gameObject = GameObject.Find("EnemyManager");
            Assert.That(gameObject, Is.Not.Null);
            yield return null;
        }
    }
}
