using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesInterrogationSetup
    {
        [MenuItem("Sea of Memories/07 - Add Questions and Endings")]
        public static void AddQuestionsAndEndings()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity") { Notice("Open Assets/Scenes/MainScene.unity first."); return; }
            var flows = new List<GameFlowController>();
            var controllers = new List<InterrogationController>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                flows.AddRange(sceneRoot.GetComponentsInChildren<GameFlowController>(true));
                controllers.AddRange(sceneRoot.GetComponentsInChildren<InterrogationController>(true));
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return; }
            if (controllers.Count != 0) { Notice("InterrogationController already exists. No duplicate was created."); return; }
            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var inventory = serializedFlow.FindProperty("inventory").objectReferenceValue as InventoryPanel;
            var dialogue = serializedFlow.FindProperty("dialogue").objectReferenceValue as DialoguePlayer;
            var input = serializedFlow.FindProperty("input").objectReferenceValue as UiInputRouter;
            var repository = serializedFlow.FindProperty("repository").objectReferenceValue as ScenarioRepository;
            var scenario = serializedFlow.FindProperty("scenario").objectReferenceValue as TextAsset;
            var catalog = serializedFlow.FindProperty("catalog").objectReferenceValue as ContentCatalog;
            var questionPanel = serializedFlow.FindProperty("questionPanel").objectReferenceValue as GameObject;
            if (inventory == null || !inventory.IsDetailsConfigured || dialogue == null || input == null
                || repository == null || scenario == null || catalog == null || questionPanel == null)
            { Notice("Apply step 06 and save MainScene before adding questions."); return; }
            Canvas canvas = questionPanel.GetComponentInParent<Canvas>(true);
            TMP_Text source = inventory.GetComponentInChildren<TMP_Text>(true);
            if (canvas == null || source == null || source.font == null)
            { Notice("Check the game Canvas and inventory TMP font."); return; }
            canvas = canvas.rootCanvas;
            if (questionPanel.transform.Find("InterrogationUI") != null || canvas.transform.Find("BadEndingPanel") != null
                || canvas.transform.Find("TrueEndingPanel") != null)
            { Notice("Existing question or ending UI was found. It will not be overwritten."); return; }
            if (!repository.Load(scenario) || !catalog.Validate()) { Notice("Check scenario/catalog errors in Console."); return; }
            string[] required = { "QUESTION_PROMPT", "QUESTION_MISSING_INFO", "QUESTION_VALID_REACTION", "BAD_END", "TRUE_END" };
            foreach (string id in required)
                if (repository.Find(id) == null) { Notice("Missing sequence: " + id); return; }
            var definitions = new ItemDefinition[6];
            for (int i = 0; i < definitions.Length; i++)
            {
                string id = "ITEM_0" + (i + 1);
                definitions[i] = catalog.GetItem(id);
                if (definitions[i] == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(definitions[i]))
                    || repository.Find("QUESTION_" + id) == null || repository.Find("REPLY_" + id) == null)
                { Notice("Missing item asset or question/reply sequence: " + id); return; }
            }
            if (!SeaOfMemoriesInterrogationChecks.CheckOrLog()) { Notice("Question rule checks failed. Check Console."); return; }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add questions and endings");
            for (int i = 0; i < definitions.Length; i++)
            {
                ItemDefinition item = definitions[i];
                Undo.RecordObject(item, "Assign question script IDs");
                item.ConfigureQuestion("QUESTION_" + item.Id, "REPLY_" + item.Id, "[PLACEHOLDER] Ask about " + item.DisplayName);
                EditorUtility.SetDirty(item);
            }
            if (!catalog.ValidateInterrogation())
            {
                Undo.RevertAllDownToGroup(group);
                Notice("Question data validation failed. Check Console. No question UI was created.");
                return;
            }
            TMP_FontAsset font = source.font;
            var controller = Undo.AddComponent<InterrogationController>(flow.gameObject);
            Image ui = Box("InterrogationUI", questionPanel.transform, Vector2.zero, Vector2.one, Color.clear);
            ui.raycastTarget = false;
            Undo.RegisterCreatedObjectUndo(ui.gameObject, "Create interrogation UI");
            Transform oldHeading = questionPanel.transform.Find("InterrogationTitle");
            if (oldHeading != null)
            {
                Undo.RecordObject(oldHeading.gameObject, "Hide preview heading");
                oldHeading.gameObject.SetActive(false);
            }
            Label("Title", ui.transform, "INTERROGATION", font, 32f,
                new Vector2(0.03f, 0.94f), new Vector2(0.47f, 0.995f));
            TMP_Text count = Label("Questions", ui.transform, "QUESTIONS 0/3", font, 28f,
                new Vector2(0.04f, 0.88f), new Vector2(0.46f, 0.94f));
            TMP_Text score = Label("Agitation", ui.transform, "AGITATION 0/100", font, 26f,
                new Vector2(0.04f, 0.81f), new Vector2(0.46f, 0.87f));
            Image bar = Box("Gauge", ui.transform, new Vector2(0.04f, 0.77f), new Vector2(0.46f, 0.80f), new Color(0.08f, 0.14f, 0.18f));
            bar.raycastTarget = false;
            Image fill = Box("Fill", bar.transform, Vector2.zero, new Vector2(0f, 1f), new Color(0.75f, 0.12f, 0.16f));
            fill.raycastTarget = false;
            TMP_Text combo = Label("Combo", ui.transform, "COMBO 0", font, 26f,
                new Vector2(0.04f, 0.70f), new Vector2(0.46f, 0.76f));
            Image guard = Box("GuardPlaceholder", ui.transform, new Vector2(0.04f, 0.36f), new Vector2(0.47f, 0.68f),
                new Color(0.06f, 0.14f, 0.18f));
            guard.raycastTarget = false;
            Label("Label", guard.transform, "GUARD\n[IMAGE PLACEHOLDER]", font, 28f,
                new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f));
            Image options = Box("QuestionOptions", ui.transform, Vector2.zero, Vector2.one, Color.clear);
            options.raycastTarget = false;
            var buttons = new Button[6];
            var labels = new TMP_Text[6];
            var icons = new Image[6];
            var markers = new TMP_Text[6];
            for (int i = 0; i < buttons.Length; i++)
            {
                float top = 0.92f - i * 0.09f;
                Image option = Box("Question_ITEM_0" + (i + 1), options.transform,
                    new Vector2(0.52f, top - 0.075f), new Vector2(0.97f, top), new Color(0.06f, 0.19f, 0.24f));
                buttons[i] = option.gameObject.AddComponent<Button>();
                buttons[i].targetGraphic = option;
                buttons[i].navigation = new Navigation { mode = Navigation.Mode.None };
                buttons[i].interactable = false;
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, controller.SelectSlot, i);
                icons[i] = Box("Icon", option.transform, new Vector2(0.02f, 0.10f), new Vector2(0.14f, 0.90f), Color.white);
                icons[i].preserveAspect = true;
                icons[i].enabled = false;
                icons[i].raycastTarget = false;
                markers[i] = Label("Marker", option.transform, "LOCK", font, 18f,
                    new Vector2(0.01f, 0.1f), new Vector2(0.15f, 0.9f));
                labels[i] = Label("Question", option.transform, "???", font, 24f,
                    new Vector2(0.17f, 0.06f), new Vector2(0.97f, 0.94f));
                labels[i].enableAutoSizing = true;
                labels[i].fontSizeMin = 16f;
                labels[i].fontSizeMax = 24f;
                labels[i].alignment = TextAlignmentOptions.MidlineLeft;
            }
            Image bad = Box("BadEndingPanel", canvas.transform, Vector2.zero, Vector2.one, Color.black);
            Undo.RegisterCreatedObjectUndo(bad.gameObject, "Create bad ending panel");
            Label("EndingTitle", bad.transform, "BAD END\n[PLACEHOLDER]", font, 60f,
                new Vector2(0.12f, 0.50f), new Vector2(0.88f, 0.82f));
            Image good = Box("TrueEndingPanel", canvas.transform, Vector2.zero, Vector2.one, Color.white);
            Undo.RegisterCreatedObjectUndo(good.gameObject, "Create true ending panel");
            TMP_Text trueTitle = Label("EndingTitle", good.transform, "TRUE END\n[PLACEHOLDER]", font, 60f,
                new Vector2(0.12f, 0.50f), new Vector2(0.88f, 0.82f));
            trueTitle.color = Color.black;
            controller.Configure(flow, catalog, dialogue, input, options.gameObject, buttons, labels, icons, markers,
                count, combo, score, fill.rectTransform);
            Undo.RecordObject(flow, "Assign interrogation references");
            flow.ConfigureInterrogation(controller, bad.gameObject, good.gameObject);
            bad.gameObject.SetActive(false);
            good.gameObject.SetActive(false);
            questionPanel.SetActive(false);
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = questionPanel;
            Notice("Question checks passed. Save MainScene (Ctrl+S), then Play. Acquire ITEM_01, ITEM_03, ITEM_05 and any fourth item. Ask the three valid items for 100 / TRUE END. Restart and ask one dummy for BAD END. Art, sound and fades are added later.");
        }

        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M07", message, "OK"); }

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

        private static TMP_Text Label(string name, Transform parent, string text, TMP_FontAsset font, float size, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            Fit(obj.GetComponent<RectTransform>(), min, max);
            TMP_Text label = obj.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.richText = false;
            label.raycastTarget = false;
            return label;
        }

        private static void Fit(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
