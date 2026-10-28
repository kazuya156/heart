using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeaOfMemories
{
    // BGM用2系統とSE用1系統を分離。進行や質問判定は呼出し元が担当する。
    public sealed class AudioController : MonoBehaviour
    {
        [SerializeField] private ContentCatalog catalog;
        [SerializeField] private AudioSource bgmA;
        [SerializeField] private AudioSource bgmB;
        [SerializeField] private AudioSource seSource;
        [SerializeField, UnityEngine.Range(0f, 1f)] private float bgmVolume = 0.18f;
        [SerializeField, UnityEngine.Range(0f, 1f)] private float seVolume = 0.45f;
        [SerializeField, Min(0f)] private float bgmFadeSeconds = 0.5f;
        [SerializeField, Tooltip("Temporary test tones for registered IDs without clips. Turn OFF for release.")]
        private bool useTemporaryTestTones;
        private readonly Dictionary<string, AudioClip> temporaryClips = new Dictionary<string, AudioClip>();
        private Coroutine fadeRoutine;
        private AudioSource activeBgm;
        public string CurrentBgmId { get; private set; } = "";
        public string LastSeId { get; private set; } = "";
        public int SePlayCount { get; private set; }
        public float BgmFadeSeconds => bgmFadeSeconds;
        public bool IsCrossFading => fadeRoutine != null;
        public bool IsConfigured => catalog != null && bgmA != null && bgmB != null && seSource != null
            && bgmA != bgmB && bgmA != seSource && bgmB != seSource;

        public void Configure(ContentCatalog content, AudioSource first, AudioSource second, AudioSource soundEffects,
            bool temporaryTestTones)
        {
            catalog = content;
            bgmA = first;
            bgmB = second;
            seSource = soundEffects;
            useTemporaryTestTones = temporaryTestTones;
            ConfigureSources();
        }

        private void Awake() { ConfigureSources(); }
        private void ConfigureSources()
        {
            if (!IsConfigured) return;
            ConfigureSource(bgmA, true);
            ConfigureSource(bgmB, true);
            ConfigureSource(seSource, false);
            seSource.volume = seVolume;
        }

        private static void ConfigureSource(AudioSource source, bool loop)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
        }

        public void PlayBgm(string id)
        {
            if (!CanPlay() || string.IsNullOrEmpty(id)) return;
            if (!ContentCatalog.IsBgmId(id)) { InvalidId(id); return; }
            AudioClip clip = Resolve(id);
            if (clip == null) return;
            // 同じ曲の再指定で先頭に戻さず、進行中のフェードも継続する。
            if (CurrentBgmId == id && activeBgm != null && activeBgm.clip == clip && activeBgm.isPlaying) return;
            StopBgm();
            activeBgm = bgmA;
            activeBgm.clip = clip;
            activeBgm.loop = id != "BGM_04";
            activeBgm.volume = bgmVolume;
            activeBgm.Play();
            CurrentBgmId = id;
        }

        public void CrossFadeBgm(string id, float seconds)
        {
            if (!CanPlay() || string.IsNullOrEmpty(id)) return;
            if (!ContentCatalog.IsBgmId(id)) { InvalidId(id); return; }
            AudioClip clip = Resolve(id);
            if (clip == null) return;
            if (CurrentBgmId == id && activeBgm != null && activeBgm.clip == clip && activeBgm.isPlaying) return;
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f)
            { PlayBgm(id); return; }
            CancelCrossFade();
            // 連続切替は音量が大きい側から引き継ぎ、古い補間を止める。
            AudioSource outgoing = bgmA.volume >= bgmB.volume ? bgmA : bgmB;
            AudioSource incoming = outgoing == bgmA ? bgmB : bgmA;
            incoming.Stop();
            incoming.clip = clip;
            incoming.loop = id != "BGM_04";
            incoming.volume = 0f;
            incoming.Play();
            activeBgm = incoming;
            CurrentBgmId = id;
            fadeRoutine = StartCoroutine(Fade(outgoing, incoming, seconds));
        }

        private IEnumerator Fade(AudioSource outgoing, AudioSource incoming, float seconds)
        {
            float from = outgoing.isPlaying ? outgoing.volume : 0f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                outgoing.volume = Mathf.Lerp(from, 0f, t);
                incoming.volume = Mathf.Lerp(0f, bgmVolume, t);
            }
            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0f;
            incoming.volume = bgmVolume;
            fadeRoutine = null;
        }

        // Fade both sources, including an interrupted cross-fade, before enabling name entry.
        public void FadeOutBgm(float seconds, System.Action completed)
        {
            if (!CanPlay()) { StopBgm(); completed?.Invoke(); return; }
            CancelCrossFade();
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f)
            { StopBgm(); completed?.Invoke(); return; }
            fadeRoutine = StartCoroutine(FadeToSilence(seconds, completed));
        }

        private IEnumerator FadeToSilence(float seconds, System.Action completed)
        {
            float first = bgmA.volume, second = bgmB.volume, elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                float remaining = 1f - Mathf.Clamp01(elapsed / seconds);
                bgmA.volume = first * remaining;
                bgmB.volume = second * remaining;
            }
            bgmA.Stop(); bgmA.clip = null; bgmA.volume = 0f;
            bgmB.Stop(); bgmB.clip = null; bgmB.volume = 0f;
            activeBgm = null;
            CurrentBgmId = "";
            fadeRoutine = null;
            completed?.Invoke();
        }

        public void PlaySe(string id)
        {
            if (!CanPlay() || string.IsNullOrEmpty(id)) return;
            if (!ContentCatalog.IsSeId(id)) { InvalidId(id); return; }
            AudioClip clip = Resolve(id);
            if (clip == null) return;
            seSource.volume = seVolume;
            seSource.PlayOneShot(clip);
            LastSeId = id;
            SePlayCount++;
        }

        public void StopAll()
        {
            StopBgm();
            if (seSource != null) seSource.Stop();
        }

        private void StopBgm()
        {
            CancelCrossFade();
            if (bgmA != null) { bgmA.Stop(); bgmA.clip = null; bgmA.volume = 0f; }
            if (bgmB != null) { bgmB.Stop(); bgmB.clip = null; bgmB.volume = 0f; }
            activeBgm = null;
            CurrentBgmId = "";
        }

        private void CancelCrossFade()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        private bool CanPlay() { return Application.isPlaying && isActiveAndEnabled && IsConfigured; }
        private AudioClip Resolve(string id)
        {
            if (!catalog.HasAudioId(id)) { InvalidId(id); return null; }
            AudioClip clip = catalog.GetAudio(id);
            if (clip != null || !useTemporaryTestTones) return clip;
            if (temporaryClips.TryGetValue(id, out clip)) return clip;
            // 本番音楽ではなく再生箇所を確認するための正弦波。実行中だけ保持する。
            bool bgm = ContentCatalog.IsBgmId(id);
            int number = id[id.Length - 1] - '0';
            const int rate = 22050;
            int samples = Mathf.RoundToInt(rate * (bgm ? 2f : 0.10f + number * 0.015f));
            var data = new float[samples];
            float frequency = bgm ? 110f + 22f * number : 360f + 65f * number;
            for (int i = 0; i < samples; i++)
            {
                float time = i / (float)rate;
                float envelope = Mathf.Min(1f, i / (rate * 0.01f))
                    * Mathf.Min(1f, (samples - 1 - i) / (rate * 0.02f));
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * (bgm ? 0.22f : 0.35f);
            }
            clip = AudioClip.Create("TEMP_TEST_" + id, samples, 1, rate, false);
            clip.SetData(data, 0);
            temporaryClips.Add(id, clip);
            return clip;
        }

        private void InvalidId(string id) { Debug.LogError("[SeaOfMemories] Unknown/retired audio ID or wrong audio type: " + id, this); }
        private void OnDisable() { StopAll(); }
        private void OnDestroy()
        {
            StopAll();
            foreach (AudioClip clip in temporaryClips.Values) if (clip != null) Destroy(clip);
            temporaryClips.Clear();
        }
    }
}
