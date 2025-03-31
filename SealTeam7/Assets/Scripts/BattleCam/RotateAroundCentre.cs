using UnityEngine;

public class RotateAroundCentre : MonoBehaviour
{

    [SerializeField] private float rotationSpeed;
    private void Update()
    {
        gameObject.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
