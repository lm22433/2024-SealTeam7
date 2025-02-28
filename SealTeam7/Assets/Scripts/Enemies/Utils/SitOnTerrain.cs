using Map;
using UnityEngine;

namespace Enemies.Utils
{
    public class SitOnTerrain : MonoBehaviour
    {
        private MapManager _mapManager;
    
        private void Start()
        {
            _mapManager = FindFirstObjectByType<MapManager>();
        }
    
        private void Update()
        {
            var x = transform.position.x;
            var z = transform.position.z;

            // sit on terrain
            transform.SetPositionAndRotation(new Vector3(x, _mapManager.GetHeight(x, z) + transform.lossyScale.y, z), transform.rotation);
        }
    }    
}