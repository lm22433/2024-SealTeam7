using Map;
using Game;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies.FunkyPhysics
{
    public class TankPhysics : BasePhysics
    {
		private bool _exploded;
		private int _lives = 1;
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
            
			//WOULD DIE BURIED
			if (transform.position.y < MapManager.GetInstance().GetHeight(transform.position) - sinkFactor && !Self.IsDying)
			{
				if (_lives <= 0)
				{
					Self.buried = Self.buriedAmount;
					Self.SetupDeath();
				}
				else
				{
					SmokeDmg.Play();
					transform.position = new Vector3(transform.position.x, MapManager.GetInstance().GetHeight(transform.position), transform.position.z);
					_lives--;
				}
			}
			//WOULD DIE FALL DMG
			if (-Rb.linearVelocity.y >= fallDeathVelocityY && Grounded && !Self.IsDying)
			{
				SmokeDmg.Play();
				if (_lives <= 0)Self.SetupDeath();
				else _lives--;
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