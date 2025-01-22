using UnityEngine;
using UnityEngine.UI;

namespace Weapons
{
    public class CrosshairController : MonoBehaviour
    {
        private RectTransform crosshair;
        
        [SerializeField, Range(25.0f, 250.0f)] 
        private float crosshairSize = 50.0f;
        
        private void Start()
        {
            crosshair = GetComponent<RectTransform>();
        }
        
        private void Update()
        {
            crosshair.sizeDelta = new Vector2(crosshairSize, crosshairSize);
        }
    }
}