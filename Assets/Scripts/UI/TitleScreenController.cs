using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeaOfMemories
{
    public sealed class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        public void Configure(GameFlowController gameFlow) { flow = gameFlow; }
        private Image titleLogo;

        public void ConfigureArtwork(Transform titlePanel, ContentCatalog catalog)
        {
            if (titlePanel == null) return;
            Sprite sprite = catalog != null ? catalog.GetSprite("BG_01") : null;
            if (titleLogo == null)
            {
                var rect = new GameObject("TitleLogo", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                rect.SetParent(titlePanel, false);
                // Keep clear of START / QUIT while preserving the logo's aspect ratio and alpha.
                rect.anchorMin = new Vector2(0.12f, 0.44f);
                rect.anchorMax = new Vector2(0.88f, 0.86f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                titleLogo = rect.GetComponent<Image>();
                titleLogo.type = Image.Type.Simple;
                titleLogo.preserveAspect = true;
                titleLogo.raycastTarget = false;
            }
            titleLogo.sprite = sprite;
            titleLogo.color = Color.white;
            titleLogo.gameObject.SetActive(sprite != null);
            foreach (TMP_Text label in titlePanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.name == "GameTitle") label.gameObject.SetActive(sprite == null);
                else if (label.name == "ProjectLabel" || label.name == "Milestone") label.gameObject.SetActive(false);
            }
        }

        public void OnStartPressed() { flow.StartNewGame(); }
        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            Debug.Log("[SeaOfMemories] Quit is tested in a Windows build. Stop Play mode to exit the Editor test.");
#else
            Application.Quit();
#endif
        }
    }
}
