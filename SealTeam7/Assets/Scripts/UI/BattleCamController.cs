using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System;
using System.Collections;
using System.Collections.Generic;

public class BattleCamController : MonoBehaviour
{
    [Serializable]
    public struct BattleCamera
    {
        public GameObject cam;
        public bool isActive;
    }

    [Header("Game Objects")]
    [SerializeField] private RenderTexture[] renderTextures;
    [SerializeField] private List<BattleCamera> battleCameras = new List<BattleCamera>();
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
        //Display.displays[1].Activate();

        StartCoroutine(waitToChangeCameraPositions());
    }

    private void FixedUpdate() {
        scrollHeading();


    }

    private IEnumerator waitToChangeCameraPositions()
    {
        int wait_time = (int)UnityEngine.Random.Range(minCameraWaitTime, maxCameraWaitTime);
        yield return new WaitForSeconds (wait_time);

        swapCameraPositions();
        StartCoroutine(waitToChangeCameraPositions());
    }

    private void swapCameraPositions() {
        List<BattleCamera> subCamera = new List<BattleCamera>(battleCameras);
        
        for(int i = 0; i < renderTextures.Length; i++) {
            int index = (int)UnityEngine.Random.Range(0, renderTextures.Length);
            Debug.Log(index);

            subCamera[index].cam.GetComponent<Camera>().targetTexture = renderTextures[i];
            subCamera.RemoveAt(index);
        }


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
