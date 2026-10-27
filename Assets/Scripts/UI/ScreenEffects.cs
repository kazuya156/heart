using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // 背景/結末のフェードと、質問コンボの赤ビネット。Phaseと台本はFlowが管理する。
    public sealed class ScreenEffects : MonoBehaviour
    {
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField, Min(0f)] private float backgroundFadeSeconds = 0.35f;
        [SerializeField, Min(0f)] private float endingFadeSeconds = 0.35f;
        [SerializeField] private Image vignetteImage;
        [SerializeField, UnityEngine.Range(0f, 1f)] private float weakAlpha = 0.18f;
        [SerializeField, UnityEngine.Range(0f, 1f)] private float mediumAlpha = 0.32f;
        [SerializeField, UnityEngine.Range(0f, 1f)] private float strongAlpha = 0.48f;
        [SerializeField, Min(0f)] private float dummyFadeSeconds = 0.5f;
        private Coroutine routine;
        private Coroutine vignetteRoutine;
        private Texture2D vignetteTexture;
        private Sprite vignetteSprite;
        private int version;
        public bool IsBusy { get; private set; }
        public bool IsConfigured => fadeOverlay != null && fadeGroup != null;
        public float BackgroundFadeSeconds => backgroundFadeSeconds;
        public float EndingFadeSeconds => endingFadeSeconds;
        public bool HasVignette => vignetteImage != null;
        public float DummyFadeSeconds => dummyFadeSeconds;
        public float VignetteAlpha => vignetteImage == null ? 0f : vignetteImage.color.a;
        public bool IsVignetteFading => vignetteRoutine != null;
        public event Action<string> Covered;
        public event Action Completed;
        public event Action Cancelled;

        public void Configure(Image overlay, CanvasGroup group)
        {
            fadeOverlay = overlay;
            fadeGroup = group;
            HideOverlay();
        }

        public void ConfigureVignette(Image image)
        {
            vignetteImage = image;
            vignetteImage.raycastTarget = false;
            SetComboLevel(0);
        }

        public void SetComboLevel(int level)
        {
            StopVignetteFade();
            if (vignetteImage == null) return;
            EnsureVignetteSprite();
            level = Mathf.Clamp(level, 0, 3);
            SetVignetteAlpha(level == 0 ? 0f : level == 1 ? weakAlpha : level == 2 ? mediumAlpha : strongAlpha);
        }

        public void FadeOutVignette(float seconds)
        {
            StopVignetteFade();
            if (vignetteImage == null) return;
            if (!isActiveAndEnabled || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds) || VignetteAlpha <= 0f)
            { SetVignetteAlpha(0f); return; }
            vignetteRoutine = StartCoroutine(FadeVignette(seconds));
        }

        private IEnumerator FadeVignette(float seconds)
        {
            float from = VignetteAlpha;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                SetVignetteAlpha(Mathf.Lerp(from, 0f, Mathf.Clamp01(elapsed / seconds)));
            }
            SetVignetteAlpha(0f);
            vignetteRoutine = null;
        }

        private void StopVignetteFade()
        {
            if (vignetteRoutine != null) StopCoroutine(vignetteRoutine);
            vignetteRoutine = null;
        }

        private void SetVignetteAlpha(float alpha)
        {
            if (vignetteImage == null) return;
            vignetteImage.color = new Color(1f, 0f, 0f, Mathf.Clamp01(alpha));
            vignetteImage.raycastTarget = false;
            vignetteImage.enabled = alpha > 0f;
        }

        private void EnsureVignetteSprite()
        {
            if (!Application.isPlaying || vignetteImage.sprite != null) return;
            if (vignetteSprite == null)
            {
                const int side = 128;
                var pixels = new Color[side * side];
                for (int y = 0; y < side; y++)
                    for (int x = 0; x < side; x++)
                    {
                        Vector2 p = new Vector2((x + 0.5f) / side * 2f - 1f, (y + 0.5f) / side * 2f - 1f);
                        float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.15f, p.magnitude));
                        pixels[y * side + x] = new Color(1f, 1f, 1f, edge);
                    }
                vignetteTexture = new Texture2D(side, side, TextureFormat.RGBA32, false)
                { name = "TemporaryVignette", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                vignetteTexture.SetPixels(pixels);
                vignetteTexture.Apply(false, false);
                vignetteSprite = Sprite.Create(vignetteTexture, new Rect(0f, 0f, side, side), new Vector2(0.5f, 0.5f), side, 0, SpriteMeshType.FullRect);
                vignetteSprite.hideFlags = HideFlags.DontSave;
            }
            vignetteImage.sprite = vignetteSprite;
        }

        public void FadeBackground(string id, float seconds)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Background ID is empty.", nameof(id));
            Begin(Color.black, id, seconds);
        }

        public void FadeScenario(string id, bool white, float seconds)
        {
            Begin(white ? Color.white : Color.black, id, seconds);
        }

        public void PlayEndingFade(bool white, float seconds)
        {
            Begin(white ? Color.white : Color.black, white ? "TRUE_END" : "BAD_END", seconds);
        }

        private void Begin(Color color, string target, float seconds)
        {
            if (!IsConfigured || !isActiveAndEnabled || IsBusy)
                throw new InvalidOperationException("ScreenEffects must be configured, active and idle.");
            IsBusy = true;
            int request = ++version;
            fadeOverlay.color = color;
            fadeOverlay.raycastTarget = true;
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = true;
            fadeGroup.blocksRaycasts = true;
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.transform.SetAsLastSibling();
            float duration = float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0f : Mathf.Max(0f, seconds);
            if (duration == 0f)
            {
                fadeGroup.alpha = 1f;
                Covered?.Invoke(target);
                if (request == version && IsBusy) Finish();
                return;
            }
            routine = StartCoroutine(Play(target, duration, request));
        }

        private IEnumerator Play(string target, float seconds, int request)
        {
            yield return Fade(0f, 1f, seconds * 0.5f);
            if (request != version || !IsBusy) yield break;
            Covered?.Invoke(target);
            if (request != version || !IsBusy) yield break;
            // 画面側が会話パネルを手前に移動しても、フェードを最前面へ戻す。
            fadeOverlay.transform.SetAsLastSibling();
            yield return Fade(1f, 0f, seconds * 0.5f);
            if (request == version && IsBusy) Finish();
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            fadeGroup.alpha = from;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                // 最初のフレームも必ず暗幕を表示する。timeScale=0でも止まらない。
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
            }
            fadeGroup.alpha = to;
        }

        private void Finish()
        {
            routine = null;
            IsBusy = false;
            HideOverlay();
            Completed?.Invoke();
        }

        public void CancelFade()
        {
            bool wasBusy = IsBusy;
            version++;
            if (routine != null) StopCoroutine(routine);
            routine = null;
            IsBusy = false;
            HideOverlay();
            if (wasBusy) Cancelled?.Invoke();
        }

        private void HideOverlay()
        {
            if (fadeGroup != null)
            {
                fadeGroup.alpha = 0f;
                fadeGroup.interactable = false;
                fadeGroup.blocksRaycasts = false;
            }
            if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            CancelFade();
            StopVignetteFade();
            SetVignetteAlpha(0f);
        }

        private void OnDestroy()
        {
            if (vignetteImage != null && vignetteImage.sprite == vignetteSprite) vignetteImage.sprite = null;
            if (Application.isPlaying)
            {
                if (vignetteSprite != null) Destroy(vignetteSprite);
                if (vignetteTexture != null) Destroy(vignetteTexture);
            }
            else
            {
                if (vignetteSprite != null) DestroyImmediate(vignetteSprite);
                if (vignetteTexture != null) DestroyImmediate(vignetteTexture);
            }
        }
    }
}
