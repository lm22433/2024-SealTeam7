using Map;
using UnityEngine;

namespace Enemies.Utils
{
    public class SitOnTerrain : MonoBehaviour
    {
        private void Update()
        {
            var x = transform.position.x;
            var z = transform.position.z;

            // sit on terrain
            transform.SetPositionAndRotation(new Vector3(x, MapManager.GetInstance().GetHeight(transform.position) + transform.lossyScale.y, z), transform.rotation);
        }
    }    
}