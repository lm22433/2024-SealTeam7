using UnityEngine;

namespace Temp
{
    public class BallSpawner : MonoBehaviour
    {
        [SerializeField] private float ballRadius;
        [SerializeField] private int numberOfBalls;
        [SerializeField] private float spawnRadius;
        
        private void Start()
        {
            for (int i = 0; i < numberOfBalls; i++)
            {
                var spawnPoint = Random.insideUnitCircle * spawnRadius;
                
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<Rigidbody>();
                ball.transform.parent = transform;
                ball.transform.localPosition = new Vector3(spawnPoint.x, 0f, spawnPoint.y);
                ball.transform.localScale = new Vector3(ballRadius, ballRadius, ballRadius);
            }
        }
    }
}