using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;

using Game;

public class BattleCamController : MonoBehaviour
{
    [Serializable]
    public struct BattleCamera
    {
        public GameObject cam;
        public bool isActive;
        public int currentTexture;

        public BattleCamera(GameObject cam, bool isActive, int i) {
            this.cam = cam;
            this.isActive = isActive;
            this.currentTexture = i;
        }
    }

    [Header("Game Objects")]
    [SerializeField] private RenderTexture[] renderTextures;
    [SerializeField] private RawImage[] rawImages;
    [SerializeField] private List<BattleCamera> battleCameras = new List<BattleCamera>();
    [SerializeField] private List<GameObject> camHolderPoints = new List<GameObject>();

    [SerializeField] private RenderTexture staticRender;
    [SerializeField] private GameObject breakingNewsTransition;

    [Header("Camera Change Parameters")]
    [SerializeField] private int currentMainCamera = 0;
    [SerializeField, Range(10f, 100f)] private float minCameraWaitTime;
    [SerializeField, Range(10f, 100f)] private float maxCameraWaitTime;

    [Header("Scrolling Headlines")]
    [SerializeField] private TMP_Text scrollingHeadlines;
    [SerializeField] private float maxScroll;
    [SerializeField] private float currentScroll = 0;
    [SerializeField] private float scrollSpeed;

    private static BattleCamController _instance;

    void Awake()
    {
        if (_instance == null) _instance = this;
        else Destroy(gameObject);

        breakingNewsTransition.SetActive(true);
    }

    void Start()
    {
        for (int i = 1; i < Display.displays.Length; i++) {
            Display.displays[i].Activate();
        }

        assignCamerasToHolders();
        swapCameraPositions();
        StartCoroutine(waitToChangeCameraPositions());
    }

    public void RegisterCamHolder(GameObject cam) {
        camHolderPoints.Add(cam);
    }

    private void Update() {
        if (GameManager.GetInstance().GameActive) {
            breakingNewsTransition.SetActive(false);
        } else {
            breakingNewsTransition.SetActive(true);
        }   
    }

    private void FixedUpdate() {
        scrollHeading();

        for(int i = 0; i < battleCameras.Count; i++) {
            if (battleCameras[i].currentTexture != -1 & !battleCameras[i].isActive) {
                rawImages[i].texture = staticRender;
            }
        }
    }

    private IEnumerator waitToChangeCameraPositions()
    {
        int wait_time = (int)UnityEngine.Random.Range(minCameraWaitTime, maxCameraWaitTime);
        yield return new WaitForSeconds (wait_time);

        assignCamerasToHolders();
        swapCameraPositions();
        StartCoroutine(waitToChangeCameraPositions());
    }


    private void assignCamerasToHolders() {
        for(int i = 0; i < camHolderPoints.Count; i++) {
            if (camHolderPoints[i] == null) {
                camHolderPoints.RemoveAt(i);
            }
        }

        List<GameObject> tempHolders = new List<GameObject>(camHolderPoints);

        for(int i = 0; i < battleCameras.Count; i++) {
            if (tempHolders.Count <= 0) {
                battleCameras[i] = new BattleCamera(
                    battleCameras[i].cam,
                    false,
                    -1
                );
            } else {
                int index = (int)UnityEngine.Random.Range(0, tempHolders.Count);

                battleCameras[i] = new BattleCamera(
                    battleCameras[i].cam,
                    true,
                    -1
                );

                battleCameras[i].cam.gameObject.transform.SetParent(tempHolders[index].transform);
                battleCameras[i].cam.gameObject.transform.position = tempHolders[index].transform.position;
                battleCameras[i].cam.gameObject.transform.rotation = tempHolders[index].transform.rotation;

                tempHolders.RemoveAt(index);
            }
        }
    }

    private void swapCameraPositions() {
        List<BattleCamera> subCamera = new List<BattleCamera>();
        List<int> refIndex = new List<int>();

        for(int i = 0; i < battleCameras.Count; i++) {
            battleCameras[i].cam.GetComponent<Camera>().targetTexture = null;

            if (battleCameras[i].isActive) {
                subCamera.Add(battleCameras[i]);
                refIndex.Add(i);
            }
        }
        
        for(int i = 0; i < renderTextures.Length; i++) {
            if (subCamera.Count <= 0) {
                rawImages[i].texture = staticRender;
            } else {
                int index = (int)UnityEngine.Random.Range(0, subCamera.Count);

                subCamera[index].cam.GetComponent<Camera>().targetTexture = renderTextures[i];
                rawImages[i].texture = renderTextures[i];
                battleCameras[refIndex[index]] = new BattleCamera(
                    subCamera[index].cam,
                    subCamera[index].isActive,
                    i
                );

                subCamera.RemoveAt(index);
                
            }
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

    public static BattleCamController GetInstance() => _instance;
}
