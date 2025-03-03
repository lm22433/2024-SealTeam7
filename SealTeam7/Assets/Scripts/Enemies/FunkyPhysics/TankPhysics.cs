using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public class TankPhysics : BasePhysics
    {
		private bool _exploded = false;

        protected override void Start()
        {
            base.Start();
            
            Rb.freezeRotation = false;
        }

		protected override void Update()
		{
			base.Update();
			if(self.IsDying() && !_exploded) {
				RaycastHit[] objs = Physics.SphereCastAll(transform.position, 50.0f, transform.forward, 1.0f);
                foreach (var item in objs)
                {
                    if(item.rigidbody != null)item.rigidbody.AddForce((item.point-transform.position + (5.0f * Vector3.up)).normalized * 25.0f, ForceMode.Impulse);
                }
                _exploded = true;
			}
		}
    }
}