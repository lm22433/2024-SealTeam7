using UnityEngine;

public class RotateAroundCentre : MonoBehaviour
{

    [SerializeField] private float rotationSpeed;
    // Start is called once before the first execution of Update after the MonoBehaviour is create

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
