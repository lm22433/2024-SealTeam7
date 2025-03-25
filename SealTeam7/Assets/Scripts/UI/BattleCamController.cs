using UnityEngine;
using UnityEngine.UI;
using TMPro;

using battleCam {
    [Serializable]
    public struct BattleCamera
    {
        public GameObject cam;
        public bool isActive;
        public RenderTexture[] battleCameras;
    }

    public class BattleCamController : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        [Header("Game Objects")]
        [SerializeField] private BattleCamera[] battleCameras;
        [SerializeField] private RawImage[] cameraTiles;
        [SerializeField] private int currentMainCamera = 0;

        [SerializeField, Range(10f, 100f)] private float minCameraWaitTime;
        [SerializeField, Range(10f, 100f)] private float maxCameraWaitTime;

        [Header("Scrolling Headlines")]
        [SerializeField] private TMP_Text scrollingHeadlines;
        [SerializeField] private float maxScroll;
        [SerializeField] private float currentScroll = 0;
        [SerializeField] private float scrollSpeed;

        void Start()
        {
            Display.displays[1].Activate();

            waitToChangeCameraPositions()
        }

        private void FixedUpdate() {
            scrollHeading();


        }

        private IEnumerator waitToChangeCameraPositions()
        {
            int wait_time = Random.Range(minCameraWaitTime, maxCameraWaitTime);
            yield return new WaitForSeconds (wait_time);
            swapCameraPositions();
        }

        private swapCameraPositions() {
            Array.Sort(battleCameras, (x, y) => x.isActive.CompareTo(y.isActive));
            
            int wait_time = Random.Range(0, );


        }

        private void scrollHeading() {
            currentScroll += scrollSpeed;
            scrollingHeadlines.gameObject.transform.Translate(new Vector3(-scrollSpeed, 0, 0));

            if (currentScroll >= maxScroll) {
                scrollingHeadlines.gameObject.transform.Translate(new Vector3(maxScroll, 0, 0));
                currentScroll = 0;
            }
        }
    }
}
