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
    public static class SeaOfMemoriesInspectionSetup
    {
        [MenuItem("Sea of Memories/04 - Add Item Inspection")]
        public static void AddItemInspection()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity") { Notice("Open Assets/Scenes/MainScene.unity first."); return; }
            var flows = new List<GameFlowController>();
            var catalogs = new List<ContentCatalog>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                flows.AddRange(root.GetComponentsInChildren<GameFlowController>(true));
                catalogs.AddRange(root.GetComponentsInChildren<ContentCatalog>(true));
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return; }
            if (catalogs.Count != 0) { Notice("ContentCatalog already exists. Check its references; no duplicate was created."); return; }
            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var exploration = serializedFlow.FindProperty("exploration").objectReferenceValue as ExplorationController;
            var closeup = serializedFlow.FindProperty("closeupPanel").objectReferenceValue as GameObject;
            var conversation = serializedFlow.FindProperty("dialoguePanel").objectReferenceValue as GameObject;
            var repository = serializedFlow.FindProperty("repository").objectReferenceValue as ScenarioRepository;
            var dialogue = serializedFlow.FindProperty("dialogue").objectReferenceValue as DialoguePlayer;
            var existingPopup = serializedFlow.FindProperty("inspectionPanel").objectReferenceValue as GameObject;
            if (exploration == null || closeup == null || !exploration.IsConfigured)
            {
                Notice("Apply 03 - Add Furniture Navigation and save MainScene first.");
                return;
            }
            Canvas canvas = closeup.GetComponentInParent<Canvas>(true);
            if (canvas == null || conversation == null || dialogue == null || repository == null)
            {
                Notice("Check Canvas, Dialogue Panel, DialoguePlayer and ScenarioRepository on GameSystems.");
                return;
            }
            canvas = canvas.rootCanvas;
            if (existingPopup != null || canvas.transform.Find("ItemInspectionPanel") != null
                || closeup.transform.Find("ItemHotspots") != null)
            {
                Notice("Existing item inspection UI was found. It will not be overwritten.");
                return;
            }
            TMP_Text source = closeup.GetComponentInChildren<TMP_Text>(true);
            if (source == null || source.font == null) { Notice("Assign a TMP font to the closeup title first."); return; }
            TMP_FontAsset font = source.font;
            var serializedExploration = new SerializedObject(exploration);
            SerializedProperty furnitureProperty = serializedExploration.FindProperty("furniture");
            var furniture = new FurnitureDefinition[furnitureProperty.arraySize];
            for (int i = 0; i < furniture.Length; i++)
                furniture[i] = furnitureProperty.GetArrayElementAtIndex(i).objectReferenceValue as FurnitureDefinition;
            ItemDefinition[] items = LoadItems();
            if (items == null) return;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add item inspection");
            var catalog = Undo.AddComponent<ContentCatalog>(flow.gameObject);
            var hotspots = new GameObject("ItemHotspots", typeof(RectTransform));
            hotspots.transform.SetParent(closeup.transform, false);
            Fit(hotspots.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Undo.RegisterCreatedObjectUndo(hotspots, "Create item hotspots");
            var buttons = new Button[2];
            for (int i = 0; i < buttons.Length; i++)
            {
                float left = 0.11f + i * 0.44f;
                buttons[i] = MakeButton("ItemSlot" + i, hotspots.transform, font,
                    new Vector2(left, 0.36f), new Vector2(left + 0.34f, 0.55f));
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, exploration.InspectSlot, i);
            }
            Image popup = Box("ItemInspectionPanel", canvas.transform, Vector2.zero, Vector2.one,
                new Color(0.01f, 0.025f, 0.04f, 0.88f));
            Undo.RegisterCreatedObjectUndo(popup.gameObject, "Create inspection popup");
            TMP_Text heading = Label("ItemTitle", popup.transform, "", font, 40f,
                new Vector2(0.1f, 0.81f), new Vector2(0.9f, 0.9f));
            Image frame = Box("ItemImageFrame", popup.transform, new Vector2(0.18f, 0.37f),
                new Vector2(0.82f, 0.79f), new Color(0.04f, 0.12f, 0.17f));
            frame.raycastTarget = false;
            Image picture = Box("LargeImage", frame.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Color.white);
            picture.raycastTarget = false;
            picture.preserveAspect = true;
            TMP_Text placeholder = Label("Placeholder", frame.transform, "", font, 32f, Vector2.zero, Vector2.one);
            var view = popup.gameObject.AddComponent<ItemInspectionView>();
            view.Configure(heading, picture, placeholder);
            popup.transform.SetSiblingIndex(conversation.transform.GetSiblingIndex());

            Transform oldPlaceholder = closeup.transform.Find("PlaceholderLabel");
            if (oldPlaceholder != null)
            {
                Undo.RecordObject(oldPlaceholder.gameObject, "Hide old background placeholder label");
                oldPlaceholder.gameObject.SetActive(false);
            }
            Undo.RecordObject(flow, "Assign inspection references");
            Undo.RecordObject(exploration, "Assign item references");
            catalog.Configure(furniture, items, repository);
            exploration.ConfigureInspection(catalog, dialogue, view, buttons);
            Transform introductionNote = conversation.transform.Find("IntroNote");
            flow.ConfigureInspection(catalog, popup.gameObject, introductionNote == null ? null : introductionNote.gameObject);
            popup.gameObject.SetActive(false);
            EditorUtility.SetDirty(catalog);
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(exploration);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = hotspots;
            Notice("Save MainScene (Ctrl+S). Play > START > ROOM > any furniture. Read both items with the text window, Enter or Space. After the final page, the furniture view returns. Acquisition is a later step.");
        }

        private static ItemDefinition[] LoadItems()
        {
            const string folder = "Assets/Data/Definitions";
            if (!AssetDatabase.IsValidFolder(folder)) { Notice("Furniture definitions are missing. Apply step 03 first."); return null; }
            string[] names = { "Uneven pencils", "Scribbles", "Small clothes", "Alarm clock at 2 AM", "Certificate with brother's name", "Endless sea" };
            string[] owners = { "DESK", "DESK", "BED", "BED", "WINDOW", "WINDOW" };
            var result = new ItemDefinition[6];
            for (int i = 0; i < result.Length; i++)
            {
                string id = "ITEM_0" + (i + 1);
                string path = folder + "/" + id + ".asset";
                ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item == null)
                {
                    if (File.Exists(path)) { Notice("A different asset exists at " + path + ". It was not overwritten."); return null; }
                    item = ScriptableObject.CreateInstance<ItemDefinition>();
                    item.Configure(id, names[i], i % 2 == 0, owners[i], "DESC_" + id);
                    AssetDatabase.CreateAsset(item, path);
                }
                if (item.Id != id || item.FurnitureId != owners[i] || item.IsValid != (i % 2 == 0)
                    || string.IsNullOrWhiteSpace(item.DisplayName) || string.IsNullOrWhiteSpace(item.DescriptionSequenceId))
                {
                    Notice("Existing item data differs from the design: " + path + ". It was not overwritten.");
                    return null;
                }
                result[i] = item;
            }
            return result;
        }

        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M04", message, "OK"); }

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
            label.richText = false;
            label.raycastTarget = false;
            return label;
        }

        private static Button MakeButton(string name, Transform parent, TMP_FontAsset font, Vector2 min, Vector2 max)
        {
            Image image = Box(name, parent, min, max, new Color(0.08f, 0.24f, 0.29f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", image.transform, "ITEM", font, 30f, Vector2.zero, Vector2.one);
            return button;
        }
    }
}
