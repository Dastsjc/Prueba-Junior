using System.Collections;
using UnityEngine;

namespace Buscaminas.Gameplay
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Tracks")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameMusic;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 1.5f;
        [SerializeField] private float musicVolume = 0.5f;

        private AudioSource audioSourceA;
        private AudioSource audioSourceB;
        private AudioSource activeSource;
        private Coroutine fadeCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSourceA = gameObject.AddComponent<AudioSource>();
            audioSourceB = gameObject.AddComponent<AudioSource>();

            ConfigureSource(audioSourceA);
            ConfigureSource(audioSourceB);

            activeSource = audioSourceA;

            if (menuMusic != null)
            {
                activeSource.clip = menuMusic;
                activeSource.volume = musicVolume;
                activeSource.Play();
            }
        }

        private void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
        }

        public void PlayMenuMusic()
        {
            Crossfade(menuMusic);
        }

        public void PlayGameMusic()
        {
            Crossfade(gameMusic);
        }

        public void Crossfade(AudioClip newClip)
        {
            if (newClip == null) return;
            if (activeSource.clip == newClip && activeSource.isPlaying) return;

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(CrossfadeRoutine(newClip));
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip)
        {
            AudioSource incoming = (activeSource == audioSourceA) ? audioSourceB : audioSourceA;
            incoming.clip = newClip;
            incoming.volume = 0f;
            incoming.Play();

            float elapsed = 0f;
            float startVolume = activeSource.volume;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
                incoming.volume = Mathf.Lerp(0f, musicVolume, t);
                yield return null;
            }

            activeSource.Stop();
            activeSource.volume = 0f;
            incoming.volume = musicVolume;
            activeSource = incoming;
            fadeCoroutine = null;
        }
    }
}
