using System.Collections;
using UnityEngine;

namespace Effects
{
    public class DamageEffectManager : MonoBehaviour
    {
        private static readonly int VignetteRadius = Shader.PropertyToID("_VignetteRadius");

        [Header("References")]
        [SerializeField] private Material screenDamageMaterial;

        private static DamageEffectManager _instance;
        private Coroutine screenDamageCoroutine;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            screenDamageMaterial.SetFloat(VignetteRadius, 1f);
        }

        private void OnApplicationQuit()
        {
            screenDamageMaterial.SetFloat(VignetteRadius, 1f);
        }

        public void ScreenDamageEffect(float intensity)
        {
            if (screenDamageCoroutine != null) StopCoroutine(screenDamageCoroutine);
            screenDamageCoroutine = StartCoroutine(ScreenDamage(intensity));
        }

        private IEnumerator ScreenDamage(float intensity)
        {
            float targetRadius = Remap(intensity, 0, 1, 0.2f, 0.0f);
            float curRadius = 1.0f;
            for (float t = 0; curRadius != targetRadius; t += Time.deltaTime)
            {
                curRadius = Mathf.Clamp(Mathf.Lerp(1, targetRadius, t), 1, targetRadius);
                screenDamageMaterial.SetFloat(VignetteRadius, curRadius);
                yield return null;
            }
            for (float t = 0; curRadius < 1; t += Time.deltaTime)
            {
                curRadius = Mathf.Lerp(targetRadius, 1, t);
                screenDamageMaterial.SetFloat(VignetteRadius, curRadius);
                yield return null;
            }
        }

        private static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax) =>
            Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));

        public static DamageEffectManager GetInstance() => _instance;
    }
}
