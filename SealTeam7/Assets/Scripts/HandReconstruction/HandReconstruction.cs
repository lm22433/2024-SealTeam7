using System.Collections;
using Map;
using UnityEngine;

public class HandReconstruction : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("Hand Calibration")]
    [SerializeField] private GameObject[] bones; //Must be length 18 (palm - 4 fingers right to left - thumb all bottom to top)
    [SerializeField] private GameObject hand;
    [SerializeField] private float _lerpFactor;
    [SerializeField] private float _thresholdDst;
    [SerializeField, Range(0,1)] private int _handNum;
    [SerializeField] private float[] positions_offset_y;

    [Header("Shader Calibration")]
    [SerializeField, Range(0,1)] private Material handMaterial;
    [SerializeField, Range(0,1)] private float maxHandSpeed;
    [SerializeField] private GameObject particleEffect;

    [Header("Hand Visibility")]
    [SerializeField] private int maxFramesWithoutHand = 10;
    [SerializeField] private float startAlpha = 0.4f;
    [SerializeField] private float fadeRate = 0.1f;
    private int nullFrameCount = 0;
    [SerializeField] private Renderer _renderer;

    private Vector3[] positions;
    Vector3 lastPosition;

    [SerializeField] private float handStartFadeSpeed;
    [SerializeField] private float handMaxFadeSpeed;
    [SerializeField] private float speedTimeMeasure;
    [SerializeField] float handSpeed = 0;
    
    private Quaternion[] _boneRotations;

    void Start()
    {
        positions = new Vector3[21];
        _boneRotations = new Quaternion[18];

        for (int i = 0; i < bones.Length; i++)
        {
            _boneRotations[i] = bones[i].transform.rotation;
        }
        
        lastPosition = Vector3.zero;

        HandSpeedTimer();

    }
    public IEnumerator HandSpeedTimer()
    {
        yield return new WaitForSeconds(speedTimeMeasure);
        
        handSpeed = Vector3.Distance(positions[0], lastPosition) / Time.deltaTime;
        lastPosition = positions[0];
        HandSpeedTimer();
    }
    

    // Update is called once per frame
    private void FixedUpdate()
    {
        //Get hand points Vector3[21]
        var tempPositions = MapManager.GetInstance().GetHandPositions(_handNum);

        if (tempPositions != null) {   
            nullFrameCount = 0;

            if (handSpeed > handStartFadeSpeed) {
                float alpha = _renderer.material.GetFloat("_TransparancyScalar");
                float fadePercent = (handSpeed - handStartFadeSpeed) / (handMaxFadeSpeed - handStartFadeSpeed) * fadeRate;
                if (alpha <= 0.005) {
                    particleEffect.SetActive(false);
                }
                
                _renderer.material.SetFloat("_TransparancyScalar", Mathf.Lerp(alpha, 0, fadePercent));

            } else {
                particleEffect.SetActive(true);
                _renderer.material.SetFloat("_TransparancyScalar", startAlpha);
            }

            for(int i = 0; i < tempPositions.Length; i++) {
                float newDst = Mathf.Abs(Vector3.Distance(tempPositions[i] + positions_offset_y[i] * -transform.up, positions[i]));

                if (newDst >= _thresholdDst)
                {
                    positions[i] = Vector3.Lerp(positions[i], tempPositions[i], _lerpFactor) + positions_offset_y[i] * -transform.up;
                }
            }
            
        } else {
            if (nullFrameCount >= maxFramesWithoutHand) {
                float alpha = _renderer.material.GetFloat("_TransparancyScalar");
                if (alpha <= 0.005) {
                    particleEffect.SetActive(false);
                    positions[0] = new Vector3(-3000, 0, -100);
                    gameObject.transform.localPosition = positions[0];
                }

                _renderer.material.SetFloat("_TransparancyScalar", Mathf.Lerp(alpha, 0, fadeRate));

                return;
            }
            nullFrameCount++;
        }
        
        //Hand direction about the Y axis
        Vector3 targetDir = positions[9] - positions[0];
        gameObject.transform.localPosition = positions[0];


        if (_handNum == 0) {
            Vector3 targetDir2 = positions[17] - positions[5];
            if (positions[17].x < positions[5].x) {
                gameObject.transform.localRotation = Quaternion.Euler(
                    gameObject.transform.localRotation.eulerAngles.x, 
                    Quaternion.LookRotation(targetDir.normalized, transform.up).eulerAngles.y, 
                    -180 + Quaternion.LookRotation(targetDir2.normalized).eulerAngles.x
                );
            } else {
                gameObject.transform.localRotation = Quaternion.Euler(
                    gameObject.transform.localRotation.eulerAngles.x, 
                    Quaternion.LookRotation(targetDir.normalized, transform.up).eulerAngles.y, 
                    -Quaternion.LookRotation(targetDir2.normalized).eulerAngles.x
                );
            }
        } else {
            Vector3 targetDir2 = positions[5] - positions[17];
            if (positions[17].x < positions[5].x) {
                gameObject.transform.localRotation = Quaternion.Euler(
                    gameObject.transform.localRotation.eulerAngles.x, 
                    Quaternion.LookRotation(targetDir.normalized, transform.up).eulerAngles.y, 
                    -Quaternion.LookRotation(targetDir2.normalized).eulerAngles.x
                );
            } else {
                gameObject.transform.localRotation = Quaternion.Euler(
                    gameObject.transform.localRotation.eulerAngles.x, 
                    Quaternion.LookRotation(targetDir.normalized, transform.up).eulerAngles.y, 
                    -180 + Quaternion.LookRotation(targetDir2.normalized).eulerAngles.x
                );
            }
        }

        //Hand direction wrist to knuckle x axis
        RotateBoneToVector(1, 0, 9);
    
        //4 finger knuckles x axis
        RotateBoneToVector(2, 17, 18);
        RotateBoneToVector(5, 13, 14);
        RotateBoneToVector(8, 9, 10);
        RotateBoneToVector(11, 5, 6);
    
        //4 finger knuckles to mid finger x axis
        RotateBoneToVector(3, 18, 19);
        RotateBoneToVector(6, 14, 15);
        RotateBoneToVector(9, 10, 11);
        RotateBoneToVector(12, 6, 7);

        //4 finger mid to end x axis
        RotateBoneToVector(4, 19, 20);
        RotateBoneToVector(7, 15, 16);
        RotateBoneToVector(10, 11, 12);
        RotateBoneToVector(13, 7, 8);

        //Thumb X axis
        RotateBoneToVector(15, 1, 2);
        RotateBoneToVector(16, 2, 3);
        RotateBoneToVector(17, 3, 4);

    }


    private void RotateBoneToVector(int bone, int start, int end) {
        Vector3 targetDir = positions[end] - positions[start];
        Quaternion newRotation = Quaternion.LookRotation(targetDir.normalized, transform.up);

        bones[bone].transform.rotation = newRotation * _boneRotations[bone];
    }

}
