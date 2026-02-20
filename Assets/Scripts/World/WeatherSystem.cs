using UnityEngine;
using RuneRealm.Core;

namespace RuneRealm.World
{
    /// <summary>
    /// Dynamic weather system creating Skyrim-style atmospheric conditions.
    /// Rain, snow, fog, and clear skies cycle naturally.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        [Header("Weather State")]
        [SerializeField] private WeatherType currentWeather = WeatherType.Clear;
        [SerializeField] private float weatherChangeInterval = 300f; // 5 minutes
        [SerializeField] private float transitionDuration = 30f;

        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private ParticleSystem fogParticles;
        [SerializeField] private ParticleSystem lightningFlash;

        [Header("Fog")]
        [SerializeField] private float clearFogDensity = 0.002f;
        [SerializeField] private float rainFogDensity = 0.008f;
        [SerializeField] private float heavyFogDensity = 0.02f;
        [SerializeField] private float snowFogDensity = 0.01f;

        [Header("Lighting")]
        [SerializeField] private Light sunLight;
        [SerializeField] private float clearLightIntensity = 1.2f;
        [SerializeField] private float overcastLightIntensity = 0.6f;
        [SerializeField] private float stormLightIntensity = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioSource ambientAudioSource;
        [SerializeField] private AudioClip rainAmbient;
        [SerializeField] private AudioClip windAmbient;
        [SerializeField] private AudioClip thunderClip;

        [Header("Wind")]
        [SerializeField] private float baseWindStrength = 0.5f;
        [SerializeField] private float stormWindStrength = 3f;
        private Vector3 windDirection;

        private float weatherTimer;
        private float transitionProgress;
        private WeatherType targetWeather;
        private bool isTransitioning;

        public WeatherType CurrentWeather => currentWeather;
        public Vector3 WindDirection => windDirection;
        public float WindStrength => GetWindStrength();

        public event System.Action<WeatherType> OnWeatherChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            windDirection = Quaternion.Euler(0, Random.Range(0, 360), 0) * Vector3.forward;
        }

        private void Update()
        {
            weatherTimer += Time.deltaTime;

            if (weatherTimer >= weatherChangeInterval)
            {
                weatherTimer = 0f;
                ChangeWeather();
            }

            if (isTransitioning)
            {
                transitionProgress += Time.deltaTime / transitionDuration;
                if (transitionProgress >= 1f)
                {
                    transitionProgress = 1f;
                    isTransitioning = false;
                    currentWeather = targetWeather;
                }
                ApplyWeatherTransition();
            }

            UpdateWindDirection();
            UpdateShaderGlobals();
        }

        private void ChangeWeather()
        {
            // Weighted random weather selection
            float roll = Random.value;
            WeatherType newWeather;

            if (roll < 0.4f) newWeather = WeatherType.Clear;
            else if (roll < 0.6f) newWeather = WeatherType.Overcast;
            else if (roll < 0.75f) newWeather = WeatherType.Rain;
            else if (roll < 0.85f) newWeather = WeatherType.HeavyRain;
            else if (roll < 0.92f) newWeather = WeatherType.Fog;
            else if (roll < 0.97f) newWeather = WeatherType.Snow;
            else newWeather = WeatherType.Storm;

            if (newWeather != currentWeather)
            {
                targetWeather = newWeather;
                isTransitioning = true;
                transitionProgress = 0f;
                OnWeatherChanged?.Invoke(newWeather);
                EventManager.Publish(GameEvents.WeatherChanged, newWeather);
            }
        }

        public void SetWeather(WeatherType weather)
        {
            targetWeather = weather;
            isTransitioning = true;
            transitionProgress = 0f;
            OnWeatherChanged?.Invoke(weather);
        }

        private void ApplyWeatherTransition()
        {
            float t = Mathf.SmoothStep(0f, 1f, transitionProgress);

            // Fog
            float targetFogDensity = GetFogDensity(targetWeather);
            float currentFogDensity = GetFogDensity(currentWeather);
            RenderSettings.fogDensity = Mathf.Lerp(currentFogDensity, targetFogDensity, t);

            // Light intensity
            float targetLight = GetLightIntensity(targetWeather);
            float currentLight = GetLightIntensity(currentWeather);
            if (sunLight != null)
                sunLight.intensity = Mathf.Lerp(currentLight, targetLight, t);

            // Particles
            UpdateParticles(t);

            // Audio
            UpdateAudio(t);
        }

        private void UpdateParticles(float t)
        {
            bool shouldRain = targetWeather == WeatherType.Rain ||
                              targetWeather == WeatherType.HeavyRain ||
                              targetWeather == WeatherType.Storm;
            bool shouldSnow = targetWeather == WeatherType.Snow;
            bool shouldFog = targetWeather == WeatherType.Fog;

            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                if (shouldRain && !rainParticles.isPlaying) rainParticles.Play();
                else if (!shouldRain && t > 0.5f && rainParticles.isPlaying) rainParticles.Stop();

                if (shouldRain)
                {
                    float rate = targetWeather == WeatherType.HeavyRain ? 2000f :
                                 targetWeather == WeatherType.Storm ? 3000f : 800f;
                    emission.rateOverTime = Mathf.Lerp(0, rate, t);
                }
            }

            if (snowParticles != null)
            {
                if (shouldSnow && !snowParticles.isPlaying) snowParticles.Play();
                else if (!shouldSnow && t > 0.5f && snowParticles.isPlaying) snowParticles.Stop();
            }

            if (fogParticles != null)
            {
                if (shouldFog && !fogParticles.isPlaying) fogParticles.Play();
                else if (!shouldFog && t > 0.5f && fogParticles.isPlaying) fogParticles.Stop();
            }
        }

        private void UpdateAudio(float t)
        {
            if (ambientAudioSource == null) return;

            bool hasRainAudio = targetWeather == WeatherType.Rain ||
                                targetWeather == WeatherType.HeavyRain ||
                                targetWeather == WeatherType.Storm;

            if (hasRainAudio && rainAmbient != null)
            {
                if (ambientAudioSource.clip != rainAmbient)
                {
                    ambientAudioSource.clip = rainAmbient;
                    ambientAudioSource.Play();
                }
                ambientAudioSource.volume = Mathf.Lerp(0, 0.6f, t);
            }
            else
            {
                ambientAudioSource.volume = Mathf.Lerp(ambientAudioSource.volume, 0, t * 2f);
            }
        }

        private void UpdateWindDirection()
        {
            float angle = Mathf.PerlinNoise(Time.time * 0.01f, 0f) * 360f;
            windDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        }

        private void UpdateShaderGlobals()
        {
            Shader.SetGlobalVector("_WindDirection", windDirection);
            Shader.SetGlobalFloat("_WindStrength", GetWindStrength());
            Shader.SetGlobalFloat("_RainIntensity",
                (currentWeather == WeatherType.Rain || currentWeather == WeatherType.HeavyRain ||
                 currentWeather == WeatherType.Storm) ? 1f : 0f);
            Shader.SetGlobalFloat("_SnowIntensity",
                currentWeather == WeatherType.Snow ? 1f : 0f);
        }

        private float GetFogDensity(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => clearFogDensity,
                WeatherType.Overcast => clearFogDensity * 1.5f,
                WeatherType.Rain => rainFogDensity,
                WeatherType.HeavyRain => rainFogDensity * 1.5f,
                WeatherType.Storm => rainFogDensity * 2f,
                WeatherType.Fog => heavyFogDensity,
                WeatherType.Snow => snowFogDensity,
                _ => clearFogDensity
            };
        }

        private float GetLightIntensity(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => clearLightIntensity,
                WeatherType.Overcast => overcastLightIntensity,
                WeatherType.Rain => overcastLightIntensity * 0.8f,
                WeatherType.HeavyRain => overcastLightIntensity * 0.6f,
                WeatherType.Storm => stormLightIntensity,
                WeatherType.Fog => overcastLightIntensity * 0.7f,
                WeatherType.Snow => overcastLightIntensity * 0.9f,
                _ => clearLightIntensity
            };
        }

        private float GetWindStrength()
        {
            return currentWeather switch
            {
                WeatherType.Clear => baseWindStrength * 0.5f,
                WeatherType.Storm => stormWindStrength,
                WeatherType.HeavyRain => stormWindStrength * 0.6f,
                WeatherType.Snow => baseWindStrength * 1.5f,
                _ => baseWindStrength
            };
        }
    }

    public enum WeatherType
    {
        Clear,
        Overcast,
        Rain,
        HeavyRain,
        Storm,
        Fog,
        Snow
    }
}
