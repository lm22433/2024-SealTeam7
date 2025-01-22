using UnityEngine;
using UnityEngine.UI;

namespace Weapons
{
    public class CrosshairController : MonoBehaviour
    {
        [SerializeField, Range(25.0f, 250.0f)] 
        private float crosshairSize = 50.0f;
        
        private RectTransform _crosshair;
        
        private void Start()
        {
            _crosshair = GetComponent<RectTransform>();
        }
        
        private void Update()
        {
            _crosshair.sizeDelta = new Vector2(crosshairSize, crosshairSize);
        }
    }
}