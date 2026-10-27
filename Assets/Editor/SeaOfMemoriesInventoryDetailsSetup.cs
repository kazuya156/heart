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
    public static class SeaOfMemoriesInventoryDetailsSetup
    {
        [MenuItem("Sea of Memories/06 - Add Inventory Reread")]
        public static void AddInventoryReread()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity") { Notice("Open Assets/Scenes/MainScene.unity first."); return; }
            var flows = new List<GameFlowController>();
            var viewers = new List<ItemDetailsViewer>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                flows.AddRange(sceneRoot.GetComponentsInChildren<GameFlowController>(true));
                viewers.AddRange(sceneRoot.GetComponentsInChildren<ItemDetailsViewer>(true));
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return; }
            if (viewers.Count != 0) { Notice("ItemDetailsViewer already exists. No duplicate was created."); return; }
            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var inventory = serializedFlow.FindProperty("inventory").objectReferenceValue as InventoryPanel;
            var dialogue = serializedFlow.FindProperty("dialogue").objectReferenceValue as DialoguePlayer;
            var input = serializedFlow.FindProperty("input").objectReferenceValue as UiInputRouter;
            var repository = serializedFlow.FindProperty("repository").objectReferenceValue as ScenarioRepository;
            if (inventory == null || !inventory.IsConfigured || dialogue == null || input == null || repository == null)
            { Notice("Apply step 05 and save MainScene before adding reread."); return; }
            var serializedInventory = new SerializedObject(inventory);
            var catalog = serializedInventory.FindProperty("catalog").objectReferenceValue as ContentCatalog;
            var serializedDialogue = new SerializedObject(dialogue);
            var choices = serializedDialogue.FindProperty("choicePanel").objectReferenceValue as ChoicePanel;
            Canvas canvas = inventory.GetComponentInParent<Canvas>(true);
            TMP_Text source = inventory.GetComponentInChildren<TMP_Text>(true);
            if (catalog == null || choices == null || !choices.IsConfigured || canvas == null || source == null || source.font == null)
            { Notice("Check the inventory catalog, choice UI and TMP font references."); return; }
            canvas = canvas.rootCanvas;
            if (canvas.transform.Find("ItemDetailsPanel") != null)
            { Notice("ItemDetailsPanel already exists. It will not be overwritten."); return; }
            var slots = new Image[GameSession.MaxItemCount];
            for (int i = 0; i < slots.Length; i++)
            {
                Transform target = inventory.transform.Find("Slot" + i);
                slots[i] = target == null ? null : target.GetComponent<Image>();
                if (slots[i] == null || slots[i].GetComponent<Button>() != null)
                { Notice("Expected an unchanged step 05 inventory slot: Slot" + i); return; }
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add inventory reread");
            TMP_FontAsset font = source.font;
            Image root = Box("ItemDetailsPanel", canvas.transform, Vector2.zero, Vector2.one,
                new Color(0f, 0f, 0f, 0.78f));
            // 全画面の暗幕が家具・質問・所持欄へのクリックを遮る。
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create inventory detail modal");
            var viewer = root.gameObject.AddComponent<ItemDetailsViewer>();
            Image frame = Box("DetailFrame", root.transform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.90f),
                new Color(0.035f, 0.085f, 0.13f));
            TMP_Text title = Label("ItemTitle", frame.transform, "ITEM DETAILS", font, 38f,
                new Vector2(0.04f, 0.86f), new Vector2(0.78f, 0.98f));
            title.enableAutoSizing = true;
            title.fontSizeMin = 22f;
            title.fontSizeMax = 38f;
            Button close = MakeButton("CloseButton", frame.transform, new Vector2(0.81f, 0.87f), new Vector2(0.97f, 0.97f));
            Label("Label", close.transform, "CLOSE", font, 28f, Vector2.zero, Vector2.one);
            UnityEventTools.AddPersistentListener(close.onClick, inventory.CloseDetails);
            Image pictureArea = Box("ImageArea", frame.transform, new Vector2(0.04f, 0.16f), new Vector2(0.36f, 0.82f),
                new Color(0.06f, 0.14f, 0.18f));
            Image image = Box("LargeImage", pictureArea.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            TMP_Text placeholder = Label("ImagePlaceholder", pictureArea.transform, "[IMAGE PLACEHOLDER]", font, 28f,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            Button advance = MakeButton("AdvanceButton", frame.transform, new Vector2(0.40f, 0.16f), new Vector2(0.96f, 0.82f));
            UnityEventTools.AddPersistentListener(advance.onClick, input.RequestAdvance);
            TMP_Text speaker = Label("Speaker", advance.transform, "", font, 26f,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f));
            speaker.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text body = Label("Body", advance.transform, "", font, 30f,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.80f));
            body.alignment = TextAlignmentOptions.TopLeft;
            Label("AdvanceHint", frame.transform, "Click text / Enter / Space", font, 22f,
                new Vector2(0.40f, 0.04f), new Vector2(0.96f, 0.12f));
            viewer.Configure(repository, title, speaker, body, image, placeholder);

            var buttons = new Button[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                Undo.RecordObject(slots[i], "Enable slot raycast");
                slots[i].raycastTarget = true;
                buttons[i] = Undo.AddComponent<Button>(slots[i].gameObject);
                buttons[i].targetGraphic = slots[i];
                buttons[i].navigation = new Navigation { mode = Navigation.Mode.None };
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, inventory.OpenSlot, i);
            }
            Undo.RecordObject(inventory, "Assign reread references");
            Undo.RecordObject(input, "Route detail input");
            // 選択肢の暗幕も左80%に収め、TAKE/LEAVE中に右の所持欄を押せるようにする。
            var choiceRect = choices.GetComponent<RectTransform>();
            Undo.RecordObject(choiceRect, "Keep inventory accessible during choices");
            Fit(choiceRect, Vector2.zero, new Vector2(0.8f, 1f));
            inventory.ConfigureDetails(flow, dialogue, viewer, choices, input, buttons);
            input.ConfigureDetails(viewer);
            root.gameObject.SetActive(false);
            EditorUtility.SetDirty(inventory);
            EditorUtility.SetDirty(input);
            EditorUtility.SetDirty(viewer);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root.gameObject;
            Notice("Save MainScene (Ctrl+S), then Play. Acquire an item and click its inventory slot. Read with the text button / Enter / Space; use CLOSE or finish the description. Main dialogue resumes from the same position. TAKE/LEAVE must remain unchanged.");
        }

        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M06", message, "OK"); }

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

        private static Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max)
        {
            Image image = Box(name, parent, min, max, new Color(0.06f, 0.14f, 0.18f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
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
