using UnityEngine;
using System.Collections;

namespace RuneRealm.Utils
{
    /// <summary>
    /// Visual effect for tree shaking when being chopped.
    /// </summary>
    public class TreeShakeEffect : MonoBehaviour
    {
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeIntensity = 0.15f;
        [SerializeField] private float shakeFrequency = 20f;

        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool isShaking;

        private void Awake()
        {
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
        }

        public void Shake()
        {
            if (!isShaking)
                StartCoroutine(ShakeCoroutine());
        }

        private IEnumerator ShakeCoroutine()
        {
            isShaking = true;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shakeDuration;
                float damping = 1f - t;

                float offsetX = Mathf.Sin(elapsed * shakeFrequency) * shakeIntensity * damping;
                float offsetZ = Mathf.Cos(elapsed * shakeFrequency * 0.8f) * shakeIntensity * damping * 0.5f;

                transform.localPosition = originalPosition + new Vector3(offsetX, 0, offsetZ);
                transform.localRotation = originalRotation * Quaternion.Euler(offsetZ * 5f, 0, offsetX * 5f);

                yield return null;
            }

            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            isShaking = false;
        }
    }
}
