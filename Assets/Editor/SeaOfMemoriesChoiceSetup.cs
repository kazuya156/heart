using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    // 既存MainSceneのオブジェクトを残したまま、選択肢UIと参照だけを追加する。
    public static class SeaOfMemoriesChoiceSetup
    {
        [MenuItem("Sea of Memories/02 - Add Intro Choices")]
        public static void AddIntroChoices()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity")
            {
                EditorUtility.DisplayDialog("Open MainScene",
                    "Open Assets/Scenes/MainScene.unity, then run this menu again. The tool does not replace your scene.", "OK");
                return;
            }

            List<DialoguePlayer> players = FindInScene<DialoguePlayer>(scene);
            List<UiInputRouter> routers = FindInScene<UiInputRouter>(scene);
            if (players.Count != 1 || routers.Count != 1)
            {
                EditorUtility.DisplayDialog("Check GameSystems",
                    "MainScene needs exactly one DialoguePlayer and one UiInputRouter. No changes were made.", "OK");
                return;
            }

            var serializedPlayer = new SerializedObject(players[0]);
            var existing = serializedPlayer.FindProperty("choicePanel").objectReferenceValue as ChoicePanel;
            if (existing != null)
            {
                EditorUtility.DisplayDialog("ChoicePanel already assigned",
                    existing.IsConfigured
                        ? "Choice UI is already assigned. Save the scene and test both responses in Play mode."
                        : "The assigned ChoicePanel is incomplete. Check its container and button template in the Inspector. No changes were made.", "OK");
                return;
            }
            var body = serializedPlayer.FindProperty("bodyText").objectReferenceValue as TMP_Text;
            Canvas canvas = body == null ? null : body.GetComponentInParent<Canvas>(true);
            if (canvas == null || body.font == null)
            {
                EditorUtility.DisplayDialog("Check DialoguePlayer",
                    "Assign Body Text under the game Canvas and a TMP font before running this menu. No changes were made.", "OK");
                return;
            }
            canvas = canvas.rootCanvas;
            if (FindInScene<ChoicePanel>(scene).Count != 0 || canvas.transform.Find("ChoicePanel") != null)
            {
                EditorUtility.DisplayDialog("ChoicePanel already exists",
                    "An existing ChoicePanel was found but is not assigned to DialoguePlayer. Check its references; the tool will not overwrite it.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Sea of Memories choice UI");
            Undo.RecordObject(players[0], "Assign choice UI");
            ChoicePanel panel = CreateAndBind(canvas.transform, players[0], routers[0]);
            Undo.RegisterCreatedObjectUndo(panel.gameObject, "Create ChoicePanel");
            EditorUtility.SetDirty(players[0]);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = panel.gameObject;
            EditorUtility.DisplayDialog("Choice UI ready",
                "Save MainScene (Ctrl+S). Press Play > START, read the first line, then choose a response with the mouse. Both responses lead to ROOM.", "OK");
        }

        public static ChoicePanel CreateAndBind(Transform canvas, DialoguePlayer player, UiInputRouter router)
        {
            var serializedPlayer = new SerializedObject(player);
            var body = serializedPlayer.FindProperty("bodyText").objectReferenceValue as TMP_Text;
            TMP_FontAsset font = body != null && body.font != null ? body.font : TMP_Settings.defaultFontAsset;

            Image overlay = Box("ChoicePanel", canvas, Vector2.zero, Vector2.one,
                new Color(0.01f, 0.025f, 0.04f, 0.82f));
            ChoicePanel panel = overlay.gameObject.AddComponent<ChoicePanel>();
            Text("ChoiceHeading", overlay.transform, "CHOOSE A RESPONSE", font, 27f,
                new Vector2(0.25f, 0.65f), new Vector2(0.75f, 0.72f));

            var container = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup));
            container.transform.SetParent(overlay.transform, false);
            Fit(container.GetComponent<RectTransform>(), new Vector2(0.26f, 0.36f), new Vector2(0.74f, 0.64f));
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Image templateImage = Box("OptionTemplate", container.transform, Vector2.zero, Vector2.one,
                new Color(0.08f, 0.24f, 0.29f));
            Button template = templateImage.gameObject.AddComponent<Button>();
            template.targetGraphic = templateImage;
            template.navigation = new Navigation { mode = Navigation.Mode.None };
            LayoutElement element = templateImage.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 84f;
            element.preferredHeight = 84f;
            TMP_Text label = Text("Label", template.transform, "OPTION", font, 30f, Vector2.zero, Vector2.one);
            label.margin = new Vector4(24f, 10f, 24f, 10f);
            template.gameObject.SetActive(false);

            panel.Configure(container.GetComponent<RectTransform>(), template);
            player.ConfigureChoices(panel, router);
            overlay.gameObject.SetActive(false);
            return panel;
        }

        private static List<T> FindInScene<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }

        private static void Fit(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image Box(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            Fit(obj.GetComponent<RectTransform>(), min, max);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static TMP_Text Text(string name, Transform parent, string value, TMP_FontAsset font,
            float size, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            Fit(obj.GetComponent<RectTransform>(), min, max);
            TMP_Text label = obj.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.richText = false;
            label.raycastTarget = false;
            return label;
        }
    }
}
