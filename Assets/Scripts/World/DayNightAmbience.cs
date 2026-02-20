using UnityEngine;
using RuneRealm.Core;

namespace RuneRealm.World
{
    /// <summary>
    /// Controls ambient environment effects based on time of day.
    /// Fireflies at night, bird sounds at dawn, cricket sounds at dusk, etc.
    /// Creates the immersive Skyrim atmosphere.
    /// </summary>
    public class DayNightAmbience : MonoBehaviour
    {
        [Header("Night Effects")]
        [SerializeField] private ParticleSystem fireflyParticles;
        [SerializeField] private Light[] torchLights;
        [SerializeField] private float torchFlickerSpeed = 10f;
        [SerializeField] private float torchFlickerAmount = 0.2f;

        [Header("Audio")]
        [SerializeField] private AudioSource ambientAudioSource;
        [SerializeField] private AudioClip dayAmbient;     // Birds, wind
        [SerializeField] private AudioClip nightAmbient;    // Crickets, owls
        [SerializeField] private AudioClip dawnAmbient;     // Dawn chorus
        [SerializeField] private float ambientCrossfade = 5f;

        [Header("Stars")]
        [SerializeField] private ParticleSystem starParticles;
        [SerializeField] private float starFadeInTime = 0.78f;
        [SerializeField] private float starFadeOutTime = 0.25f;

        private enum TimePhase { Day, Dawn, Dusk, Night }
        private TimePhase currentPhase = TimePhase.Day;

        private void Update()
        {
            if (GameManager.Instance == null) return;

            float time = GameManager.Instance.TimeOfDay;
            TimePhase newPhase = GetTimePhase(time);

            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChanged(newPhase);
            }

            UpdateTorchFlicker();
            UpdateStars(time);
        }

        private TimePhase GetTimePhase(float time)
        {
            if (time >= 0.22f && time < 0.28f) return TimePhase.Dawn;
            if (time >= 0.28f && time < 0.72f) return TimePhase.Day;
            if (time >= 0.72f && time < 0.80f) return TimePhase.Dusk;
            return TimePhase.Night;
        }

        private void OnPhaseChanged(TimePhase phase)
        {
            switch (phase)
            {
                case TimePhase.Night:
                    if (fireflyParticles != null) fireflyParticles.Play();
                    SetTorchesActive(true);
                    TransitionAmbientAudio(nightAmbient);
                    break;

                case TimePhase.Dawn:
                    if (fireflyParticles != null) fireflyParticles.Stop();
                    TransitionAmbientAudio(dawnAmbient);
                    break;

                case TimePhase.Day:
                    SetTorchesActive(false);
                    TransitionAmbientAudio(dayAmbient);
                    break;

                case TimePhase.Dusk:
                    SetTorchesActive(true);
                    TransitionAmbientAudio(nightAmbient);
                    break;
            }
        }

        private void SetTorchesActive(bool active)
        {
            if (torchLights == null) return;
            foreach (var torch in torchLights)
            {
                if (torch != null)
                    torch.enabled = active;
            }
        }

        private void UpdateTorchFlicker()
        {
            if (torchLights == null) return;

            foreach (var torch in torchLights)
            {
                if (torch != null && torch.enabled)
                {
                    float noise = Mathf.PerlinNoise(Time.time * torchFlickerSpeed + torch.GetInstanceID(), 0f);
                    torch.intensity = 1f + (noise - 0.5f) * torchFlickerAmount * 2f;
                    torch.color = Color.Lerp(
                        new Color(1f, 0.6f, 0.2f),
                        new Color(1f, 0.8f, 0.4f),
                        noise
                    );
                }
            }
        }

        private void UpdateStars(float time)
        {
            if (starParticles == null) return;

            bool shouldShowStars = time > starFadeInTime || time < starFadeOutTime;
            var emission = starParticles.emission;

            if (shouldShowStars && !starParticles.isPlaying)
                starParticles.Play();
            else if (!shouldShowStars && starParticles.isPlaying)
                starParticles.Stop();
        }

        private void TransitionAmbientAudio(AudioClip clip)
        {
            if (ambientAudioSource == null || clip == null) return;
            if (ambientAudioSource.clip == clip) return;

            // Simple crossfade
            ambientAudioSource.clip = clip;
            ambientAudioSource.Play();
        }
    }
}
