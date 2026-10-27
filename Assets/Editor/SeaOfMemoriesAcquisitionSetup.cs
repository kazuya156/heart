using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesAcquisitionSetup
    {
        [MenuItem("Sea of Memories/05 - Add Acquisition Loop")]
        public static void AddAcquisitionLoop()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity") { Notice("Open Assets/Scenes/MainScene.unity first."); return; }
            var flows = new List<GameFlowController>();
            var inventories = new List<InventoryPanel>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                flows.AddRange(root.GetComponentsInChildren<GameFlowController>(true));
                inventories.AddRange(root.GetComponentsInChildren<InventoryPanel>(true));
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return; }
            if (inventories.Count != 0) { Notice("InventoryPanel already exists. No duplicate was created. Check its references if needed."); return; }
            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var catalog = serializedFlow.FindProperty("catalog").objectReferenceValue as ContentCatalog;
            var exploration = serializedFlow.FindProperty("exploration").objectReferenceValue as ExplorationController;
            var dialogue = serializedFlow.FindProperty("dialogue").objectReferenceValue as DialoguePlayer;
            var room = serializedFlow.FindProperty("roomPanel").objectReferenceValue as GameObject;
            var closeup = serializedFlow.FindProperty("closeupPanel").objectReferenceValue as GameObject;
            var conversation = serializedFlow.FindProperty("dialoguePanel").objectReferenceValue as GameObject;
            var inspection = serializedFlow.FindProperty("inspectionPanel").objectReferenceValue as GameObject;
            if (catalog == null || exploration == null || dialogue == null || room == null || closeup == null
                || conversation == null || inspection == null || !exploration.HasInspectionUI)
            {
                Notice("Apply step 04 and save MainScene before adding acquisition.");
                return;
            }
            Canvas canvas = room.GetComponentInParent<Canvas>(true);
            if (canvas == null) { Notice("Room Panel must be under the game Canvas."); return; }
            canvas = canvas.rootCanvas;
            if (canvas.transform.Find("InventoryPanel") != null || canvas.transform.Find("QuestionPreviewPanel") != null)
            {
                Notice("An existing inventory or question preview was found. It will not be overwritten.");
                return;
            }
            var serializedDialogue = new SerializedObject(dialogue);
            var choice = serializedDialogue.FindProperty("choicePanel").objectReferenceValue as ChoicePanel;
            if (choice == null || !choice.IsConfigured) { Notice("Check the ChoicePanel reference on DialoguePlayer."); return; }
            string[] furnitureIds = { "DESK", "BED", "WINDOW" };
            var buttons = new Button[3];
            for (int i = 0; i < buttons.Length; i++)
            {
                Transform target = room.transform.Find("FurnitureNavigation/" + furnitureIds[i] + "Button");
                buttons[i] = target == null ? null : target.GetComponent<Button>();
                if (buttons[i] == null) { Notice("Furniture button missing: " + furnitureIds[i]); return; }
            }
            Transform backTarget = closeup.transform.Find("ReturnToRoomButton");
            Button back = backTarget == null ? null : backTarget.GetComponent<Button>();
            TMP_Text source = closeup.GetComponentInChildren<TMP_Text>(true);
            if (back == null || source == null || source.font == null) { Notice("Check BACK and the closeup TMP font."); return; }
            if (!SeaOfMemoriesAcquisitionChecks.CheckOrLog()) { Notice("Acquisition rule checks failed. Check Console before continuing."); return; }
            TMP_FontAsset font = source.font;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add acquisition loop");
            Image inventoryRoot = Box("InventoryPanel", canvas.transform, new Vector2(0.82f, 0.06f),
                new Vector2(0.98f, 0.94f), new Color(0.02f, 0.045f, 0.065f, 0.96f));
            inventoryRoot.raycastTarget = false;
            CanvasGroup canvasGroup = inventoryRoot.gameObject.AddComponent<CanvasGroup>();
            var inventory = inventoryRoot.gameObject.AddComponent<InventoryPanel>();
            Undo.RegisterCreatedObjectUndo(inventoryRoot.gameObject, "Create inventory slots");
            TMP_Text heading = Label("InventoryHeading", inventoryRoot.transform, "INFORMATION (0/4)", font, 22f,
                new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.98f));
            heading.enableAutoSizing = true;
            heading.fontSizeMin = 18f;
            heading.fontSizeMax = 22f;
            var icons = new Image[GameSession.MaxItemCount];
            var names = new TMP_Text[GameSession.MaxItemCount];
            for (int i = 0; i < GameSession.MaxItemCount; i++)
            {
                float top = 0.87f - i * 0.20f;
                Image slot = Box("Slot" + i, inventoryRoot.transform, new Vector2(0.05f, top - 0.17f),
                    new Vector2(0.95f, top), new Color(0.06f, 0.14f, 0.18f));
                slot.raycastTarget = false;
                icons[i] = Box("Icon", slot.transform, new Vector2(0.32f, 0.35f), new Vector2(0.68f, 0.94f), Color.white);
                icons[i].raycastTarget = false;
                icons[i].preserveAspect = true;
                icons[i].enabled = false;
                names[i] = Label("ItemName", slot.transform, "", font, 22f, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f));
                names[i].enableAutoSizing = true;
                names[i].fontSizeMin = 14f;
                names[i].fontSizeMax = 22f;
            }
            inventory.Configure(catalog, heading, icons, names, canvasGroup);
            Image preview = Box("QuestionPreviewPanel", canvas.transform, Vector2.zero, new Vector2(0.8f, 1f),
                new Color(0.035f, 0.085f, 0.13f));
            Undo.RegisterCreatedObjectUndo(preview.gameObject, "Create interrogation preview");
            Label("InterrogationTitle", preview.transform, "INTERROGATION", font, 70f,
                new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.8f));

            // 右4枠の領域を確保。既存パネルは保持し、その子のUI配置も相対的に維持する。
            SetWorldViewport(room);
            SetWorldViewport(closeup);
            SetWorldViewport(conversation);
            SetWorldViewport(inspection);
            Transform choiceHeading = choice.transform.Find("ChoiceHeading");
            TMP_Text headingText = choiceHeading == null ? null : choiceHeading.GetComponent<TMP_Text>();
            if (headingText != null)
            {
                Undo.RecordObject(headingText, "Use shared choice heading");
                headingText.text = "CHOOSE";
            }
            Undo.RecordObject(flow, "Assign inventory references");
            Undo.RecordObject(exploration, "Assign acquisition references");
            flow.ConfigureAcquisition(inventory, preview.gameObject);
            exploration.ConfigureAcquisition(choice, buttons, back);
            inventoryRoot.gameObject.SetActive(false);
            preview.gameObject.SetActive(false);
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(exploration);
            EditorUtility.SetDirty(inventory);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = inventoryRoot.gameObject;
            Notice("Save MainScene (Ctrl+S). Play and inspect an item. TAKE adds it; LEAVE keeps it in place. Collect four to play the guard return and interrogation intro. INTERROGATION is this step's test endpoint; questions are added later.");
        }

        private static void SetWorldViewport(GameObject panel)
        {
            var rect = panel.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Reserve inventory viewport");
            Fit(rect, Vector2.zero, new Vector2(0.8f, 1f));
        }

        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M05", message, "OK"); }

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
    }
}
