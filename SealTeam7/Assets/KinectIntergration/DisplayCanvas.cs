using UnityEngine;
using UnityEngine.UI;

public class DisplayCanvas : MonoBehaviour
{
    [SerializeField] private RawImage m_RawImage;
    //Select a Texture in the Inspector to change to

    [SerializeField] private KinectAPI kinect;

    void Start()
    {
        ref Texture2D m_Texture = ref kinect.SubscribeDepthTexture();

        m_RawImage.texture = m_Texture;
    }
}
