using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesCursorSetup
    {
        [MenuItem("Sea of Memories/09 - Add Cursor and Hover")]
        public static void AddCursorAndHover()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity") { Notice("Open Assets/Scenes/MainScene.unity first."); return; }
            var flows = new List<GameFlowController>();
            var cursors = new List<CursorOverlay>();
            var hotspots = new List<HotspotView>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                flows.AddRange(sceneRoot.GetComponentsInChildren<GameFlowController>(true));
                cursors.AddRange(sceneRoot.GetComponentsInChildren<CursorOverlay>(true));
                hotspots.AddRange(sceneRoot.GetComponentsInChildren<HotspotView>(true));
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return; }
            if (cursors.Count != 0 || hotspots.Count != 0)
            { Notice("CursorOverlay or HotspotView already exists. No duplicate was created."); return; }
            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var effects = serializedFlow.FindProperty("effects").objectReferenceValue as ScreenEffects;
            var exploration = serializedFlow.FindProperty("exploration").objectReferenceValue as ExplorationController;
            var room = serializedFlow.FindProperty("roomPanel").objectReferenceValue as GameObject;
            if (effects == null || !effects.IsConfigured || exploration == null || !exploration.HasAcquisitionUI || room == null)
            { Notice("Apply step 08 and save MainScene before adding cursor/hover."); return; }
            Canvas canvas = room.GetComponentInParent<Canvas>(true);
            if (canvas == null) { Notice("Check the game Canvas."); return; }
            canvas = canvas.rootCanvas;
            if (canvas.transform.Find("CursorOverlay") != null)
            { Notice("A CursorOverlay object already exists. It will not be overwritten."); return; }
            var serializedExploration = new SerializedObject(exploration);
            SerializedProperty furniture = serializedExploration.FindProperty("furnitureButtons");
            SerializedProperty items = serializedExploration.FindProperty("itemButtons");
            if (furniture.arraySize != 3 || items.arraySize != 2)
            { Notice("Expected 3 furniture buttons and 2 reused item buttons."); return; }
            var buttons = new List<Button>();
            for (int i = 0; i < furniture.arraySize; i++) buttons.Add(furniture.GetArrayElementAtIndex(i).objectReferenceValue as Button);
            for (int i = 0; i < items.arraySize; i++) buttons.Add(items.GetArrayElementAtIndex(i).objectReferenceValue as Button);
            var distinct = new HashSet<Button>();
            foreach (Button button in buttons)
                if (button == null || button.targetGraphic == null || !distinct.Add(button)
                    || button.transform.Find("HoverHighlight") != null)
                { Notice("Check the five distinct furniture/item buttons and their graphics."); return; }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add cursor and hover");
            var iconRoot = new GameObject("CursorOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconRoot.transform.SetParent(canvas.transform, false);
            var iconRect = iconRoot.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(64f, 64f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.pivot = new Vector2(50f / 128f, 78f / 128f);
            Image icon = iconRoot.GetComponent<Image>();
            icon.color = Color.white;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.enabled = false;
            Undo.RegisterCreatedObjectUndo(iconRoot, "Create cursor image");
            var cursor = Undo.AddComponent<CursorOverlay>(flow.gameObject);
            cursor.Configure(canvas, icon);
            foreach (Button button in buttons)
            {
                var frame = new GameObject("HoverHighlight", typeof(RectTransform));
                frame.transform.SetParent(button.transform, false);
                var rect = frame.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                Edge("Top", frame.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -2f), Vector2.zero);
                Edge("Bottom", frame.transform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f));
                Edge("Left", frame.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(2f, 0f));
                Edge("Right", frame.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-2f, 0f), Vector2.zero);
                frame.SetActive(false);
                Undo.RegisterCreatedObjectUndo(frame, "Create hotspot highlight frame");
                var view = Undo.AddComponent<HotspotView>(button.gameObject);
                view.Configure(flow, cursor, button, frame);
                EditorUtility.SetDirty(view);
            }
            EditorUtility.SetDirty(cursor);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = flow.gameObject;
            Notice("Save MainScene (Ctrl+S), then Play. Move over DESK/BED/WINDOW and uncollected items: the magnifier grows from 64 to 84 and a cyan frame appears. No hover during popups, reread or fades. Stop Play or leave the game window to restore the OS cursor. The magnifier is a replaceable runtime placeholder.");
        }

        private static void Edge(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.5f, 1f, 1f, 0.45f);
            image.raycastTarget = false;
        }

        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M09", message, "OK"); }
    }
}
