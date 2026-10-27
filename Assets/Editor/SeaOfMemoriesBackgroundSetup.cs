using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesBackgroundSetup
    {
        [MenuItem("Sea of Memories/12 - Add Backgrounds")]
        public static void AddBackgrounds()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity") { Notice("Open Assets/Scenes/MainScene.unity first."); return; }
            var flows = new List<GameFlowController>();
            var backgrounds = new List<BackgroundView>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                flows.AddRange(sceneRoot.GetComponentsInChildren<GameFlowController>(true));
                backgrounds.AddRange(sceneRoot.GetComponentsInChildren<BackgroundView>(true));
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return; }
            if (backgrounds.Count != 0) { Notice("BackgroundView already exists. No duplicate was created."); return; }
            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var catalog = serializedFlow.FindProperty("catalog").objectReferenceValue as ContentCatalog;
            var repository = serializedFlow.FindProperty("repository").objectReferenceValue as ScenarioRepository;
            var scenario = serializedFlow.FindProperty("scenario").objectReferenceValue as TextAsset;
            var audio = serializedFlow.FindProperty("audioController").objectReferenceValue as AudioController;
            var effects = serializedFlow.FindProperty("effects").objectReferenceValue as ScreenEffects;
            var title = serializedFlow.FindProperty("titlePanel").objectReferenceValue as GameObject;
            var room = serializedFlow.FindProperty("roomPanel").objectReferenceValue as GameObject;
            var closeup = serializedFlow.FindProperty("closeupPanel").objectReferenceValue as GameObject;
            var question = serializedFlow.FindProperty("questionPanel").objectReferenceValue as GameObject;
            var bad = serializedFlow.FindProperty("badEndingPanel").objectReferenceValue as GameObject;
            var good = serializedFlow.FindProperty("trueEndingPanel").objectReferenceValue as GameObject;
            if (catalog == null || repository == null || scenario == null || audio == null || !audio.IsConfigured
                || effects == null || !effects.IsConfigured || title == null || room == null || closeup == null
                || question == null || bad == null || good == null
                || serializedFlow.FindProperty("backgroundView").objectReferenceValue != null)
            { Notice("Apply step 11, save MainScene, and check its references first."); return; }
            Canvas canvas = room.GetComponentInParent<Canvas>(true);
            if (canvas == null) { Notice("Room Panel must be under the game Canvas."); return; }
            canvas = canvas.rootCanvas;
            Transform backgroundRoot = canvas.transform.Find("BackgroundImage");
            Image matte = backgroundRoot == null ? null : backgroundRoot.GetComponent<Image>();
            TMP_Text source = title.GetComponentInChildren<TMP_Text>(true);
            if (matte == null || source == null || source.font == null)
            { Notice("Check BackgroundImage and the existing TMP font."); return; }
            if (matte.sprite != null || backgroundRoot.Find("Artwork") != null || backgroundRoot.Find("BackgroundFallback") != null)
            { Notice("Existing background artwork was found. It will not be overwritten."); return; }
            Image[] oldBackgrounds = { closeup.GetComponent<Image>(), question.GetComponent<Image>(), bad.GetComponent<Image>(), good.GetComponent<Image>() };
            foreach (Image image in oldBackgrounds)
                if (image == null || image.sprite != null)
                { Notice("Check the four existing placeholder panel Images. Assigned artwork will not be overwritten."); return; }
            if (!repository.Load(scenario) || !catalog.Validate() || !catalog.ValidatePortraits() || !catalog.ValidateAudio())
            { Notice("Check Console for scenario/catalog errors."); return; }
            var serializedCatalog = new SerializedObject(catalog);
            SerializedProperty sprites = serializedCatalog.FindProperty("sprites");
            var entries = new List<SpriteCatalogEntry>();
            var ids = new HashSet<string>();
            for (int i = 0; i < sprites.arraySize; i++)
            {
                SerializedProperty row = sprites.GetArrayElementAtIndex(i);
                string id = row.FindPropertyRelative("id").stringValue;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(ContentCatalog.NormalizeExpressionId(id)))
                { Notice("Existing sprite IDs are empty or duplicated. Fix them before continuing."); return; }
                entries.Add(new SpriteCatalogEntry { id = id, sprite = row.FindPropertyRelative("sprite").objectReferenceValue as Sprite });
            }
            foreach (string id in ContentCatalog.RequiredBackgroundIds)
                if (ids.Add(id)) entries.Add(new SpriteCatalogEntry { id = id });

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add background display");
            Undo.RecordObject(catalog, "Register background sprite IDs");
            catalog.ConfigureSprites(entries.ToArray());
            if (!catalog.ValidateBackgrounds())
            {
                Undo.RevertAllDownToGroup(group);
                Notice("Background ID validation failed. Check Console. No background UI was created.");
                return;
            }
            // 既存の装飾とパネルは削除せず、覆いになる仮描画だけを無効化する。
            HidePlaceholder(backgroundRoot.Find("SeaBand"));
            HidePlaceholder(backgroundRoot.Find("Horizon"));
            HidePlaceholder(closeup.transform.Find("PlaceholderLabel"));
            foreach (Image image in oldBackgrounds)
            {
                Undo.RecordObject(image, "Remove placeholder panel fill");
                image.enabled = false;
                EditorUtility.SetDirty(image);
            }
            Undo.RecordObject(matte, "Configure background backing");
            matte.enabled = true;
            matte.raycastTarget = false;
            Undo.RecordObject(backgroundRoot, "Keep background behind game UI");
            backgroundRoot.SetAsFirstSibling();
            var artworkRoot = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            artworkRoot.transform.SetParent(backgroundRoot, false);
            Fit(artworkRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Undo.RegisterCreatedObjectUndo(artworkRoot, "Create background artwork Image");
            var labelRoot = new GameObject("BackgroundFallback", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelRoot.transform.SetParent(backgroundRoot, false);
            Fit(labelRoot.GetComponent<RectTransform>(), new Vector2(0.1f, 0.008f), new Vector2(0.9f, 0.045f));
            TMP_Text label = labelRoot.GetComponent<TextMeshProUGUI>();
            label.font = source.font;
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.richText = false;
            label.raycastTarget = false;
            Undo.RegisterCreatedObjectUndo(labelRoot, "Create background fallback label");
            var view = Undo.AddComponent<BackgroundView>(flow.gameObject);
            view.Configure(catalog, matte, artworkRoot.GetComponent<Image>(), label);
            view.SetBackground("BG_TITLE");
            Undo.RecordObject(flow, "Connect background view");
            flow.ConfigureBackground(view);
            EditorUtility.SetDirty(catalog);
            EditorUtility.SetDirty(matte);
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = flow.gameObject;
            Notice("Save MainScene (Ctrl+S), then Play. Background IDs BG_TITLE and BG_01-07 are registered. Missing sprites show colored placeholders. START now fades into the cell; ROOM/furniture and endings change behind the fade. Assign final Sprite assets in ContentCatalog > Sprites. Existing buttons, dialogs and item rules are preserved.");
        }

        private static void HidePlaceholder(Transform target)
        {
            if (target == null) return;
            Undo.RecordObject(target.gameObject, "Hide old background placeholder");
            target.gameObject.SetActive(false);
        }

        private static void Fit(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M12", message, "OK"); }
    }
}
