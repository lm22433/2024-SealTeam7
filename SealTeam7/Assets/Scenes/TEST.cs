using System;
using UnityEngine;
using Enemies;
using Enemies.Utils;
using UnityEngine.VFX;

public class TEST : MonoBehaviour
{
    public float speed;
    public VisualEffect deathParticles;
    public Transform model;

    private float degrees;

    private void Awake()
    {
        deathParticles.Stop();
    }

    void Update()
    {
        transform.Rotate(0, speed * Time.deltaTime, 0);
        degrees += speed * Time.deltaTime;

        if (degrees >= 360f)
        {
            model.gameObject.SetActive(false);
            deathParticles.Play();
        }
    }
}
