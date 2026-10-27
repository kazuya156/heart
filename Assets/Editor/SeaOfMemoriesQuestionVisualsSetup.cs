using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesQuestionVisualsSetup
    {
        [MenuItem("Sea of Memories/10 - Add Question Visuals")]
        public static void AddQuestionVisuals()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity") { Notice("Open Assets/Scenes/MainScene.unity first."); return; }
            var flows = new List<GameFlowController>();
            var portraits = new List<GuardPortraitView>();
            var cursors = new List<CursorOverlay>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                flows.AddRange(sceneRoot.GetComponentsInChildren<GameFlowController>(true));
                portraits.AddRange(sceneRoot.GetComponentsInChildren<GuardPortraitView>(true));
                cursors.AddRange(sceneRoot.GetComponentsInChildren<CursorOverlay>(true));
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return; }
            if (portraits.Count != 0) { Notice("GuardPortraitView already exists. No duplicate was created."); return; }
            if (cursors.Count != 1 || !cursors[0].IsConfigured) { Notice("Apply step 09 and save MainScene first."); return; }
            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var effects = serializedFlow.FindProperty("effects").objectReferenceValue as ScreenEffects;
            var interrogation = serializedFlow.FindProperty("interrogation").objectReferenceValue as InterrogationController;
            var dialogue = serializedFlow.FindProperty("dialogue").objectReferenceValue as DialoguePlayer;
            var catalog = serializedFlow.FindProperty("catalog").objectReferenceValue as ContentCatalog;
            var repository = serializedFlow.FindProperty("repository").objectReferenceValue as ScenarioRepository;
            var scenario = serializedFlow.FindProperty("scenario").objectReferenceValue as TextAsset;
            var questionPanel = serializedFlow.FindProperty("questionPanel").objectReferenceValue as GameObject;
            if (effects == null || !effects.IsConfigured || effects.HasVignette || interrogation == null
                || !interrogation.IsConfigured || interrogation.HasVisualSetup || dialogue == null || catalog == null
                || repository == null || scenario == null || questionPanel == null)
            { Notice("Check step 09 scene references. Existing vignette/visual references will not be overwritten."); return; }
            if (questionPanel.transform.Find("RedVignette") != null)
            { Notice("RedVignette already exists. It will not be overwritten."); return; }
            Transform guardRoot = questionPanel.transform.Find("InterrogationUI/GuardPlaceholder");
            Image guardImage = guardRoot == null ? null : guardRoot.GetComponent<Image>();
            TMP_Text fallback = guardRoot == null ? null : guardRoot.GetComponentInChildren<TMP_Text>(true);
            if (guardImage == null || fallback == null) { Notice("Check the existing guard placeholder."); return; }
            if (!repository.Load(scenario) || !catalog.Validate()) { Notice("Check Console for scenario/catalog errors."); return; }
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
            string[] required = { "CH_01_normal", "shaken1", "shaken2", "limit", "smile" };
            foreach (string id in required)
                if (ids.Add(id)) entries.Add(new SpriteCatalogEntry { id = id, sprite = null });

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add question visuals");
            Undo.RecordObject(catalog, "Register portrait sprite IDs");
            catalog.ConfigureSprites(entries.ToArray());
            if (!catalog.ValidatePortraits())
            {
                Undo.RevertAllDownToGroup(group);
                Notice("Portrait ID validation failed. Check Console. No visual UI was created.");
                return;
            }
            var vignetteRoot = new GameObject("RedVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            vignetteRoot.transform.SetParent(questionPanel.transform, false);
            var rect = vignetteRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image vignette = vignetteRoot.GetComponent<Image>();
            vignette.raycastTarget = false;
            Undo.RegisterCreatedObjectUndo(vignetteRoot, "Create red vignette");
            var portrait = Undo.AddComponent<GuardPortraitView>(flow.gameObject);
            Undo.RecordObject(guardImage, "Bind portrait placeholder");
            Undo.RecordObject(fallback, "Label portrait expression");
            portrait.Configure(catalog, guardRoot.GetComponent<RectTransform>(), guardImage, fallback);
            portrait.ResetView();
            Undo.RecordObject(effects, "Assign vignette image");
            effects.ConfigureVignette(vignette);
            Undo.RecordObject(interrogation, "Assign question visuals");
            interrogation.ConfigureVisuals(effects, portrait);
            Undo.RecordObject(dialogue, "Assign scenario portrait target");
            dialogue.ConfigurePortrait(portrait);
            EditorUtility.SetDirty(catalog);
            EditorUtility.SetDirty(effects);
            EditorUtility.SetDirty(interrogation);
            EditorUtility.SetDirty(dialogue);
            EditorUtility.SetDirty(portrait);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = flow.gameObject;
            Notice("Save MainScene (Ctrl+S), then Play. Valid questions strengthen the red vignette and shake the guard placeholder (0.18 s / 8 px). A dummy fades the red over 0.5 s without a new shake. Portrait sprites can be assigned later in ContentCatalog. Audio is added later.");
        }

        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M10", message, "OK"); }
    }
}
