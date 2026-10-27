using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesExplorationSetup
    {
        [MenuItem("Sea of Memories/03 - Add Furniture Navigation")]
        public static void AddFurnitureNavigation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity")
            {
                Notice("Open Assets/Scenes/MainScene.unity, then run this menu again.");
                return;
            }
            var flows = new List<GameFlowController>();
            var controllers = new List<ExplorationController>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                flows.AddRange(root.GetComponentsInChildren<GameFlowController>(true));
                controllers.AddRange(root.GetComponentsInChildren<ExplorationController>(true));
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController. No changes were made."); return; }
            if (controllers.Count != 0) { Notice("ExplorationController already exists. Check its references; no duplicate was created."); return; }

            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var room = serializedFlow.FindProperty("roomPanel").objectReferenceValue as GameObject;
            var closeup = serializedFlow.FindProperty("closeupPanel").objectReferenceValue as GameObject;
            Canvas canvas = room == null ? null : room.GetComponentInParent<Canvas>(true);
            if (canvas == null) { Notice("GameFlowController needs Room Panel under the game Canvas."); return; }
            canvas = canvas.rootCanvas;
            if (closeup != null || canvas.transform.Find("CloseupPanel") != null
                || room.transform.Find("FurnitureNavigation") != null)
            {
                Notice("Existing furniture UI was found. It will not be overwritten. Check its references in the Inspector.");
                return;
            }
            TMP_Text source = room.GetComponentInChildren<TMP_Text>(true);
            TMP_FontAsset font = source == null ? null : source.font;
            if (font == null) { Notice("Assign a TMP font to the room text before running this menu."); return; }
            FurnitureDefinition[] definitions = LoadDefinitions();
            if (definitions == null) return;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add furniture navigation");
            Undo.RecordObject(flow, "Assign exploration references");
            var controller = Undo.AddComponent<ExplorationController>(flow.gameObject);

            var navigation = new GameObject("FurnitureNavigation", typeof(RectTransform));
            navigation.transform.SetParent(room.transform, false);
            Fit(navigation.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Undo.RegisterCreatedObjectUndo(navigation, "Create furniture buttons");
            for (int i = 0; i < definitions.Length; i++)
            {
                float left = 0.1f + i * 0.29f;
                Button button = MakeButton(definitions[i].Id + "Button", navigation.transform,
                    definitions[i].Id, font, new Vector2(left, 0.34f), new Vector2(left + 0.22f, 0.60f));
                UnityEventTools.AddStringPersistentListener(button.onClick, controller.OpenFurniture, definitions[i].Id);
            }

            Image closeupImage = Box("CloseupPanel", canvas.transform, Vector2.zero, Vector2.one,
                new Color(0.06f, 0.16f, 0.21f));
            Undo.RegisterCreatedObjectUndo(closeupImage.gameObject, "Create closeup panel");
            TMP_Text title = Label("FurnitureTitle", closeupImage.transform, "", font, 70f,
                new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.82f));
            Label("PlaceholderLabel", closeupImage.transform, "[PLACEHOLDER BACKGROUND]", font, 25f,
                new Vector2(0.2f, 0.44f), new Vector2(0.8f, 0.54f));
            Button back = MakeButton("ReturnToRoomButton", closeupImage.transform, "BACK", font,
                new Vector2(0.07f, 0.08f), new Vector2(0.27f, 0.17f));
            UnityEventTools.AddPersistentListener(back.onClick, controller.ReturnToRoom);

            // 初期段階の説明だけを隠す。既存オブジェクトは削除しない。
            HideOldNote(room.transform.Find("RoomNote"));
            HideOldNote(room.transform.Find("DevelopmentNote"));
            Transform returnButton = room.transform.Find("DevelopmentReturnButton");
            if (returnButton != null)
            {
                var rect = returnButton.GetComponent<RectTransform>();
                Undo.RecordObject(rect, "Move development title button");
                Fit(rect, new Vector2(0.32f, 0.12f), new Vector2(0.68f, 0.21f));
            }
            controller.Configure(flow, definitions, title, closeupImage);
            flow.ConfigureExploration(controller, closeupImage.gameObject);
            closeupImage.gameObject.SetActive(false);
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = navigation;
            Notice("Save MainScene (Ctrl+S). Play > START, finish the intro, then test DESK, BED and WINDOW. BACK returns to ROOM. Images and items will be added later.");
        }

        private static FurnitureDefinition[] LoadDefinitions()
        {
            const string folder = "Assets/Data/Definitions";
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Data", "Definitions");
            string[] ids = { "DESK", "BED", "WINDOW" };
            string[] backgrounds = { "BG_03", "BG_04", "BG_05" };
            string[] first = { "ITEM_01", "ITEM_03", "ITEM_05" };
            string[] second = { "ITEM_02", "ITEM_04", "ITEM_06" };
            Color[] colors = { new Color(0.18f, 0.13f, 0.09f), new Color(0.10f, 0.14f, 0.23f), new Color(0.05f, 0.22f, 0.27f) };
            var result = new FurnitureDefinition[3];
            for (int i = 0; i < result.Length; i++)
            {
                string path = folder + "/" + ids[i] + ".asset";
                FurnitureDefinition definition = AssetDatabase.LoadAssetAtPath<FurnitureDefinition>(path);
                if (definition == null)
                {
                    if (File.Exists(path)) { Notice("A different asset already exists at " + path + ". Nothing was overwritten."); return null; }
                    definition = ScriptableObject.CreateInstance<FurnitureDefinition>();
                    definition.Configure(ids[i], backgrounds[i], first[i], second[i], colors[i]);
                    AssetDatabase.CreateAsset(definition, path);
                }
                if (definition.Id != ids[i] || definition.BackgroundId != backgrounds[i]
                    || definition.ItemIds == null || definition.ItemIds.Count != 2
                    || definition.ItemIds[0] != first[i] || definition.ItemIds[1] != second[i])
                {
                    Notice("Existing definition differs from the design: " + path + ". Check its IDs; it was not overwritten.");
                    return null;
                }
                result[i] = definition;
            }
            return result;
        }

        private static void HideOldNote(Transform note)
        {
            if (note == null) return;
            Undo.RecordObject(note.gameObject, "Hide old room note");
            note.gameObject.SetActive(false);
        }

        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M03", message, "OK"); }

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
            label.raycastTarget = false;
            return label;
        }

        private static Button MakeButton(string name, Transform parent, string text, TMP_FontAsset font, Vector2 min, Vector2 max)
        {
            Image image = Box(name, parent, min, max, new Color(0.08f, 0.24f, 0.29f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", image.transform, text, font, 30f, Vector2.zero, Vector2.one);
            return button;
        }
    }
}
