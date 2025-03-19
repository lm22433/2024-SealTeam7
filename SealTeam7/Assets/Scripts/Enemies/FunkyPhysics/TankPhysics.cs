using Map;
using Game;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies.FunkyPhysics
{
    public class TankPhysics : BasePhysics
    {
		private bool _exploded;
		[SerializeField] private int lives = 1;
		[SerializeField] private float gracePeriod = 3.0f;
		private float _deathTime = 0.0f;
		[SerializeField] private VisualEffect SmokeDmg;

		protected override void Start()
		{
			base.Start();
			SmokeDmg.Stop();
		}

		protected override void Update()
		{
			if (!GameManager.GetInstance().IsGameActive()) return;

			Grounded = transform.position.y < MapManager.GetInstance().GetHeight(transform.position) + groundedOffset;
			
			_deathTime += Time.deltaTime;
            
			//WOULD DIE BURIED
			if (transform.position.y < MapManager.GetInstance().GetHeight(transform.position) - sinkFactor && !Self.IsDying && _deathTime >= gracePeriod)
			{
				if (lives <= 0)
				{
					Self.buried = Self.buriedAmount;
					Self.SetupDeath();
				}
				else
				{
					_deathTime = 0.0f;
					SmokeDmg.Play();
					transform.position = new Vector3(transform.position.x, MapManager.GetInstance().GetHeight(transform.position), transform.position.z);
					lives--;
				}
			}
			//WOULD DIE FALL DMG
			if (-Rb.linearVelocity.y >= fallDeathVelocityY && Grounded && !Self.IsDying && _deathTime >= gracePeriod)
			{
				SmokeDmg.Play();
				if (lives <= 0)Self.SetupDeath();
				else
				{
					lives--;
					_deathTime = 0.0f;
				}
			}

			EnemyUpdate();
		}

        protected override void EnemyUpdate()
        {
            if (Grounded && Vector3.Dot(transform.up, MapManager.GetInstance().GetNormal(transform.position)) < 0.5f && !Self.IsDying) Self.SetupDeath();

			if (Self.IsDying && !_exploded)
			{
				RaycastHit[] objs = Physics.SphereCastAll(transform.position, 50.0f, transform.forward, 1.0f);
                foreach (var item in objs)
                {
                    if (item.rigidbody) item.rigidbody.AddForce((item.point-transform.position + (5.0f * Vector3.up)).normalized * 25.0f, ForceMode.Impulse);
                }
                _exploded = true;
			}
		}
    }
}