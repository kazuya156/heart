using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SeaOfMemories.EditorTools
{
    // Editorフォルダー内だけで動く、シーン組立用ツール。ゲーム本体には入らない。
    public static class SeaOfMemoriesSetup
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";
        private const string ScenarioPath = "Assets/Data/Scenario/scenario.json";
        private static readonly Color Ink = new Color(0.025f, 0.075f, 0.12f);
        private static readonly Color Accent = new Color(0.45f, 0.85f, 0.82f);

        [MenuItem("Sea of Memories/01 - Create Starter Scene")]
        public static void CreateStarterScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
#if !ENABLE_INPUT_SYSTEM
            EditorUtility.DisplayDialog("Input System required",
                "Install Input System from Package Manager, then set Active Input Handling to Input System Package (New) and restart Unity. Run this menu again afterward.", "OK");
            return;
#else
            if (File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("MainScene already exists",
                    "The setup will not overwrite MainScene. Open Assets/Scenes/MainScene.unity to continue testing.", "OK");
                return;
            }
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                EditorUtility.DisplayDialog("TMP resources required",
                    "Select Window > TextMeshPro > Import TMP Essential Resources. Import the resources, then run this setup again.", "OK");
                return;
            }
            TextAsset scenario = AssetDatabase.LoadAssetAtPath<TextAsset>(ScenarioPath);
            if (scenario == null)
            {
                EditorUtility.DisplayDialog("Scenario not found", "Copy the complete Assets folder from the starter ZIP into this project.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = Ink;
            camera.GetComponent<Camera>().orthographic = true;
            camera.AddComponent<AudioListener>();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = eventSystem.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            // この段階のEnter/Spaceは会話送り専用。UI Submitとの競合を防ぐ。
            module.submit = null;

            var systems = new GameObject("GameSystems");
            var repository = systems.AddComponent<ScenarioRepository>();
            var dialogue = systems.AddComponent<DialoguePlayer>();
            var input = systems.AddComponent<UiInputRouter>();
            var flow = systems.AddComponent<GameFlowController>();
            var titleController = systems.AddComponent<TitleScreenController>();

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = Box("BackgroundImage", canvasObject.transform, Vector2.zero, Vector2.one, Ink);
            background.raycastTarget = false;
            Box("SeaBand", background.transform, new Vector2(0f, 0.38f), new Vector2(1f, 0.54f), new Color(0.04f, 0.16f, 0.22f));
            Box("Horizon", background.transform, new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.542f), Accent);

            GameObject title = Panel("TitlePanel", canvasObject.transform);
            Label("ProjectLabel", title.transform, "A SHORT VISUAL NOVEL", 24f, new Vector2(0.15f, 0.73f), new Vector2(0.85f, 0.79f), Accent);
            Label("GameTitle", title.transform, "Sea of Memories", 90f, new Vector2(0.1f, 0.57f), new Vector2(0.9f, 0.72f), Color.white);
            Button start = MakeButton("StartButton", title.transform, "START", new Vector2(0.38f, 0.28f), new Vector2(0.62f, 0.36f));
            Button quit = MakeButton("QuitButton", title.transform, "QUIT", new Vector2(0.38f, 0.17f), new Vector2(0.62f, 0.25f));
            Label("Milestone", title.transform, "PLACEHOLDER CONTENT", 21f, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.1f), Accent);

            GameObject room = Panel("RoomPanel", canvasObject.transform);
            Label("RoomTitle", room.transform, "ROOM", 64f, new Vector2(0.15f, 0.66f), new Vector2(0.85f, 0.79f), Color.white);
            Label("RoomNote", room.transform, "Title > Intro > Room: complete.\nFurniture and items are not implemented yet.", 30f,
                new Vector2(0.12f, 0.47f), new Vector2(0.88f, 0.64f), Accent);
            Button returnToTitle = MakeButton("DevelopmentReturnButton", room.transform, "TEST: RETURN TO TITLE",
                new Vector2(0.32f, 0.26f), new Vector2(0.68f, 0.35f));
            Label("DevelopmentNote", room.transform, "Development-only button. Not part of the final game UI.", 21f,
                new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.22f), Color.gray);

            GameObject conversation = Panel("DialoguePanel", canvasObject.transform);
            Label("IntroNote", conversation.transform, "INTRO / PLACEHOLDER SCRIPT", 24f,
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.8f), Accent);
            Image window = Box("DialogueWindow", conversation.transform, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.33f), new Color(0.02f, 0.04f, 0.065f, 0.96f));
            window.raycastTarget = true;
            Button advance = window.gameObject.AddComponent<Button>();
            advance.targetGraphic = window;
            advance.transition = Selectable.Transition.None;
            advance.navigation = new Navigation { mode = Navigation.Mode.None };
            TMP_Text speaker = Label("SpeakerText", window.transform, "", 25f, new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.94f), Accent);
            speaker.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text body = Label("BodyText", window.transform, "", 34f, new Vector2(0.04f, 0.23f), new Vector2(0.96f, 0.73f), Color.white);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.richText = false;
            Label("AdvanceHint", window.transform, "CLICK TEXT WINDOW / ENTER / SPACE", 18f,
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.18f), Color.gray);

            dialogue.Configure(repository, speaker, body);
            input.Configure(flow, dialogue);
            SeaOfMemoriesChoiceSetup.CreateAndBind(canvasObject.transform, dialogue, input);
            flow.Configure(repository, scenario, dialogue, input, title, conversation, room);
            titleController.Configure(flow);
            UnityEventTools.AddPersistentListener(start.onClick, titleController.OnStartPressed);
            UnityEventTools.AddPersistentListener(quit.onClick, titleController.OnQuitPressed);
            UnityEventTools.AddPersistentListener(advance.onClick, input.RequestAdvance);
            UnityEventTools.AddPersistentListener(returnToTitle.onClick, flow.EnterTitle);
            room.SetActive(false);
            conversation.SetActive(false);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[SeaOfMemories] Could not save MainScene.");
                return;
            }
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
                if (existing.path != ScenePath) scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
            Debug.Log("[SeaOfMemories] Starter scene created. Open Game view, choose 16:9, and press Play.");
            EditorUtility.DisplayDialog("Starter scene ready", "Open the Game tab and select 16:9. Press Play, then START. Read the placeholder intro, choose a response and check that ROOM appears.", "OK");
#endif
        }

        private static GameObject Panel(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            Fit(obj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            return obj;
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
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Label(string name, Transform parent, string content, float size,
            Vector2 min, Vector2 max, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            Fit(obj.GetComponent<RectTransform>(), min, max);
            TMP_Text label = obj.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = size;
            label.text = content;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private static Button MakeButton(string name, Transform parent, string text, Vector2 min, Vector2 max)
        {
            Image image = Box(name, parent, min, max, new Color(0.08f, 0.24f, 0.29f));
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", image.transform, text, 27f, Vector2.zero, Vector2.one, Color.white);
            return button;
        }
    }
}
