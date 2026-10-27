using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesFadeSetup
    {
        [MenuItem("Sea of Memories/08 - Add Screen Fades")]
        public static void AddScreenFades()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity") { Notice("Open Assets/Scenes/MainScene.unity first."); return; }
            var flows = new List<GameFlowController>();
            var effects = new List<ScreenEffects>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                flows.AddRange(sceneRoot.GetComponentsInChildren<GameFlowController>(true));
                effects.AddRange(sceneRoot.GetComponentsInChildren<ScreenEffects>(true));
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return; }
            if (effects.Count != 0) { Notice("ScreenEffects already exists. No duplicate was created."); return; }
            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var inventory = serializedFlow.FindProperty("inventory").objectReferenceValue as InventoryPanel;
            var interrogation = serializedFlow.FindProperty("interrogation").objectReferenceValue as InterrogationController;
            var room = serializedFlow.FindProperty("roomPanel").objectReferenceValue as GameObject;
            var bad = serializedFlow.FindProperty("badEndingPanel").objectReferenceValue as GameObject;
            var good = serializedFlow.FindProperty("trueEndingPanel").objectReferenceValue as GameObject;
            if (inventory == null || !inventory.IsDetailsConfigured || interrogation == null || !interrogation.IsConfigured
                || room == null || bad == null || good == null)
            { Notice("Apply step 07 and save MainScene before adding fades."); return; }
            Canvas canvas = room.GetComponentInParent<Canvas>(true);
            if (canvas == null) { Notice("Check the game Canvas reference."); return; }
            canvas = canvas.rootCanvas;
            if (canvas.transform.Find("FadeOverlay") != null)
            { Notice("FadeOverlay already exists. It will not be overwritten."); return; }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add screen fades");
            var overlayRoot = new GameObject("FadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            overlayRoot.transform.SetParent(canvas.transform, false);
            var rect = overlayRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image overlay = overlayRoot.GetComponent<Image>();
            overlay.color = Color.black;
            overlay.raycastTarget = true;
            CanvasGroup fadeGroup = overlayRoot.GetComponent<CanvasGroup>();
            Undo.RegisterCreatedObjectUndo(overlayRoot, "Create fade overlay");
            var screenEffects = Undo.AddComponent<ScreenEffects>(flow.gameObject);
            screenEffects.Configure(overlay, fadeGroup);
            Undo.RecordObject(flow, "Assign screen effects");
            flow.ConfigureFades(screenEffects);
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(screenEffects);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = flow.gameObject;
            Notice("Save MainScene (Ctrl+S), then Play. Furniture / BACK should fade through black; BAD END fades black and TRUE END fades white. Input is blocked during fades. Background Fade Seconds and Ending Fade Seconds are adjustable on ScreenEffects.");
        }

        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M08", message, "OK"); }
    }
}
