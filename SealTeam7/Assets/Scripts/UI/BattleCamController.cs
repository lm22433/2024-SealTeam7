using UnityEngine;
using UnityEngine.UI;

public class BattleCamController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("Game Objects")]
    [SerializeField] private RenderTexture[] battleCameras;
    [SerializeField] private RawImage[] cameraTiles;
    [SerializeField] private int currentMainCamera = 0;

    void Start()
    {
        Display.displays[1].Activate();
    }
}
