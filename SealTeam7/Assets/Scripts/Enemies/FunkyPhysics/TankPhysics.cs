using Map;
using Game;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies.FunkyPhysics
{
    public class TankPhysics : BasePhysics
    {
		private bool _exploded;
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

			//Grounded = transform.position.y < MapManager.GetInstance().GetHeight(transform.position) + groundedOffset;
			
			_deathTime += Time.deltaTime;
            
			//WOULD DIE BURIED
			if (transform.position.y < MapManager.GetInstance().GetHeight(transform.position) - sinkFactor && !Self.IsDying && _deathTime >= gracePeriod)
			{
				SmokeDmg.Play();
				Self.SetupDeath();
			}
			//WOULD DIE FALL DMG
			if (-Rb.linearVelocity.y >= fallDeathVelocityY && Self.Grounded && !Self.IsDying && _deathTime >= gracePeriod)
			{
				SmokeDmg.Play();
				Self.SetupDeath();
			}

			EnemyUpdate();
		}

        protected override void EnemyUpdate()
        {
            if (Vector3.Dot(transform.up, MapManager.GetInstance().GetNormal(transform.position)) < 0f && Self.Grounded && !Self.IsDying) Self.SetupDeath();

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