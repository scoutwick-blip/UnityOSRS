using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RuneRealm.Core;

namespace RuneRealm.Audio
{
    /// <summary>
    /// Central audio manager handling music, ambient sounds, and SFX.
    /// Creates Skyrim's immersive audio layering with OSRS sound cues.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Music")]
        [SerializeField] private AudioClip mainTheme;
        [SerializeField] private AudioClip[] explorationTracks;
        [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private float musicCrossfadeDuration = 3f;

        [Header("Ambient")]
        [SerializeField] private AudioClip forestAmbient;
        [SerializeField] private AudioClip mountainAmbient;
        [SerializeField] private AudioClip coastAmbient;
        [SerializeField] private AudioClip nightAmbient;
        [SerializeField] private float ambientVolume = 0.4f;

        [Header("SFX")]
        [SerializeField] private AudioClip levelUpFanfare;
        [SerializeField] private AudioClip itemPickup;
        [SerializeField] private AudioClip menuOpen;
        [SerializeField] private AudioClip menuClose;
        [SerializeField] private AudioClip skillComplete;
        [SerializeField] private AudioClip questComplete;
        [SerializeField] private AudioClip inventoryFull;
        [SerializeField] private AudioClip buttonClick;

        private Coroutine musicFadeCoroutine;
        private Queue<AudioClip> musicQueue = new Queue<AudioClip>();
        private bool isFading;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupAudioSources();
        }

        private void Start()
        {
            SubscribeToEvents();

            if (mainTheme != null)
                PlayMusic(mainTheme);
        }

        private void SetupAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = musicVolume;
            }

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
                ambientSource.volume = ambientVolume;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
                uiSource.spatialBlend = 0f; // 2D for UI
            }
        }

        private void SubscribeToEvents()
        {
            EventManager.Subscribe<Skills.SkillType>(GameEvents.SkillLevelUp, _ => PlaySFX(levelUpFanfare));
            EventManager.Subscribe<Inventory.ItemData>(GameEvents.ItemAdded, _ => PlaySFX(itemPickup));
            EventManager.Subscribe(GameEvents.InventoryFull, () => PlaySFX(inventoryFull));
            EventManager.Subscribe<string>(GameEvents.QuestCompleted, _ => PlaySFX(questComplete));
        }

        public void PlayMusic(AudioClip clip, bool crossfade = true)
        {
            if (clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            if (crossfade && musicSource.isPlaying)
            {
                if (musicFadeCoroutine != null)
                    StopCoroutine(musicFadeCoroutine);
                musicFadeCoroutine = StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            isFading = true;

            // Fade out current
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < musicCrossfadeDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (musicCrossfadeDuration / 2f));
                yield return null;
            }

            // Switch track
            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in new
            elapsed = 0f;
            while (elapsed < musicCrossfadeDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / (musicCrossfadeDuration / 2f));
                yield return null;
            }

            musicSource.volume = musicVolume;
            isFading = false;
        }

        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null || ambientSource.clip == clip) return;
            ambientSource.clip = clip;
            ambientSource.volume = ambientVolume;
            ambientSource.Play();
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volumeScale);
        }

        public void PlayUISFX(AudioClip clip)
        {
            if (clip == null || uiSource == null) return;
            uiSource.PlayOneShot(clip);
        }

        public void PlayMenuOpen()
        {
            PlayUISFX(menuOpen);
        }

        public void PlayMenuClose()
        {
            PlayUISFX(menuClose);
        }

        public void PlayButtonClick()
        {
            PlayUISFX(buttonClick);
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (!isFading)
                musicSource.volume = musicVolume;
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            ambientSource.volume = ambientVolume;
        }

        public void SetSFXVolume(float volume)
        {
            sfxSource.volume = Mathf.Clamp01(volume);
        }

        public void PlayRandomExplorationTrack()
        {
            if (explorationTracks == null || explorationTracks.Length == 0) return;
            AudioClip track = explorationTracks[Random.Range(0, explorationTracks.Length)];
            PlayMusic(track);
        }
    }
}
